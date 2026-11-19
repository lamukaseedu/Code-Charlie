
using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    [SerializeField] private float maxStamina = 1f;
    [SerializeField] private float drainRate = 0.25f;
    [SerializeField] private float regenRate = 0.25f;
    [SerializeField] private float regenDelay = 2f;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Slider stamina;
    private float staminaVal;
    private float regenTimer;

    public float CurrentStamina => staminaVal;
    public static bool HasStamina;

    private void Start()
    {
        staminaVal = maxStamina;
        stamina.maxValue = maxStamina;
        stamina.value = staminaVal;
    }

    private void Update()
    {
        if (staminaVal > 0)
        {
            HasStamina = true;
        }
        else HasStamina = false;
        if (playerMovement.IsSprinting && HasStamina)
        {
            staminaVal -= drainRate * Time.deltaTime;
            regenTimer = 0f;
        }
        else
        {
            // Count time spent not sprinting
            regenTimer += Time.deltaTime;

            // Regenerate after two seconds.
            if (regenTimer >= regenDelay)
            {
                staminaVal += regenRate * Time.deltaTime;
            }
        }

        // Keep stamina within its allowed range.
        staminaVal = Mathf.Clamp(staminaVal, 0f, maxStamina);
        stamina.value = staminaVal;
    }

    public void UseStamina(float amount)
    {
        staminaVal -= amount;
        staminaVal = Mathf.Clamp(staminaVal, 0f, maxStamina);

        // Restart the regeneration delay.
        regenTimer = 0f;
    }

    public bool HasEnoughStamina(float amount)
    {
        return staminaVal >= amount;
    }
}
