using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraFollow : MonoBehaviour
{
    public GameObject FollowTarget;
    public float followSpeed;
    public float followHeight = 2.0f;
    public float smoothTime = 1f;
    public Vector3 vel = new Vector3(0, 0, 2);sss
    private Vector3 targetPosition;

    void FixedUpdate()
    {
        float targX = FollowTarget.transform.position.x;
        float targY = FollowTarget.transform.position.y;

        float currX = transform.position.x;
        float currY = transform.position.y;

        currX += (targX - currX) + followSpeed * Time.deltaTime;
        currY += (targY - currY) + followSpeed * Time.deltaTime;

        

        targetPosition = new Vector3
            (
                currX,
                currY + followHeight,
                transform.position.z
            );

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref vel, smoothTime);
    }

}
