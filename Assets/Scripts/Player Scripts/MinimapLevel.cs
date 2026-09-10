/*
 * Author: Lam Nguyen
 * Created: 9/10/2026
 */

using UnityEngine;

public class MinimapLevel : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private GameObject firstFloorMinimap;
    [SerializeField] private GameObject secondFloorMinimap;
    [SerializeField] private float yLevel = 9.5f;

    private void Start()
    {
        UpdateMinimap();
    }

    private void Update()
    {
        UpdateMinimap();
    }

    // Checks to see if the player is above a certain y-level,
    // switches the floors accordingly if they aren't already switched
    private void UpdateMinimap()
    {
        if (player == null || firstFloorMinimap == null || secondFloorMinimap == null)
            return;

        bool isSecondFloor = player.position.y > yLevel;

        if (firstFloorMinimap.activeSelf != !isSecondFloor)
            firstFloorMinimap.SetActive(!isSecondFloor);

        if (secondFloorMinimap.activeSelf != isSecondFloor)
            secondFloorMinimap.SetActive(isSecondFloor);
    }
}
