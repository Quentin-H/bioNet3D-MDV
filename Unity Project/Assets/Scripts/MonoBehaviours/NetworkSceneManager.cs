using System;
using System.Linq;
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

    public GameObject topNetworkRankObject;
    private List<GameObject> topNetworkRankObjects = new List<GameObject>();
    public GameObject topBaselineScoreObject;
    private List<GameObject> topBaselineScoreObjects = new List<GameObject>();
    private float4[] topBaselineScoreLocations = new float4[200]; // first 3 values are coordinates, the last one is the value associated with the node here
    public GameObject topDegreeObject;
    private List<GameObject> topDegreeObjects = new List<GameObject>();
    private IDictionary<FixedString32, Entity> sceneNodeEntities = new Dictionary<FixedString32, Entity>(); // This is for use internally liek creating edges, since internally genes are identified by feature IDs
    private IDictionary<String, Entity> sceneNodeEntitiesMappedToNames = new Dictionary<String, Entity>(); // This is for use with searching for nodes since the user would use names
    //maybe add dictionary with keys as coordinates if we want a feature that finds the connected node
    private List<float4> blineList = new List<float4>(); // first 3 values are coordinates, last is value, populated when spawning nodes
    private List<float4> degreeList = new List<float4>();  // first 3 values are coordinates, last is value, populated when spawning nodes
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

        for(int i = 0; i < 200; i++)
        {
            //Debug.Log(topBaselineScoreLocations[i]);
        }

        // sort this by baseline score List<Order> SortedList = objListOrder.OrderBy(o=>o.OrderDate).ToList();
        blineList = blineList.OrderByDescending(o => o.w).ToList();
        for (int i = 0; i < 200; i++)
        {
            GameObject newObject = Instantiate(topBaselineScoreObject, new float3(blineList[i].x, blineList[i].y, blineList[i].z), Quaternion.identity);
            topBaselineScoreObjects.Add(newObject);
        }
        // get top 200 and spawn objects at locations at each 

        degreeList = degreeList.OrderByDescending(o => o.w).ToList();
        for (int i = 0; i < 200; i++)
        {
            GameObject newObject = Instantiate(topDegreeObject, new float3(degreeList[i].x, degreeList[i].y, degreeList[i].z), Quaternion.identity);
            topDegreeObjects.Add(newObject);
        }

        ConvertRawInputEdges();
        //StartCoroutine(ConvertRawInputEdges());
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

        foreach(string line in rawLayoutInputLines) 
        {
            if (String.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string fID = "";
            float3 coord = new float3(0,0,0);
            string dName = "";
            string desc = "";
            int nRank = 0;
            double blineScore = 0;
            int deg = 0;

            try
            {
                fID = line.Split('|')[0];
                
                coord.x = float.Parse(line.Split('[')[1].Split(',')[0].Trim()) * positionMultiplier;
                coord.y = float.Parse(line.Split(',')[1].Split(',')[0].Trim()) * positionMultiplier;
                coord.z = float.Parse(line.Split(',')[2].Split(']')[0].Trim()) * positionMultiplier;

                dName = line.Split('|')[2];
                desc = line.Split('|')[3];
                nRank = int.Parse(line.Split('|')[4].Trim());
                blineScore = double.Parse(line.Split('|')[5].Trim());
                deg = int.Parse(line.Split('|')[6].Trim());

                SpawnNode(fID, coord, dName, desc, nRank, blineScore, deg);
            } catch (ArgumentException ex) { /*Debug.Log("Node already spawned");*/ } // Sometimes somehow the node has already been spawned and is present in the scene, I do not know how this happens because when I search for the nodes in the file they are only there once.
        }
        Debug.Log("Done Spawning");
    }

    private void SpawnNode(string fID, float3 coord, string dName, string desc, int nRank, double blineScore, int deg)
    {
        if (fID == "" || fID == null)
        {
            return;
        }

        if (nRank <= 200) // adds a billboard if the network rank is less than or equal to 200
        {
            GameObject newObject = Instantiate(topNetworkRankObject, new float3(coord.x, coord.y, coord.z), Quaternion.identity);
            topNetworkRankObjects.Add(newObject);
        }

        blineList.Add(new float4(coord.x, coord.y, coord.z, (float)blineScore));
        degreeList.Add(new float4(coord.x, coord.y, coord.z, (float)deg));

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

    private void ConvertRawInputEdges()
    //IEnumerator ConvertRawInputEdges()
    {
        Debug.Log("Started edge conversion");

        string[] rawEdgeInputLines = new string[0];

        try { rawEdgeInputLines = inputDataHolder.GetComponent<DataHolder>().rawEdgeFile.Split('\n'); } catch { }

        foreach(string line in rawEdgeInputLines)  
        {
            if (String.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string node1Name = "";
            float3 node1Coords = new float3(0,0,0);;
            string node2Name = "";
            float3 node2Coords = new float3(0,0,0);
            double edgeWeight = 0.0;

            try 
            {
                node1Name = line.Split()[0];
                node1Coords = entityManager.GetComponentData<Translation>(sceneNodeEntities[node1Name]).Value; // we should be doing this by getting the LocalToWorld component but for some reason if this isnt an enumerator that returns 0

                node2Name = line.Split()[1];
                node2Coords = entityManager.GetComponentData<Translation>(sceneNodeEntities[node2Name]).Value;

                edgeWeight = double.Parse(line.Split()[2]);
            } catch { Debug.Log("Edge line parsing error"); }

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

            edgeConversionSteps++;
            edgeConversionProgressText.text = "Edge importation in progress. (" + String.Format("{0:0.00}", ((float)edgeConversionSteps / (float)rawEdgeInputLines.Length) * 100.0f) + "%)";
            //yield return null;
        }
        //edgeConversionProgressText.gameObject.SetActive(false);
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

    public void showTopRankedBillboards(bool showOrHide)
    {
        foreach(GameObject cur in topNetworkRankObjects) 
        {
            cur.SetActive(showOrHide);
        }
    }

    public void showTopBaselineBillboards(bool showOrHide)
    {
        foreach(GameObject cur in topBaselineScoreObjects) 
        {
            cur.SetActive(showOrHide);
        }
    }

    public void showTopDegreeBillboards(bool showOrHide)
    {
        foreach(GameObject cur in topDegreeObjects) 
        { 
            cur.SetActive(showOrHide);
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