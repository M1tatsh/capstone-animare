using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RotateCamera : MonoBehaviour
{
    private Rigidbody rb;
    private PlayerCollision collision;

    public float rotAmount = 90f;
    public float smoothTime = 1f;
    public bool disable = false;

    private Vector3 yRot = Vector3.up;
    private Vector3 vel = new Vector3(0, 0, 2);
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        collision = rb.GetComponent<PlayerCollision>(); 
    }

    void Update()
    {
        if (Input.GetAxis("Fire2") > 0 && disable == false)
        {
            StartCoroutine(StartRotation(smoothTime, 1));
        }
        else if (Input.GetAxis("Fire2") < 0 && disable == false)
        {
            StartCoroutine(StartRotation(smoothTime, -1));
        }
    }

    IEnumerator StartRotation(float time, int f)
    {
        Quaternion targRot = Quaternion.Euler(0, transform.rotation.eulerAngles.y + (rotAmount * f), 0);

        disable = true;
        transform.rotation = Quaternion.Slerp(transform.rotation, targRot, time);
        print(transform.rotation);
        yield return new WaitForSeconds(time);
        disable = false;
    }
}
