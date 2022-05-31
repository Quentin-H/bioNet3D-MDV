using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Material))]
public class Billboard : MonoBehaviour
{
    public float initialScale = 1.0f;
    public float yawAngleOffset = 0.0f;
    private Camera sceneCamera;
    private Vector3 initialScaleAsFloat3; 

    public bool screenspace;

    private void Awake() 
    {
        sceneCamera = Camera.main;
        initialScaleAsFloat3 = transform.localScale * initialScale;
        transform.localScale = initialScaleAsFloat3;
    }

    private void Update()
    {
        transform.LookAt(sceneCamera.transform);
        transform.Rotate(0f, 0f, yawAngleOffset);
    }
}