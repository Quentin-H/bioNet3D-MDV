using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Collections;
using NodeViz;

public class NetworkSceneManager : MonoBehaviour
{
    [HideInInspector]
    public static NetworkSceneManager instance;
    public NetworkCamera networkCamera;

    public GameObject nodePrefab;
    public Gradient nodeValueGradient;
    public Gradient edgeValueGradient;
    // If set to low number ex. 10, creates beautiful patterns on layouts besides fr3d
    // Figure out a way to automatically set this based on the layout so points aren't too close together
    public float positionMultiplier;
    private Entity nodeEntityPrefab;

    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;

    private GameObject inputDataHolder;
    private bool edgesShowing = false;
    public Button showHideEdgesButton;
    public Text edgeConversionProgressText;
    private int edgeConversionPercent;
    private int edgeConversionSteps;
    public GameObject top200Object;

    private IDictionary<FixedString32, Entity> sceneNodeEntities = new Dictionary<FixedString32, Entity>(); // This is for use internally liek creating edges, since internally genes are identified by feature IDs
    private IDictionary<String, Entity> sceneNodeEntitiesMappedToNames = new Dictionary<String, Entity>(); // This is for use with searching for nodes since the user would use names
    //maybe add dictionary with keys as coordinates if we want a feature that finds the connected node
    private Dictionary<Entity, List<NodeEdgePosition>> entitiesToEdges = new Dictionary<Entity, List<NodeEdgePosition>>();
    private List<GameObject> activeLines = new List<GameObject>();



    private void Start() 
    {
        if (instance != null && instance != this) 
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        inputDataHolder = GameObject.Find("InputDataHolder");

        try { positionMultiplier = inputDataHolder.GetComponent<DataHolder>().positionMultiplier; } catch { }

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        blobAssetStore = new BlobAssetStore();
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
        nodeEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(nodePrefab, settings);
        
        ConvertRawInputNodes(); // this has to complete before convert raw input edges
        //StartCoroutine(ConvertRawInputEdges());
        ConvertRawInputEdges();
    }

    private void OnDestroy() 
    {
        entityManager.DestroyEntity(entityManager.UniversalQuery);
        blobAssetStore.Dispose();
    }

    private void ConvertRawInputNodes() 
    {
        Debug.Log("started node conversion");

        string[] rawLayoutInputLines = new string[0];
        try { rawLayoutInputLines = inputDataHolder.GetComponent<DataHolder>().rawNodeLayoutFile.Split('\n'); } catch { }

        int i = 1;
        foreach(string line in rawLayoutInputLines) 
        {
            string fID = "";
            float3 coord = new float3(0,0,0);
            string dName = "";
            string desc = "";
            int nRank = 0;
            double blineScore = 0;
            int deg = 0;

            try { fID = line.Split('|')[0]; } catch { Debug.Log("Error parsing ID at line " + i); }
                
            try 
            {
                coord.x = float.Parse(line.Split('[')[1].Split(',')[0].Trim()) * positionMultiplier;
                coord.y = float.Parse(line.Split(',')[1].Split(',')[0].Trim()) * positionMultiplier;
                coord.z = float.Parse(line.Split(',')[2].Split(']')[0].Trim()) * positionMultiplier;
            } catch { Debug.Log("Error parsing coordinates at line " + i ); }

            try { dName = line.Split('|')[2]; } catch { Debug.Log( "Error parsing name at line " + i ); }
            try { desc = line.Split('|')[3]; } catch { Debug.Log( "Error parsing description at line " + i ); }
            try { nRank = int.Parse(line.Split('|')[4].Trim()); } catch { Debug.Log( "Error parsing network rank at line " + i ); }
            try { blineScore = double.Parse(line.Split('|')[5].Trim()); } catch { Debug.Log( "Error parsing baseline score at line " + i ); }
            try { deg = int.Parse(line.Split('|')[6].Trim()); } catch { Debug.Log( "Error parsing degree at line " + i ); }

            SpawnNode(fID, coord, dName, desc, nRank, blineScore, deg);

            i++;
        }
        Debug.Log("Done Spawning");
    }

    private void SpawnNode(string fID, float3 coord, string dName, string desc, int nRank, double blineScore, int deg)
    {
        if (fID == "" || fID == null)
        {
            return;
        }

        if (nRank <= 200)
        {
            Instantiate(top200Object, new Vector3(coord.x, coord.y, coord.z), Quaternion.identity);
        }

        Entity newNodeEntity = entityManager.Instantiate(nodeEntityPrefab);

        Translation translation = new Translation()
        {
            Value = coord
        };

        entityManager.AddComponentData(newNodeEntity, translation);

        Color evaluatedColor = nodeValueGradient.Evaluate( (float) blineScore);
        float4 colorF = new float4(evaluatedColor.r, evaluatedColor.g, evaluatedColor.b, evaluatedColor.a);
        
        /* This code is for converting the system to use material overrides rather than creating a new material for each node. 
        This should increase performance by a massive amount (probably increase fps 5-10 fold) and it is the correct "ECS" way of doing things, 
        however I could not get it working.

        MaterialColor mcc = new MaterialColor { Value = colorF };
        entityManager.AddComponentData<MaterialColor>(newNodeEntity, mcc); */

        var renderMesh = entityManager.GetSharedComponentData<RenderMesh>(newNodeEntity);
        var mat = new UnityEngine.Material(renderMesh.material);
        mat.SetColor("_UnlitColor", evaluatedColor);
        renderMesh.material = mat;
        entityManager.SetSharedComponentData(newNodeEntity, renderMesh);

        entityManager.SetComponentData(newNodeEntity, new NodeData { 
            featureID = fID, 
            displayName = dName, 
            description = desc, 
            networkRank = nRank,
            baselineScore = blineScore,
            degree = deg });

        FixedString32 idAsFixed = fID;
        sceneNodeEntities.Add(idAsFixed, newNodeEntity);

        sceneNodeEntitiesMappedToNames.Add(dName, newNodeEntity);
    }

    //IEnumerator ConvertRawInputEdges()
    private void ConvertRawInputEdges()
    {
        Debug.Log("Started edge conversion");

        string[] rawEdgeInputLines = new string[0];

        try { rawEdgeInputLines = inputDataHolder.GetComponent<DataHolder>().rawEdgeFile.Split('\n'); } catch { }

        foreach(string line in rawEdgeInputLines)  // convert this to parralel for job
        {
            if (String.IsNullOrWhiteSpace(line)) // if the line is blank it skips below. Otherwise the code will go to one of the catch statements but a wrong key error will occur in them.
            {
                continue;
            }

            string node1Name = "";
            float4 coord1 = new float4(0,0,0,0);
            float3 node1Coords = new float3(0,0,0);;
            string node2Name = "";
            float4 coord2 = new float4(0,0,0,0);
            float3 node2Coords = new float3(0,0,0);
            double edgeWeight = 0.0;

            try 
            {
                node1Name = line.Split()[0];
                coord1 =  entityManager.GetComponentData<LocalToWorld>(sceneNodeEntities[node1Name]).Value[3];
                node1Coords = new float3(coord1.x,coord1.y, coord1.z);

                node2Name = line.Split()[1];
                coord2 =  entityManager.GetComponentData<LocalToWorld>(sceneNodeEntities[node2Name]).Value[3];
                node2Coords =  new float3(coord2.x,coord2.y, coord2.z);

                edgeWeight = double.Parse(line.Split()[2]);
            } catch { }

            NodeEdgePosition newEdge1 = new NodeEdgePosition(new FixedString32(node1Name), node1Coords, new FixedString32(node2Name), node2Coords, edgeWeight);
            try
            {
                entitiesToEdges[sceneNodeEntities[node1Name]].Add(newEdge1);
            }
            catch
            {
                entitiesToEdges.Add(sceneNodeEntities[node1Name], new List<NodeEdgePosition>(){newEdge1});
            }

            NodeEdgePosition newEdge2 = new NodeEdgePosition(new FixedString32(node2Name), node2Coords, new FixedString32(node1Name), node1Coords, edgeWeight);
            try 
            {
                entitiesToEdges[sceneNodeEntities[node2Name]].Add(newEdge2);
            }
            catch
            {
                entitiesToEdges.Add(sceneNodeEntities[node2Name], new List<NodeEdgePosition>(){newEdge2});
            }

            //Debug.Log(newEdge1.nodeACoords + "|" + newEdge1.nodeBCoords + "|" + newEdge2.nodeACoords + "|" + newEdge2.nodeBCoords); This prints all zeros
            //Debug.Log(node1Coords + "|" + node2Coords);

            edgeConversionSteps++;
            edgeConversionProgressText.text = "Edge importation in progress. (" + String.Format("{0:0.00}", ((float)edgeConversionSteps / (float)rawEdgeInputLines.Length) * 100.0f) + "%)";
            //yield return null;
        }
        edgeConversionProgressText.gameObject.SetActive(false);
        Debug.Log("Done converting edges");
    }

    //  U      U    I
    //  U      U    I
    //  U      U    I
    //   UUUUUU     I
    public void showHideEdges()
    {
        Entity selectedEntity =  networkCamera.selectedEntity;

        edgesShowing = !edgesShowing;

        if (edgesShowing) //show the edges
        {
            showHideEdgesButton.GetComponentInChildren<Text>().text = "Hide Edges";

            try //if the degree is 0 then there will not be a key in the dictionary so it will throw an error, but that's fine, we don't need to do anything in that case
            {
                foreach(NodeEdgePosition nodeEdgePos in entitiesToEdges[selectedEntity])
                {
                    // Charles says not to worry about self connections. However, they still add 2 degrees to nodes so this needs to be fixed

                    GameObject line = new GameObject();
                    activeLines.Add(line);
                    line.transform.position = nodeEdgePos.nodeACoords;
                    line.AddComponent<LineRenderer>();
                    LineRenderer lr = line.GetComponent<LineRenderer>();
                    Color evaluatedColor = edgeValueGradient.Evaluate((float)nodeEdgePos.weight * 10000); // multiply by 100000, bc the edge weight numbers are too small to make a significant difference on the gradient
                    lr.material = new UnityEngine.Material(Shader.Find("HDRP/Unlit")); // add shader that supports transparency
                    lr.GetComponent<Renderer>().material.color = evaluatedColor;
                    //lr.SetWidth(0.25f, 0.25f);
                    lr.startWidth = 0.25f;
                    lr.endWidth = 0.25f;
                    lr.SetPosition(0, nodeEdgePos.nodeACoords);
                    lr.SetPosition(1, nodeEdgePos.nodeBCoords);
                }
            } catch { } 
            
        }
        if (!edgesShowing) 
        {
            foreach(GameObject cur in activeLines) 
            {
                
                //Destroy(cur.GetComponent<Renderer>().material);   //Prevents a memory leak because we manually created the material when spawning the line (do we need this?)
                Destroy(cur);
            }
            showHideEdgesButton.GetComponentInChildren<Text>().text = "Show Edges";
        } 
    }

    public void searchForNode(string query)
    {        
        try 
        {
            networkCamera.selectedEntity = sceneNodeEntitiesMappedToNames[query];
            networkCamera.nodeSelected = true;
            networkCamera.focusOnNode();
        } catch { Debug.Log("Node in query not found"); }
    }

    // Gradient Editing stuff
    public void editNodeGradient() 
    {
        GradientPicker.Create(nodeValueGradient, "Choose Node Baseline Value Gradient...", SetColor, editNodeGradientFinished);
    }

    private void editNodeGradientFinished(Gradient finalGradient)
    {
        nodeValueGradient = finalGradient;
        // add something that respawns the nodes, or figure out a way to use material overrides and have it reference the gradient and send all nodes a message to update their color to the gradient reference
    }

    public void editEdgeGradient()
    {
        GradientPicker.Create(edgeValueGradient, "Choose Edge Weight Gradient...", SetColor, editEdgeGradientFinished);
    }

    public void editEdgeGradientFinished(Gradient finalGradient)
    {
        edgeValueGradient = finalGradient;
    }

    private void SetColor(Gradient currentGradient) { }
    //--------------------------------------------------

}