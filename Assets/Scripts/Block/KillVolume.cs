using UnityEngine;

public class KillVolume : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<DeathBehavior>() != null)
        {
            other.gameObject.GetComponent<DeathBehavior>().KillActor();
        }
    }
}
