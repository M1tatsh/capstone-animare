using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockBase : MonoBehaviour
{
    [Header("Stats")]
    public float weight = 1f;
    const float CARRYABLE_WEIGHT = 1f;

    [Header("Debug Toggles")]
    public bool carryable = false;
    public bool connected = false;
    public bool wallSlideable = false;
    public bool hangable = false;
    public bool sleeping = true;
    public bool stayInPlace = false;

    [HideInInspector] public Rigidbody rb;
    public GameObject coupledBlock = null;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        carryable = weight < CARRYABLE_WEIGHT;
        connected = coupledBlock != null;
    }

    void Update()
    {
        
    }
}
