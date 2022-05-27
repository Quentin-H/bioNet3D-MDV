using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Material))]
public class Billboard : MonoBehaviour
{
    public float scale = 1.0f;
    public float yawAngleOffset = 0.0f;
    private Camera sceneCamera;
    private Vector3 initialScale; 

    public bool screenspace;

    private void Awake() 
    {
        sceneCamera = Camera.main;
        initialScale = transform.localScale * scale;

        if (!screenspace) 
        {
            transform.localScale = new Vector3(scale, scale, scale);
        } 
    }

    private void Update()
    {
        transform.LookAt(sceneCamera.transform);
        transform.Rotate(0f, 0f, yawAngleOffset);

        if (screenspace) 
        {
            Plane plane = new Plane(sceneCamera.transform.forward, sceneCamera.transform.position); 
		    float dist = plane.GetDistanceToPoint(transform.position);
		    transform.localScale = initialScale * dist; 
        } 
    }
}