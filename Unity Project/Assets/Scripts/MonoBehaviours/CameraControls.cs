using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;


public class CameraControls : MonoBehaviour
{
    Transform camParent;
    float sensitivity = 1f;

    void Awake()
    {
        camParent = this.transform.parent;
    }

    void Update()
    {
        sensitivity += GetSensitivityInput();

        float3 rotation = new float3();

        //if (camParent.eulerAngles.x < 90f && camParent.eulerAngles.x > -90f)
        //{
            rotation = GetRotationInput();
        //} 
        
        camParent.Rotate(rotation, Space.Self);

        camParent.eulerAngles = new float3(
            camParent.eulerAngles.x,
            camParent.eulerAngles.y,
            0);

        transform.Translate(Vector3.forward * GetZoomInput());
    }

    private Vector3 GetRotationInput()
    {
        Vector3 rotation = new Vector3();

        // Forwards
        if (Input.GetKey(KeyCode.W))
            rotation += new Vector3(1f, 0, 0);

        // Backwards
        if (Input.GetKey(KeyCode.S))
            rotation += new Vector3(-1f, 0, 0);

        // Left
        if (Input.GetKey(KeyCode.A))
            rotation += new Vector3(0, 1f, 0);

        // Right
        if (Input.GetKey(KeyCode.D))
            rotation += new Vector3(0, -1f, 0);

        return rotation;
    }

    private float GetSensitivityInput()
    {
        float sensitivityInput = 1f;
        const float sensitivityInputSensitivity = .01f;

        if (Input.GetKey(KeyCode.KeypadPlus))
            sensitivityInput += sensitivityInputSensitivity;

        if (Input.GetKey(KeyCode.Minus))
            sensitivityInput += sensitivityInputSensitivity;

        return sensitivityInput;
    }

    private float GetZoomInput()
    {
        float zoomInput = 0;

        if (Input.GetKey(KeyCode.LeftShift))
            zoomInput += 0.1f; // maybe these should be equal to sensitivity

        if (Input.GetKey(KeyCode.LeftControl))
            zoomInput -= 0.1f;

        return zoomInput;
    }
}