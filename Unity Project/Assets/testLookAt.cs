using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testLookAt : MonoBehaviour
{
    public Transform target;
    public Vector3 vector;

    void Update()
    {
        transform.LookAt(target, vector);
        transform.eulerAngles  = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
    }
}
