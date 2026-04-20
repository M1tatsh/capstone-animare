using UnityEngine;

public class DeathBehavior : MonoBehaviour
{
    Vector3 startPosition = Vector3.zero;

    private void Start()
    {
        startPosition = transform.position;
    }
    public void KillActor()
    {
        transform.position = startPosition;
    }
}
