using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraFollow : MonoBehaviour
{
    public GameObject FollowTarget;
    public float followSpeed;
    public float followHeight = 2.0f;

    void Update()
    {
        float targX = FollowTarget.transform.position.x;
        float targY = FollowTarget.transform.position.y;

        float currX = transform.position.x;
        float currY = transform.position.y;

        currX += (targX - currX) + followSpeed * Time.deltaTime;
        currY += (targY - currY) + followSpeed * Time.deltaTime;

        transform.position = new Vector3
            (
                currX,
                currY + followHeight,
                transform.position.z
            );
    }

}
