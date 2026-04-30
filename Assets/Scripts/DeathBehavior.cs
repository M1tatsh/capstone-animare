using UnityEngine;

public class DeathBehavior : MonoBehaviour
{
    Transform startPosition = null;

    private void Start()
    {
        startPosition = transform;
    }
    public void KillActor()
    {
        Debug.Log(gameObject.name + " Killed");

        transform.position = startPosition.position;
        transform.rotation = startPosition.rotation;
    }
}
