using UnityEngine;
using UnityEngine.InputSystem;

public class KeypadPointer : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 9/12/2026
     */

    [Header("Raycast")]
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private LayerMask keypadButtonLayer;
    [SerializeField, Min(0.1f)] private float maximumDistance = 5f;

    [Header("Input System")]
    [SerializeField] private InputActionReference clickAction;

    [Header("State")]
    [SerializeField] private bool interactionEnabled;

    private KeypadButton hoveredButton;

    private void Awake()
    {
        if (interactionCamera == null)
            interactionCamera = Camera.main;
    }

    //Update by tracking the mouse position and whether the raycast is highlighting over a keypad button, if it is then highlight the keypad. 
    private void Update()
    {
        if (!interactionEnabled || interactionCamera == null || Mouse.current == null)
        {
            ChangeHoveredButton(null);
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = interactionCamera.ScreenPointToRay(mousePosition);

        KeypadButton hitButton = null;
        if (Physics.Raycast(ray, out RaycastHit hit, maximumDistance,
                keypadButtonLayer, QueryTriggerInteraction.Collide))
        {
            Debug.Log( $"Keypad ray hit: {hit.collider.name} " + $"on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            hitButton = hit.collider.GetComponentInParent<KeypadButton>();
        }

        ChangeHoveredButton(hitButton);

        if (hoveredButton != null && clickAction != null &&
            clickAction.action.WasPressedThisFrame())
        {
            hoveredButton.Press();
        }
    }

    //Set the keypad interactions to enabled.
    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;

        if (!enabled)
            ChangeHoveredButton(null);
    }

    //Switch which button is being hovered over to correctly indicate to the player what they are selecting. 
    private void ChangeHoveredButton(KeypadButton nextButton)
    {
        if (hoveredButton == nextButton)
            return;

        if (hoveredButton != null)
            hoveredButton.SetHovered(false);

        hoveredButton = nextButton;

        if (hoveredButton != null)
            hoveredButton.SetHovered(true);
    }

    private void OnDisable()
    {
        ChangeHoveredButton(null);
    }
}
