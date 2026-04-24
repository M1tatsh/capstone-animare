using UnityEngine;
using Unity.Cinemachine;

public class CameraRotator : MonoBehaviour
{
    float move = 0;
    public float moveSpeed;

    void Start()
    {
        move = GetComponent<CinemachineOrbitalFollow>().HorizontalAxis.Value;
    }

    void FixedUpdate()
    {
        MoveHorizontal(moveSpeed);
    }

    private void MoveHorizontal(float speed)
    {
        GetComponent<CinemachineOrbitalFollow>().HorizontalAxis.Value += (move + moveSpeed) * Time.deltaTime;

        if (GetComponent<CinemachineOrbitalFollow>().HorizontalAxis.Value >= 360f || GetComponent<CinemachineOrbitalFollow>().HorizontalAxis.Value <= -360f)
            GetComponent<CinemachineOrbitalFollow>().HorizontalAxis.Value = 0f;
    }
}
