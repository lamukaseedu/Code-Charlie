using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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

    [Header("Credential Field Options")]
    [SerializeField] private bool prefillEmployeeID = false;
    [SerializeField] private bool lockEmployeeID = false;

    [SerializeField] private bool prefillAccessKey = false;
    [SerializeField] private bool lockAccessKey = false;


    [Header("Status Messages")]
    [SerializeField] private string defaultMessage = "LOGIN";
    [SerializeField] private string successMessage = "ACCESS GRANTED";
    [SerializeField] private string failureMessage = "ACCESS DENIED";

    [Header("Timing")]
    [SerializeField] private float failureDisplayDuration = 1.5f;

    [Header("Login Events")]
    [SerializeField] private UnityEvent onLoginSuccess;
    [SerializeField] private UnityEvent onLoginFailure;

    [Header("Keyboard Shortcut Options")]
    [SerializeField] private InputActionReference navigateLoginAction;
    [SerializeField] private InputActionReference submitLoginAction;

    private bool isLoggingIn;
    private Coroutine failureCoroutine;

    private void OnEnable()
    {
        navigateLoginAction.action.performed += OnNavigateLogin;
        submitLoginAction.action.performed += OnSubmitLogin;
    }

    private void OnDisable()
    {
        navigateLoginAction.action.performed -= OnNavigateLogin;
        submitLoginAction.action.performed -= OnSubmitLogin;
    }
    private void Start()
    {
        InitializeLoginFields();
        ResetStatus();
        loginButton.onClick.AddListener(AttemptLogin);
    }

    private void InitializeLoginFields()
    {
        if (prefillEmployeeID)
            employeeIDInput.SetTextWithoutNotify(correctEmployeeID);
        
        if (prefillAccessKey)
            accessKeyInput.SetTextWithoutNotify(correctAccessKey);

        employeeIDInput.readOnly = lockEmployeeID;
        accessKeyInput.readOnly = lockAccessKey; 
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

    private void OnNavigateLogin(InputAction.CallbackContext context)
    {
        if (isLoggingIn)
            return;
        bool canEditID = !employeeIDInput.readOnly;
        bool canEditKey = !accessKeyInput.readOnly;

        if (!canEditID && !canEditKey)
            return;

        if (canEditID && !canEditKey)
        {
            employeeIDInput.Select();
            employeeIDInput.ActivateInputField();
            return;
        }
        if (!canEditID && canEditKey)
        {
            accessKeyInput.Select();
            accessKeyInput.ActivateInputField();
            return;
        }
        if (employeeIDInput.isFocused)
        {
            accessKeyInput.Select();
            accessKeyInput.ActivateInputField();
        } else
        {
            employeeIDInput.Select();
            employeeIDInput.ActivateInputField();
        }
            
    }

    private void OnSubmitLogin(InputAction.CallbackContext context)
    {
        if (isLoggingIn)
            return;
        AttemptLogin();
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
