/*
 * Author: Lam Nguyen
 * Created: 10/24/2026
 */

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class SceneManager : MonoBehaviour
{
    [Tooltip("Invoked when this component's scene finishes loading, after OnEnable and before Start. The component must be active and enabled during loading.")]
    public UnityEvent OnSceneLoaded = new UnityEvent();

    private void OnEnable()
    {
        UnitySceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        UnitySceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene == gameObject.scene)
            OnSceneLoaded?.Invoke();
    }

    // Reloads the active scene, replacing all currently loaded scenes.
    public void ResetScene()
    {
        UnitySceneManager.LoadScene(UnitySceneManager.GetActiveScene().path);
    }

    // Loads a scene using its index in the build scene list.
    public void SetSceneByIndex(int sceneIndex)
    {
        UnitySceneManager.LoadScene(sceneIndex);
    }

    // Loads a scene using its name or full scene path.
    public void SetSceneByName(string sceneName)
    {
        UnitySceneManager.LoadScene(sceneName);
    }
}
