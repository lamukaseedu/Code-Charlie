using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
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

    [SerializeField] private MeshRenderer playerBody;

    [SerializeField] private Animator eyeAnimator;

    [SerializeField] private GameObject eyelids;

    [SerializeField] private Volume volume;

    [SerializeField] private Animator healthBarAnimator;

    private PlayerMinigameContext player;

    private Vignette vignette;

    private DepthOfField DOF;

    private IEnumerator Start()
    {
        player = PlayerMinigameContext.Instance;
        // Disable gameplay
        player.Input.currentActionMap?.Disable();

        playerBody.enabled = false;

        // Set initial camera priorities
        wakeUpCamera.Priority = 20;
        gameplayCamera.Priority = 10;

        // Wait before opening eyes
        yield return new WaitForSeconds(initialWait);

        // TODO: Play blinking animation here
        eyeAnimator.SetTrigger("open");
        yield return new WaitForSeconds(3.5f);
        yield return new WaitForSeconds(transitionWait);

        // Switch to gameplay camera
        gameplayCamera.Priority = 20;
        wakeUpCamera.Priority = 10;

        float elapsedTime = 0f;
        float startVignetteIntensity = 0f;
        float startDOFIntensity = 0f;

        if (volume.profile.TryGet<Vignette>(out vignette)) {
            startVignetteIntensity = vignette.intensity.value;
        }

        if (volume.profile.TryGet<DepthOfField>(out DOF))
        {
            startDOFIntensity = DOF.focalLength.value;
        }

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
}

