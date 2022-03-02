using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;


public class CameraControls : MonoBehaviour
{
    Transform camParent;

    void Awake()
    {
        camParent = this.transform.parent;
    }

    // Update is called once per frame
    void Update()
    {
        float3 rotation = new float3();

        if (camParent.eulerAngles.x < 90f && camParent.eulerAngles.x > -90f)
        {
            rotation = GetBaseInput();
        } 

        camParent.Rotate(rotation, Space.Self);
    }

    private Vector3 GetBaseInput() // helper for moveCamera
    {
        Vector3 velocity = new Vector3();

        // Forwards
        if (Input.GetKey(KeyCode.W))
            velocity += new Vector3(1f, 0, 0);

        // Backwards
        if (Input.GetKey(KeyCode.S))
            velocity += new Vector3(1f, 0, 0);

        // Left
        if (Input.GetKey(KeyCode.A))
            velocity += new Vector3(0, 1f, 0);

        // Right
        if (Input.GetKey(KeyCode.D))
            velocity += new Vector3(0, 1f, 0);

        return velocity;
    }
}
