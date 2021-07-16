using UnityEngine;
using System.Collections;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Entities;
using RaycastHit = Unity.Physics.RaycastHit;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NetworkCamera : MonoBehaviour
{
    public static NetworkSceneManager networkSceneManager;
    
    //Node Click Variables
    [SerializeField]
    Camera Cam = default;
    const float RAYCAST_DISTANCE = 1000;
    PhysicsWorld physicsWorld => World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
    EntityManager entityManager => World.DefaultGameObjectInjectionWorld.EntityManager;
    public static Entity selectedEntity;
    public GameObject selectedNodeUI;
    public Text nodeNameText;
    public Text nodeDescriptionText;
    public Text nodeNetworkRank;
    public Text nodeBaselineScore;
    public Text nodeDegreeText;
    public bool nodeSelected;

    // Fly Cam Variables
    public float mainSpeed = 10.0f;   // Default speed
    public float shiftAdd  = 25.0f;   // Amount to accelerate when shift is pressed
    public float maxShift  = 100.0f;  // Maximum speed when holding shift
    public float camSens   = 0.15f;   // Mouse sensitivity
    private Vector3 lastMouse = new Vector3(255, 255, 255);  // middle of the screen, rather than at the top (play)
    private float totalRun = 1.0f;
    public Text lockCameraText;
    public Text lockCursorText;
    private bool cameraLocked = false;
    private bool cursorLocked = false;


    private void Update()
    {
        if (cameraLocked == false)
        {
            moveCamera();
        }
    }

    private void LateUpdate()
    {
        selectNode();

        if (Input.GetKeyDown(KeyCode.F))
        {
            focusOnNode();
        }
       locking();
    }

    private void locking()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            cameraLocked = !cameraLocked;

            if (cameraLocked) { lockCameraText.text = "Press C to unlock camera"; }
            if (!cameraLocked) { lockCameraText.text = "Press C to lock camera"; }
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            cursorLocked = !cursorLocked;

            if (cursorLocked) 
            { 
                lockCursorText.text = "Press V to unlock cursor"; 
                Cursor.lockState = CursorLockMode.None;
                Cursor.lockState = CursorLockMode.Locked;
            }
            if (!cursorLocked) 
            { 
                lockCursorText.text = "Press V to lock cursor"; 
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }

    private void moveCamera()
    {
        //replace with a better fps mouselook
        lastMouse = Input.mousePosition - lastMouse;
        lastMouse = new Vector3(-lastMouse.y * camSens, lastMouse.x * camSens, 0);
        lastMouse = new Vector3(transform.eulerAngles.x + lastMouse.x, transform.eulerAngles.y + lastMouse.y, 0);
        transform.eulerAngles = lastMouse;
        lastMouse = Input.mousePosition;
        //

        Vector3 p = GetBaseInput();
        if (Input.GetKey(KeyCode.LeftShift))
        {
            totalRun += Time.deltaTime;
            p *= totalRun * shiftAdd;
            p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
            p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
            p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
        }
        else
        {
            totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
            p *= mainSpeed;
        }

        p *= Time.deltaTime;
        transform.Translate(p);
    }

    private Vector3 GetBaseInput()
    {
        Vector3 p_Velocity = new Vector3();

        // Forwards
        if (Input.GetKey(KeyCode.W))
            p_Velocity += new Vector3(0, 0, 1);

        // Backwards
        if (Input.GetKey(KeyCode.S))
            p_Velocity += new Vector3(0, 0, -1);

        // Left
        if (Input.GetKey(KeyCode.A))
            p_Velocity += new Vector3(-1, 0, 0);

        // Right
        if (Input.GetKey(KeyCode.D))
            p_Velocity += new Vector3(1, 0, 0);

        // Up
        if (Input.GetKey(KeyCode.Space))
            p_Velocity += new Vector3(0, 1, 0);

        // Down
        if (Input.GetKey(KeyCode.LeftControl))
            p_Velocity += new Vector3(0, -1, 0);

        return p_Velocity;
    }

    private void selectNode()
    {
        if (Cam == null || !Input.GetMouseButtonDown(0) || EventSystem.current.IsPointerOverGameObject()) return;

        var position = Input.mousePosition;
        var screenPointToRay = Cam.ScreenPointToRay(position);
        var rayInput = new RaycastInput
        {
            Start = screenPointToRay.origin,
            End = screenPointToRay.GetPoint(RAYCAST_DISTANCE),
            Filter = CollisionFilter.Default
        };

        if (!physicsWorld.CastRay(rayInput, out RaycastHit hit) && !EventSystem.current.IsPointerOverGameObject()) 
        {
            nodeSelected = false;
            selectedNodeUI.SetActive(false);
            return;
        }

        selectedEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;
        nodeSelected = true;
        
        nodeNameText.text = "Node Name: " + entityManager.GetComponentData<NodeData>(selectedEntity).displayName;
        nodeDescriptionText.text = "Description: " + entityManager.GetComponentData<NodeData>(selectedEntity).description;
        nodeNetworkRank.text = "Network Rank: " + entityManager.GetComponentData<NodeData>(selectedEntity).networkRank;
        nodeBaselineScore.text = "Baseline Value: " + entityManager.GetComponentData<NodeData>(selectedEntity).baselineScore;
        nodeDegreeText.text = "Degree: " + entityManager.GetComponentData<NodeData>(selectedEntity).degree;

        selectedNodeUI.SetActive(true);
    }

    private void focusOnNode()
    {
        // move camera to the node with an ofset, angle the camera properly, check if colliding with node, if so keep moving the camera until no longer colliding
        /*
        Translation translation = new Translation()
        {
            entityManager.GetComponentData<Translation>(selectedEntity)        
        };
        */
    }
}