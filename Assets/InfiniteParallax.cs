using UnityEngine;

public class AutoScrollParallax : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 2f;
    private float spriteWidth;
    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
        // Measures actual width even if Draw Mode is set to Tiled
        spriteWidth = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    private void Update()
    {
        // Continuously moves left and loops seamlessly using Mathf.Repeat
        float newPosition = Mathf.Repeat(Time.time * scrollSpeed, spriteWidth);
        transform.position = startPosition + Vector3.left * newPosition;
    }
}
