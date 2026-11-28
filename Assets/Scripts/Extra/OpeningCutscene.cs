using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class OpeningCutscene : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera wakeUpCamera;
    [SerializeField] private CinemachineCamera gameplayCamera;

    [Header("Timing")]
    [SerializeField] private float initialWait = 1f;
    [SerializeField] private float transitionWait = 2f;
    [SerializeField] private float blendDuration = 2f;

    [Header("Cutscene Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip wakeUpGrunt;
    [SerializeField] private AudioClip getUpShuffle;

    [SerializeField] private MeshRenderer playerBody;

    [SerializeField] private Animator eyeAnimator;

    [SerializeField] private GameObject eyelids;

    [SerializeField] private Volume volume;

    [SerializeField] private Animator healthBarAnimator;

    [Header("Events")]
    [Tooltip("Invoked when the cutscene finishes and gameplay has been re-enabled.")]
    [SerializeField] private UnityEvent onCutsceneEnded = new UnityEvent();

    private PlayerMinigameContext player;

    private Vignette vignette;

    private DepthOfField DOF;

    private IEnumerator Start()
    {
        player = PlayerMinigameContext.Instance;
        // Disable gameplay
        player.Input.currentActionMap?.Disable();

        playerBody.enabled = false;

        float startVignetteIntensity = 0f;
        float startDOFIntensity = 0f;

        if (volume.profile.TryGet<Vignette>(out vignette))
        {
            vignette.intensity.value = 0.5f;
            startVignetteIntensity = vignette.intensity.value;
        }

        if (volume.profile.TryGet<DepthOfField>(out DOF))
        {
            DOF.focalLength.value = 240;
            startDOFIntensity = DOF.focalLength.value;
        }

        // Set initial camera priorities
        wakeUpCamera.Priority = 20;
        gameplayCamera.Priority = 10;

        // Wait before opening eyes
        yield return new WaitForSeconds(initialWait);

        // Blinking animation here and audio
        eyeAnimator.SetTrigger("open");
        PlayCutsceneSound(wakeUpGrunt);

        yield return new WaitForSeconds(4f);
        yield return new WaitForSeconds(transitionWait);

        PlayCutsceneSound(getUpShuffle);
        gameplayCamera.Priority = 20;
        wakeUpCamera.Priority = 10;

        // Switch to gameplay camera
        gameplayCamera.Priority = 20;
        wakeUpCamera.Priority = 10;

        float elapsedTime = 0f;

        while (elapsedTime < blendDuration)
        {
            elapsedTime += Time.deltaTime;
            DOF.focalLength.value = Mathf.Lerp(startDOFIntensity, 0f, (elapsedTime / blendDuration));
            vignette.intensity.value = Mathf.Lerp(startVignetteIntensity, 0f, (elapsedTime / blendDuration));
            yield return null;
        }

        // Wait for Cinemachine blend
        //yield return new WaitForSeconds(blendDuration);

        // Enable gameplay
        SetGameplayEnabled(true);
        onCutsceneEnded?.Invoke();
    }

    private void SetGameplayEnabled(bool enabled)
    {
        player.Input.currentActionMap?.Enable();
        Destroy(eyelids);
        playerBody.enabled = true;
        healthBarAnimator.SetTrigger("StartHealth");
        StartCoroutine(DisableHealthAnimator());
    }

    private IEnumerator DisableHealthAnimator()
    {
        yield return new WaitForSeconds(2.0f);
        healthBarAnimator.enabled = false;
    }

    private void PlayCutsceneSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            GameAudio.PlayOneShot(audioSource, clip);
    }
}

