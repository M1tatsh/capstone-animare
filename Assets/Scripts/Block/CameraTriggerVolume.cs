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
        if(other.GetComponent<RotationHandler>() != null && other.GetComponent<RotationHandler>().CheckAxis(axis))
        {
            other.GetComponent<RotationHandler>().SetRotation();
        }
    }
}
