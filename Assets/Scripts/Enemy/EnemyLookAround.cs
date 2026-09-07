using UnityEngine;

public class EnemyLookAround : MonoBehaviour
{
    /*
     * Author: ANDRES RONDON-VILLARMOSA
     * Created: 9/1/2026
     */

    private Transform playerTarget;
    public bool verticalLook;

    //Initiate script by finding the player object and setting the target to the player transform.
    private void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError(
                "Enemy could not find a GameObject with the Player tag.",
                this
            );
        } else
        {
            playerTarget = player.transform;
        }
    }
    //Update by constantly tracking where the player target is.
    void Update()
    {
        if (verticalLook)
        {
            transform.LookAt(playerTarget);
        } else
        {
            Vector3 changeDirection = playerTarget.position;
            changeDirection.y = transform.position.y;


            transform.LookAt(changeDirection);
        }
    }
}
