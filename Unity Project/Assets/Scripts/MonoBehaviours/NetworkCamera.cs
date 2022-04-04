using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Physics;
using Unity.Physics.Systems;
using RaycastHit = Unity.Physics.RaycastHit;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using NodeViz; // not necessary

public class NetworkCamera : MonoBehaviour
{
    [HideInInspector] [SerializeField] private Camera cam;

    //Node Click Variables
    const float RAYCAST_DISTANCE = 100000;
    PhysicsWorld physicsWorld => World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
    EntityManager entityManager => World.DefaultGameObjectInjectionWorld.EntityManager;
    private Entity selectedEntity;
    [SerializeField] private Transform camParent;
    [SerializeField] private GameObject selectedNodeUI;
    [SerializeField] private Text nodeNameText;
    [SerializeField] private Text nodeDescriptionText;
    [SerializeField] private Text nodeNetworkRank;
    [SerializeField] private Text nodeBaselineScore;
    [SerializeField] private Text nodeDegreeText;
    [SerializeField] private Text nodeClusterText;
    private bool nodeSelected;
    [SerializeField] private Dropdown viewAxisDropdown;
    [SerializeField] private NetworkSceneManager sceneManager;
    
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


    private void Start() 
    {
        cam = Camera.main;
        //this.x + (distanceFromOrigin(node[0]) * 1.75)
    }

    private void Update()
    {
        if (cameraLocked == false)
        {
            //moveCamera();
        }

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) {
            sendRay();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            focusOnSelectedNode();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            ScreenCapture.CaptureScreenshot("C:/Users/Quentin/Desktop/MDV_Screencap.png", 1);
        }

        locking();
    }

    // this takes care of checking key presses for the various locking features the application has
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

        Vector3 p = getBaseInput();
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

    private Vector3 getBaseInput() // helper for moveCamera
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

    private void sendRay()
    {
        // Sanity checks, checking if the mouse is over a UI element, checking if left mouse button isn't clicked
        //if (cam == null || !Input.GetMouseButtonDown(0) || EventSystem.current.IsPointerOverGameObject()) return; 
        //if (EventSystem.current.IsPointerOverGameObject()) return;

        // sends a ray through the scene
        var position = Input.mousePosition;
        var screenPointToRay = cam.ScreenPointToRay(position);
        var rayInput = new RaycastInput
        {
            Start = screenPointToRay.origin,
            End = screenPointToRay.GetPoint(RAYCAST_DISTANCE),
            Filter = CollisionFilter.Default
        };

        // if nothing is hit, nothing is selected, this also effectively deselects a selected entity if one was selected before
        if (!physicsWorld.CastRay(rayInput, out RaycastHit hit) && !EventSystem.current.IsPointerOverGameObject())
        {
            nodeSelected = false;
            selectedNodeUI.SetActive(false);
        } else {
            Entity entity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;
            selectNode(entity);
        }
    }

    public void selectNode(Entity entity) 
    {
        nodeSelected = true;
        selectedEntity = entity;
        NodeData selectedNodeData = entityManager.GetComponentData<NodeData>(entity);
        
        // sets the node ui panel to the attributes the selected node has
        nodeNameText.text = "Node Name: " + selectedNodeData.displayName;
        nodeDescriptionText.text = "Description: " + selectedNodeData.description;
        nodeNetworkRank.text = "Network Rank: " + selectedNodeData.networkRank;
        nodeBaselineScore.text = "Baseline Value: " + selectedNodeData.baselineScore;
        nodeDegreeText.text = "Degree: " + selectedNodeData.degree;
        nodeClusterText.text = "Cluster: " + selectedNodeData.cluster;

        selectedNodeUI.SetActive(true); 
    }   

    public void searchForNode(string query)
    {        
        try 
        {
            selectNode(sceneManager.FindNode(query));
            focusOnSelectedNode();
        } catch { Debug.Log("Node not found (" + query.Trim() + ")"); }
    }

    // called by search bar
    public void focusOnSelectedNode()
    {
        if (nodeSelected == true)
        {
            //When you get a position of an entity it returns a 4 dimensional coordinate, I don't know why
            float4 entityPos4 = entityManager.GetComponentData<LocalToWorld>(selectedEntity).Value[3];
            float3 entityPos = new float3(entityPos4.x, entityPos4.y, entityPos4.z);

            //find distance between node and orange
            // make camera parent look at node
            // set z of camera as subtracting distance + 10
            camParent.transform.LookAt(entityPos, Vector3.right);

            camParent.eulerAngles = new float3(
                camParent.eulerAngles.x,
                camParent.eulerAngles.y,
                0);
            float distance = Vector3.Distance(Vector3.zero, entityPos);
            float3 currentPos = cam.transform.localPosition;
            currentPos.z = distance + 10f;
            cam.transform.localPosition = currentPos;
        }
    }

    public Entity getSelectedEntity() 
    {
        return selectedEntity;
    }

    // called by UI
    public void setViewAxis(int view) // if the user clicks a view option from the dropdown menu it will bring them to a zoomed out view from that axis
    {
        if (view == 0) { return; } // if the user chooses the blank option return because we don't want anything to happen
        if (view == 1) //x 
        {
            cam.transform.position = new float3(1650, 290, 535);
            cam.transform.eulerAngles = new float3(0, -90, 0);
        }
        if (view == 2) //y
        {
            cam.transform.position = new float3(275, 1700, 100);
            cam.transform.eulerAngles = new float3(90, 0, 0);
        }
        if (view == 3) //z
        {
            cam.transform.position = new float3(430, 620, -990);
            cam.transform.eulerAngles = new float3(0, 0, 0);
        }
        if (view == 4) //-x
        {
            cam.transform.position = new float3(-830, 290, 535);
            cam.transform.eulerAngles = new float3(0, 90, 0);
        }
        if (view == 5) //-y
        {
            cam.transform.position = new float3(500, -900, 600);
            cam.transform.eulerAngles = new float3(-90, 0, 0);
        }
        if (view == 6) //-z
        {
            cam.transform.position = new float3(645, 175, 1650);
            cam.transform.eulerAngles = new float3(0, 180, 0);
        }
        // set the value of the dropdown menu back to blank after moving the camera to the user's desired camera angle/axis
        viewAxisDropdown.value = 0;
    }
}