using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Explicit editor command; never runs automatically on import or in a build.
[InitializeOnLoad]
public static class LesterNavigationSetup
{
    const string DataPath = "Assets/Navigation/LesterMainNavMesh.asset";
    const string AgentName = "Lester";
    static EnemyMovement testMotor;
    static float testStart;
    static int testIndex, testVisits;
    static double testDeadline;

    static LesterNavigationSetup()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("LesterPatrolTest", false)) return;
            SessionState.SetBool("LesterPatrolTest", false);
            testMotor = UnityEngine.Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None).First(x => x.name == "Lester");
            // Controlled locomotion check: prevent player sensing/noise from interrupting the route.
            var awareness = testMotor.GetComponent<EnemyAwareness>();
            typeof(EnemyAwareness).GetField("player", System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance).SetValue(awareness, null);
            foreach (var emitter in UnityEngine.Object.FindObjectsByType<PlayerNoiseEmitter>(FindObjectsSortMode.None))
                emitter.enabled = false;
            testIndex = testMotor.PatrolIndex;
            testVisits = 0;
            testStart = Time.time;
            testDeadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update += CheckPatrol;
        };
    }

    static void CheckPatrol()
    {
        Time.timeScale = 5f;
        if (testMotor.PatrolIndex != testIndex)
        {
            testIndex = testMotor.PatrolIndex;
            testVisits++;
            Debug.Log("LESTER_PATROL_VISIT " + testVisits + " position=" + testMotor.transform.position);
        }
        if (testVisits >= testMotor.patrolPoints.Length)
        {
            Debug.Log("LESTER_MAIN_PATROL_PASS completed " + testVisits + " legs in " + (Time.time - testStart) + " game seconds.");
            EditorApplication.update -= CheckPatrol;
            EditorApplication.Exit(0);
        }
        else if (Time.time - testStart > 75f || EditorApplication.timeSinceStartup > testDeadline)
        {
            Debug.LogError("LESTER_MAIN_PATROL_FAIL visits=" + testVisits + " position=" +
                testMotor.transform.position + " status=" + testMotor.NavigationStatus);
            EditorApplication.update -= CheckPatrol;
            EditorApplication.Exit(1);
        }
    }

    public static void ValidateMainPatrolBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("This controlled patrol check is batch-only.");
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
        SessionState.SetBool("LesterPatrolTest", true);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Tools/Lester/Bake Navigation and Set Up Patrol")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before baking Lester navigation.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Main.unity")
            throw new InvalidOperationException("Open Main before using Lester navigation setup.");
        var enemies = UnityEngine.Object.FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None)
            .Where(x => x.gameObject.scene == scene && x.name == "Lester").ToArray();
        if (enemies.Length != 1)
            throw new InvalidOperationException("Expected exactly one active Lester in Main.");
        var motor = enemies[0];
        int agent = EnsureAgent();
        // Characters and movable physics objects must not become baked walls.
        var roots = new HashSet<GameObject>();
        foreach (var body in UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            if (body.gameObject.scene == scene) roots.Add(body.gameObject);
        foreach (var controller in UnityEngine.Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None))
            if (controller.gameObject.scene == scene) roots.Add(controller.gameObject);
        foreach (var root in roots)
        {
            var modifier = root.GetComponent<NavMeshModifier>();
            if (modifier == null) modifier = Undo.AddComponent<NavMeshModifier>(root);
            Undo.RecordObject(modifier, "Exclude dynamic object from navigation bake");
            modifier.ignoreFromBuild = true;
            modifier.applyToChildren = true;
            EditorUtility.SetDirty(modifier);
            PrefabUtility.RecordPrefabInstancePropertyModifications(modifier);
        }
        var navigation = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Lester Navigation");
        if (navigation == null)
        {
            navigation = new GameObject("Lester Navigation");
            Undo.RegisterCreatedObjectUndo(navigation, "Create Lester navigation");
        }
        var surface = navigation.GetComponent<NavMeshSurface>();
        if (surface == null) surface = Undo.AddComponent<NavMeshSurface>(navigation);
        surface.agentTypeID = agent;
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = Physics.DefaultRaycastLayers;
        surface.overrideVoxelSize = true;
        surface.voxelSize = 0.1f;
        surface.BuildNavMesh();
        if (surface.navMeshData == null) throw new InvalidOperationException("NavMesh bake returned no data.");
        var filter = new NavMeshQueryFilter { agentTypeID = agent, areaMask = NavMesh.AllAreas };
        var collider = motor.GetComponent<Collider>();
        Vector3 feet = motor.transform.position;
        if (collider != null) feet.y = collider.bounds.min.y;
        if (!NavMesh.SamplePosition(feet, out var spawn, 3f, filter) ||
            Vector2.Distance(new Vector2(feet.x, feet.z), new Vector2(spawn.position.x, spawn.position.z)) > 0.25f)
            throw new InvalidOperationException("Lester spawn is outside the baked walkable region. Scene has not been saved.");

        Undo.RecordObject(motor, "Configure Lester navigation");
        motor.agentTypeID = agent;
        if (motor.patrolPoints == null || motor.patrolPoints.Length == 0)
        {
            var route = new GameObject("Lester Patrol");
            Undo.RegisterCreatedObjectUndo(route, "Create Lester patrol");
            var points = new List<Transform>();
            // Seed a modest, reachable route; designers can move these points and rebake.
            var offsets = new[] { new Vector3(5,0,0), new Vector3(0,0,5),
                new Vector3(-5,0,0), new Vector3(0,0,-5), new Vector3(3,0,3),
                new Vector3(-3,0,-3), Vector3.zero };
            foreach (var offset in offsets)
            {
                if (!NavMesh.SamplePosition(spawn.position + offset, out var hit, 2f, filter)) continue;
                if (points.Any(p => Vector3.Distance(p.position, hit.position) < 2f)) continue;
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(spawn.position, hit.position, filter, path) ||
                    path.status != NavMeshPathStatus.PathComplete) continue;
                var point = new GameObject("Patrol " + (points.Count + 1));
                point.transform.SetParent(route.transform);
                point.transform.position = hit.position;
                points.Add(point.transform);
                if (points.Count == 3) break;
            }
            if (points.Count < 2) throw new InvalidOperationException("Could not find two reachable patrol points. Scene has not been saved.");
            motor.patrolPoints = points.ToArray();
        }
        ValidateRoute(motor, filter);
        if (!AssetDatabase.IsValidFolder("Assets/Navigation")) AssetDatabase.CreateFolder("Assets", "Navigation");
        var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(DataPath);
        if (existing == null) AssetDatabase.CreateAsset(surface.navMeshData, DataPath);
        else
        {
            var generated = surface.navMeshData;
            surface.RemoveData();
            EditorUtility.CopySerialized(generated, existing);
            surface.navMeshData = existing;
            surface.AddData();
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(generated);
        }
        EditorUtility.SetDirty(surface);
        EditorUtility.SetDirty(motor);
        PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Main.");
        Debug.Log("LESTER_M3_SETUP_OK agent=" + agent + " patrol=" +
            string.Join("; ", motor.patrolPoints.Select(p => p.position.ToString("F2"))) +
            " excludedDynamicRoots=" + roots.Count);
    }

    static void ValidateRoute(EnemyMovement motor, NavMeshQueryFilter filter)
    {
        for (int i = 0; i < motor.patrolPoints.Length; i++)
        {
            var a = motor.patrolPoints[i];
            var b = motor.patrolPoints[(i + 1) % motor.patrolPoints.Length];
            if (a == null || b == null) throw new InvalidOperationException("Patrol contains an empty point.");
            if (!NavMesh.SamplePosition(a.position, out var start, 0.5f, filter) ||
                !NavMesh.SamplePosition(b.position, out var end, 0.5f, filter))
                throw new InvalidOperationException("Patrol point lies outside the NavMesh.");
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(start.position, end.position, filter, path) ||
                path.status != NavMeshPathStatus.PathComplete)
                throw new InvalidOperationException("Patrol points are not connected.");
        }
    }

    static int EnsureAgent()
    {
        for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
        {
            int id = NavMesh.GetSettingsByIndex(i).agentTypeID;
            if (NavMesh.GetSettingsNameFromID(id) != AgentName) continue;
            var existing = NavMesh.GetSettingsByID(id);
            if (existing.agentRadius < 0.75f || existing.agentHeight < 2.6f)
                throw new InvalidOperationException("The existing Lester agent is smaller than his collider. Correct its dimensions before baking.");
            return id;
        }
        int created = NavMesh.CreateSettings().agentTypeID;
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset")[0]);
        var agents = settings.FindProperty("m_Settings");
        var names = settings.FindProperty("m_SettingNames");
        for (int i = 0; i < agents.arraySize; i++)
        {
            var entry = agents.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("agentTypeID").intValue != created) continue;
            entry.FindPropertyRelative("agentRadius").floatValue = 0.75f;
            entry.FindPropertyRelative("agentHeight").floatValue = 2.6f;
            entry.FindPropertyRelative("agentClimb").floatValue = 0.1f;
            entry.FindPropertyRelative("agentSlope").floatValue = 45f;
            names.GetArrayElementAtIndex(i).stringValue = AgentName;
            settings.ApplyModifiedProperties();
            return created;
        }
        throw new InvalidOperationException("Could not configure Lester's navigation agent.");
    }

    // Used for an explicit unattended bake while the main Editor is closed.
    public static void SetupMainBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
            Setup();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
}
