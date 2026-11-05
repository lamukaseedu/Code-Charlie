/*
 * Author: Brian Delaney
 * Created: 10/22/2026
 */
using System;
using Unity.VisualScripting;
using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [SerializeField] private Transform door;
    [SerializeField] private bool open = false;
    [SerializeField] private float speed = 200f;
    [SerializeField] private float openHeight = 300f;
    private Vector3 closedPosition;
    private Vector3 openPosition;

    void Start()
    {
        closedPosition = door.localPosition;
        openPosition = closedPosition + new Vector3(0, openHeight, 0);
    }

    void OnTriggerEnter(Collider other)
    {

        if (!open && (other.CompareTag("Player") || other.CompareTag("Stalker")))
        {
            open = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (open && (other.CompareTag("Player") || other.CompareTag("Stalker")))
        {
            open = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 doorPosition = open ? openPosition : closedPosition;
        door.localPosition = Vector3.MoveTowards(door.localPosition, doorPosition, speed * Time.deltaTime);
    }
}
