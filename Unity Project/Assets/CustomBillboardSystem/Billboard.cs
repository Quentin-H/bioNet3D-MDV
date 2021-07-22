using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Material))]
public class Billboard : MonoBehaviour
{
    public float scale = 1.0f;
    public float yawAngleOffset = 0.0f;
    //public float maxScale = 500.0f;
    private Camera camera;
    private Vector3 initialScale; 

    private void Awake() 
    {
        camera = Camera.main;
        initialScale = transform.localScale * scale; 
    }

    private void Update()
    {
        transform.LookAt(camera.transform);
        transform.Rotate(0f, 0f, yawAngleOffset);

        Plane plane = new Plane(camera.transform.forward, camera.transform.position); 
		float dist = plane.GetDistanceToPoint(transform.position);
        //if ( (dist * scale) > maxScale) { transform.localScale = initialScale * maxScale; } 
		transform.localScale = initialScale * dist; 
    }
}