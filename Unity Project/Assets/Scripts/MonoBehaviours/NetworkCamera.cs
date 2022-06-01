using System.Collections;
using System.Collections.Generic;

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



public class NetworkCamera : MonoBehaviour
{
    [SerializeField] private Camera cam;

    //Node Click Variables
    const float RAYCAST_DISTANCE = 100000;
    PhysicsWorld physicsWorld => World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;
    EntityManager entityManager => World.DefaultGameObjectInjectionWorld.EntityManager;
    private Entity selectedEntity;

    [SerializeField] private Transform camOriginParent;
    [SerializeField] private Transform camLookAtParent;

    [SerializeField] private GameObject highlightedBillboard;
    [HideInInspector] public List<GameObject> highlightBillboardObjects = new List<GameObject>();
    [SerializeField] public Toggle highlightShowHideUI;
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
    [SerializeField] private NodeObjectScaling nodeScaleManager;
    [SerializeField] private ECSBillboardManager ecsBillboardManager;
    
    // Fly Cam Variables
    public float mainSpeed = 10.0f;   // Default speed
    public float shiftAdd  = 25.0f;   // Amount to accelerate when shift is pressed
    public float maxShift  = 100.0f;  // Maximum speed when holding shift
    public float camSens   = 0.15f;   // Mouse sensitivity
    private Vector3 lastMouse = new Vector3(255, 255, 255);  // middle of the screen, rather than at the top (play)
    private float totalRun = 1.0f;



    private void Start() 
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) 
        {
            SendRay();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            FocusOnSelectedNode();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            ScreenCapture.CaptureScreenshot("C:/Users/Quentin/Desktop/MDV_Screencap.png", 1);
        }
    }

    private void SendRay()
    {
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
            SelectNode(entity);
        }
    }

    public void SelectNode(Entity entity) 
    {
        ecsBillboardManager.HighlightSelectedNode(entity);
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

    public void SearchForNode(string query)
    {        
        try 
        {
            SelectNode(sceneManager.FindNode(query.Trim()));
            FocusOnSelectedNode();
        } catch { Debug.Log("Node not found (" + query.Trim() + ")"); }
    }

    // called by search bar
    public void FocusOnSelectedNode()
    {
        if (nodeSelected == true)
        {
            //When you get a position of an entity it returns a 4 dimensional coordinate, I don't know why
            float4 entityPos4 = entityManager.GetComponentData<LocalToWorld>(selectedEntity).Value[3];
            float3 entityPos = new float3(entityPos4.x, entityPos4.y, entityPos4.z);

            camOriginParent.transform.LookAt(entityPos, Vector3.right);

            camOriginParent.eulerAngles = new float3(
                camOriginParent.eulerAngles.x,
                camOriginParent.eulerAngles.y,
                0);
            float distance = Vector3.Distance(Vector3.zero, entityPos);
            float3 currentPos = cam.transform.localPosition;
            currentPos.z = distance + 10f;
            cam.transform.localPosition = currentPos;
        }
    }

    public Entity GetSelectedEntity() 
    {
        return selectedEntity;
    }

    public void SetHighlightedNodes(List<Entity> toBeHighlighted) 
    {
        ClearHighlights();

        foreach (Entity curEntity in toBeHighlighted)
        {
            float3 entityPos = new float3(
                entityManager.GetComponentData<Translation>(curEntity).Value.x, 
                entityManager.GetComponentData<Translation>(curEntity).Value.y, 
                entityManager.GetComponentData<Translation>(curEntity).Value.z);

            GameObject newObject = Instantiate(highlightedBillboard, entityPos, Quaternion.identity);
            newObject.transform.localScale *= nodeScaleManager.nodeScale;
            highlightBillboardObjects.Add(newObject);
        }
        highlightShowHideUI.isOn = true;
    }

    public void ClearHighlights()
    {
        foreach (GameObject curObject in highlightBillboardObjects)
        {
            Destroy(curObject);
        }
    }

    // called by UI
    private bool showingHighlights = true;
    public void ShowHideHighlights()
    {
        if (showingHighlights == true)
        {
            foreach (GameObject curObject in highlightBillboardObjects)
            {
                try { curObject.SetActive(false); } catch { }
            }
            showingHighlights = false;
        } 
        else if (showingHighlights == false)
        {
           foreach (GameObject curObject in highlightBillboardObjects)
            {
                try { curObject.SetActive(true); } catch { }
            } 
            showingHighlights = true;
        }
    }

    public void SetViewAxis(int view) // if the user clicks a view option from the dropdown menu it will bring them to a zoomed out view from that axis
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