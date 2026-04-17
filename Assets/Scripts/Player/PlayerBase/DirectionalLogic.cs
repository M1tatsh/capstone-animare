using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DirectionalLogic : MonoBehaviour
{

    public int DirectionCalc(float rotY)
    {
        int dirOut = 0;

        if (rotY == 0) dirOut = 0;
        else if (rotY == 90) dirOut = 1;
        else if (rotY == -90) dirOut = 2;
        else if (rotY == -180) dirOut = 3;

        return dirOut;
    }
}
