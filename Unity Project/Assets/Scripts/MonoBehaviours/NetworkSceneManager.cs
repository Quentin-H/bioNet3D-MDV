using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine.UI;
using Unity.Transforms;
using UnityEngine.UI;
using Unity.Rendering;
using Unity.Collections;
using System;
using NodeViz;

public class NetworkSceneManager : MonoBehaviour
{
    public static NetworkSceneManager instance;
    public static NetworkCamera networkCamera;

    public GameObject nodePrefab;
    public Gradient nodeValueGradient;
    [ColorUsage(true, true)]
    public Color selectedGlowColor; // HDR
    //If set to low number ex. 10, creates beautiful patterns on layouts besides fr3d
    // Figure out a way to automatically set this based on the layout so points aren't too close together
    public float positionMultiplier;
    private Entity nodeEntityPrefab;

    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;

    private GameObject inputDataHolder;
    //private bool edgesShowing = false;
    //public Button showHideEdgesButton;
    public Text edgeConversionProgressText;
    private int edgeConversionPercent;
    private int edgeConversionSteps;
    //public LineRenderer lineRenderer;

    private IDictionary<FixedString32, Entity> sceneNodeEntities = new Dictionary<FixedString32, Entity>();
    //maybe add dictionary with keys as coordinates if we want a feature that finds the connected node
    private List<NodeEdgePosition> edgeList = new List<NodeEdgePosition>();
    private Dictionary<Entity, List<NodeEdgePosition>> entitiesToEdges = new Dictionary<Entity, List<NodeEdgePosition>>();


    private void Start() 
    {
        if (instance != null && instance != this) 
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        inputDataHolder = GameObject.Find("InputDataHolder");

        positionMultiplier = inputDataHolder.GetComponent<DataHolder>().positionMultiplier;

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        blobAssetStore = new BlobAssetStore();
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
        nodeEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(nodePrefab, settings);
        
        ConvertRawInputNodes(); // this has to complete before convert raw input edges
        StartCoroutine(ConvertRawInputEdges());
    }

    private void OnDestroy() 
    {
        entityManager.DestroyEntity(entityManager.UniversalQuery);
        blobAssetStore.Dispose();
    }

    private void ConvertRawInputNodes() 
    {
        Debug.Log("started node conversion");

        string[] rawLayoutInputLines = inputDataHolder.GetComponent<DataHolder>().rawNodeLayoutFile.Split('\n');

        foreach(string line in rawLayoutInputLines) 
        {
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
            } catch { Debug.Log("Error parsing a node from file."); }
        }
        Debug.Log("Done Spawning");
    }

    private void SpawnNode(string fID, float3 coord, string dName, string desc, int nRank, double blineScore, int deg)
    {
        if (fID == "" || fID == null)
        {
            return;
        }

        Entity newNodeEntity = entityManager.Instantiate(nodeEntityPrefab);

        Translation translation = new Translation()
        {
            Value = coord
        };

        entityManager.AddComponentData(newNodeEntity, translation);

        Color evaluatedColor = nodeValueGradient.Evaluate( (float) blineScore);
        float4 colorF = new float4(evaluatedColor.r, evaluatedColor.g, evaluatedColor.b, evaluatedColor.a);
        //MaterialColor mcc = new MaterialColor { Value = colorF };
        //entityManager.AddComponentData<MaterialColor>(newNodeEntity, mcc);
        var renderMesh = entityManager.GetSharedComponentData<RenderMesh>(newNodeEntity);
        var mat = new UnityEngine.Material(renderMesh.material);
        mat.SetColor("_Color", evaluatedColor);
        //mat.SetColor("_GlowColor", selectedGlowColor);
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
    }

    //edge stuff
    IEnumerator ConvertRawInputEdges()
    {
        Debug.Log("Started edge conversion");

        string[] rawEdgeInputLines = inputDataHolder.GetComponent<DataHolder>().rawEdgeFile.Split('\n');

        foreach(string line in rawEdgeInputLines)  // convert this to parralel for job
        {
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
                coord2 =  entityManager.GetComponentData<LocalToWorld>(sceneNodeEntities[node1Name]).Value[3];
                node2Coords =  new float3(coord2.x,coord2.y, coord2.z);

                edgeWeight = double.Parse(line.Split()[2]);
            } catch { }

            //Debug.Log(node1Coords + "|" + node2Coords);
            //edgeList.Add(newEdge);

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
            yield return null;
        }
        edgeConversionProgressText.gameObject.SetActive(false);
        Debug.Log("Done converting edges");
    }

    /*public void showHideEdges()
    {
        Entity selectedEntity = NetworkCamera.selectedEntity;

        edgesShowing = !edgesShowing;

        if (edgesShowing) //show the edges
        {
            showHideEdgesButton.GetComponentInChildren<Text>().text = "Hide Edges";

            foreach(NodeEdgePosition nodeEdgePos in edgeList)
            {
                //add coloring based on user provided color gradient of lines
                if (nodeEdgePos.nodeAName == entityManager.GetComponentData<NodeData>(selectedEntity).nodeName ^ nodeEdgePos.nodeBName == entityManager.GetComponentData<NodeData>(selectedEntity).nodeName)
                {
                    Vector3[] points = new Vector3[2];
                    points[0] = new Vector3(nodeEdgePos.nodeACoords.x, nodeEdgePos.nodeACoords.y, nodeEdgePos.nodeACoords.z);
                    points[1] = new Vector3(nodeEdgePos.nodeBCoords.x, nodeEdgePos.nodeBCoords.y, nodeEdgePos.nodeBCoords.z);
                    Instantiate(lineRenderer, new Vector3(0, 0, 0), Quaternion.identity);
                    lineRenderer.SetPositions(points);
                    continue; // if this is true it cant be self connecting so we skip
                }
                //if the node connects to itself
                if (nodeEdgePos.nodeAName == entityManager.GetComponentData<NodeData>(selectedEntity).nodeName && nodeEdgePos.nodeBName == entityManager.GetComponentData<NodeData>(selectedEntity).nodeName)
                {
                    
                }
            }
        }
        if (!edgesShowing) // hide em
        {
            showHideEdgesButton.GetComponentInChildren<Text>().text = "Show Edges";
        } 
    }*/
}