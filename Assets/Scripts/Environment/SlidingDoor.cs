/*
 * Author: Brian Delaney
 * Created: 10/22/2026
 */
using System;
using Unity.VisualScripting;
using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [SerializeField] private bool open = false;
    private float speed = 1f;
    private float openHeight = 4.2f;
    private Vector3 closedPosition;
    private Vector3 openPosition;

    void Start()
    {
        closedPosition = transform.localPosition;
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
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, doorPosition, speed * Time.deltaTime);
    }
}
