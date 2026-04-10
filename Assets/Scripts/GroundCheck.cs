using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    [SerializeField] public Collider col;
    private GameObject obj;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.layer == 6)
        {

        }
    }
}
