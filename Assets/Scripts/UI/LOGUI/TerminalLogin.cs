using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections;

public class TerminalLogin : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 10/20/2026
     */

    [Header("Login Fields")]
    [SerializeField] private TMP_InputField employeeIDInput;
    [SerializeField] private TMP_InputField accessKeyInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private TerminalLoginButton loginButtonVisuals;
    [SerializeField] private TMP_Text statusText;

    [Header("Credentials")]
    [SerializeField] private string correctEmployeeID = "PX-0427";
    [SerializeField] private string correctAccessKey = "ORPHEUS";

    [Header("Status Messages")]
    [SerializeField] private string defaultMessage = "LOGIN";
    [SerializeField] private string successMessage = "ACCESS GRANTED";
    [SerializeField] private string failureMessage = "ACCESS DENIED";

    [Header("Timing")]
    [SerializeField] private float failureDisplayDuration = 1.5f;

    [Header("Login Events")]
    [SerializeField] private UnityEvent onLoginSuccess;
    [SerializeField] private UnityEvent onLoginFailure;

    private bool isLoggingIn;
    private Coroutine failureCoroutine;

    private void Start()
    {
        ResetStatus();
        loginButton.onClick.AddListener(AttemptLogin);
    }

    public void AttemptLogin()
    {
        if (isLoggingIn)
            return;

        string employeeID = employeeIDInput.text.Trim();
        string accessKey = accessKeyInput.text;

        if (employeeID == correctEmployeeID &&
            accessKey == correctAccessKey)
        {
            if (failureCoroutine != null)
            {
                StopCoroutine(failureCoroutine);
                failureCoroutine = null;
            }

            isLoggingIn = true;

            statusText.text = successMessage;
            loginButtonVisuals.ShowLoginResult(true);

            loginButton.interactable = false;
            employeeIDInput.interactable = false;
            accessKeyInput.interactable = false;

            onLoginSuccess?.Invoke();
        }
        else
        {
            if (failureCoroutine != null)
                StopCoroutine(failureCoroutine);

            failureCoroutine = StartCoroutine(ShowLoginFailure());
        }
    }

    private IEnumerator ShowLoginFailure()
    {
        statusText.text = failureMessage;
        loginButtonVisuals.ShowLoginResult(false);

        onLoginFailure?.Invoke();

        yield return new WaitForSeconds(failureDisplayDuration);

        ResetStatus();
        failureCoroutine = null;
    }

    private void ResetStatus()
    {
        statusText.text = defaultMessage;
        loginButtonVisuals.ClearLoginResult();
    }

    private void OnDestroy()
    {
        if (loginButton != null)
            loginButton.onClick.RemoveListener(AttemptLogin);
    }
}
