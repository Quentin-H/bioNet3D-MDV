using UnityEngine;
//using UnityEngine.rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Material))]
public class Billboard : MonoBehaviour
{
    public float scale = 1.0f;
    private Camera camera;
    private Vector3 initialScale; 

    private void Awake() 
    {
        camera = Camera.main;
        initialScale = transform.localScale; 
    }

    private void Update()
    {
        transform.LookAt(camera.transform);

        Plane plane = new Plane(camera.transform.forward, camera.transform.position); 
		float dist = plane.GetDistanceToPoint(transform.position); 
		transform.localScale = initialScale * dist * scale; 
    }
}