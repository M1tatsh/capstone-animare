using UnityEngine;

public class DoorScript : MonoBehaviour
{
    public Transform door;
    public Vector3 spawnPointOffset;

    void Start()
    {
        
    }


    void Update()
    {
        
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Player")
        {
            if (Input.GetButton("Jump") && door != null)
            {
                other.transform.position = door.transform.position + spawnPointOffset;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (door == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(door.transform.position + spawnPointOffset, 0.5f);
    }
}
