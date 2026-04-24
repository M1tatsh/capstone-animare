using UnityEngine;

public class CursorParallax : MonoBehaviour
{
    public float offsetMultiplier = 1f;
    public float smoothTime = 0.3f;

    private Vector2 startPosition;
    private Vector3 velocity;

    void Start()
    {
        startPosition = transform.localPosition;
        print(transform.localPosition);
    }


    void Update()
    {
        Vector2 offset = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, startPosition + (offset * offsetMultiplier), ref velocity, smoothTime);
    } 
}
