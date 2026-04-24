using UnityEngine;
using Unity.Cinemachine;

public class CameraRegister : MonoBehaviour
{
    private void OnEnable()
    {
        CameraManager.RegisterCamera(GetComponent<CinemachineCamera>());
    }

    private void OnDisable()
    {
        CameraManager.UnregisterCamera(GetComponent<CinemachineCamera>());
    }
}
