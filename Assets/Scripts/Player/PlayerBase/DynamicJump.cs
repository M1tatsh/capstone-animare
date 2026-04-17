using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicJump : MonoBehaviour
{
    private Rigidbody rb;
    public float fallMult = 2.5f;
    public float shortHopMult = 2f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMult - 1) * Time.deltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump")) //character is ascending while player isn't holding jump button
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (shortHopMult - 1) * Time.deltaTime;
        }
    }
}
