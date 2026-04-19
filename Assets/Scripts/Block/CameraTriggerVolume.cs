using UnityEngine;

public class CameraTriggerVolume : MonoBehaviour
{
    [HideInInspector] public enum Axis
    {
        X,
        Z
    }

    public Axis axis = Axis.X;

    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<PlayerMovement>() != null && other.GetComponent<PlayerRotationHandler>().CheckAxis(axis))
        {
            other.GetComponent<PlayerRotationHandler>().SetRotation();
        }
    }
}
