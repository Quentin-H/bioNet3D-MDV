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

public class NetworkSceneManager : MonoBehaviour
{
    public static NetworkSceneManager instance;
    public static NetworkCamera networkCamera;

    public GameObject nodePrefab;
    //If set to low number ex. 10, creates beautiful patterns on layouts besides fr3d
    // Figure out a way to automatically set this based on the layout so points aren't too close together
    public float positionMultiplier;
    private Entity nodeEntityPrefab;

    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;

    private GameObject inputDataHolder;
    private bool edgesShowing = false;
    public Button showHideEdgesButton;
    public Gradient nodeValueGradient;

    private List<Entity> sceneNodeEntities = new List<Entity>();

    private List<NodeEdgePosition> edgeList = new List<NodeEdgePosition>();
    

    private void Start() 
    {
        inputDataHolder = GameObject.Find("InputDataHolder");

        if (instance != null && instance != this) 
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        blobAssetStore = new BlobAssetStore();
        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
        nodeEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(nodePrefab, settings);
        
        ConvertRawInputNodes();
        StartCoroutine("ConvertRawInputEdges");
        Debug.Log("3");
    }

    private void OnDestroy() 
    {
        blobAssetStore.Dispose();
    }

    private void ConvertRawInputNodes()
    {
        Debug.Log("started node conversion");

        string[] rawLayoutInputLines = inputDataHolder.GetComponent<DataHolder>().rawNodeLayoutFile.Split('\n');

        foreach(string line in rawLayoutInputLines)
        {
            //add try catches for index errors
            string id = "";

            float3 coord = new float3(0,0,0);
            double value = 0;

            try
            {
                id = line.Split(' ')[0];
                value = double.Parse(line.Split(']')[1].Trim());
                coord.x = float.Parse(line.Split('[')[1].Split(',')[0].Trim()) * positionMultiplier;
                coord.y = float.Parse(line.Split(',')[1].Split(',')[0].Trim()) * positionMultiplier;
                coord.z = float.Parse(line.Split(',')[2].Split(']')[0].Trim()) * positionMultiplier;
            } catch { }

            SpawnNode(id, coord, value);
        }
        //why isnt this reached
        Debug.Log("Done Spawning");
    }

    IEnumerator ConvertRawInputEdges()
    {
        Debug.Log("Started edge conversion");
        string[] rawEdgeInputLines = inputDataHolder.GetComponent<DataHolder>().rawEdgeFile.Split('\n');
        Debug.Log(inputDataHolder.GetComponent<DataHolder>().rawEdgeFile);

        foreach(string line in rawEdgeInputLines) //add try catches
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
                coord1 =  entityManager.GetComponentData<LocalToWorld>(searchForNode(node1Name)).Value[3];
                node1Coords = new float3(coord1.x,coord1.y, coord1.z);

                node2Name = line.Split()[1];
                coord2 =  entityManager.GetComponentData<LocalToWorld>(searchForNode(node2Name)).Value[3];
                node2Coords =  new float3(coord2.x,coord2.y, coord2.z);

                edgeWeight = double.Parse(line.Split()[2]);
            } catch { }

            NodeEdgePosition newEdge = new NodeEdgePosition(new FixedString32(node1Name), node1Coords, new FixedString32(node2Name), node2Coords, edgeWeight);

            edgeList.Add(newEdge);
            yield return null;
        }
        Debug.Log("Done converting edges");
    }

    private void SpawnNode(string id, float3 coord, double value)
    {
        Entity newNodeEntity = entityManager.Instantiate(nodeEntityPrefab);

        Translation translation = new Translation()
        {
            Value = coord
        };

        entityManager.AddComponentData(newNodeEntity, translation);

        Color evaluatedColor = nodeValueGradient.Evaluate( (float)value);
        float4 colorF = new float4(evaluatedColor.r, evaluatedColor.g, evaluatedColor.b, evaluatedColor.a);
        MaterialColor mcc = new MaterialColor { Value = colorF };
        entityManager.AddComponentData<MaterialColor>(newNodeEntity, mcc);
        
        entityManager.SetComponentData(newNodeEntity, new NodeData { nodeName = id, nodeValue = value });

        sceneNodeEntities.Add(newNodeEntity);
    }

    public void showHideEdges()
    {
        Entity selectedEntity = NetworkCamera.selectedEntity;

        edgesShowing = !edgesShowing;

        if (edgesShowing) 
        {
            showHideEdgesButton.GetComponentInChildren<Text>().text = "Hide Edges";
        }
        if (!edgesShowing)
        {
            showHideEdgesButton.GetComponentInChildren<Text>().text = "Show Edges";
        } 
    }

    private Entity searchForNode(string query)
    {
        foreach(Entity curEntity in sceneNodeEntities)
        {
            if (entityManager.GetComponentData<NodeData>(curEntity).nodeName == query)
            {
                return curEntity;
            }
        }
        Debug.Log("Entity not found!");
        return new Entity();
    }
}

public struct NodeEdgePosition
{
    public FixedString32 nodeAName;
    public float3 nodeACoords;
    
    public FixedString32 nodeBName;
    public float3 nodeBCoords;

    public double weight;

    public NodeEdgePosition(FixedString32 setNodeAName, float3 setNodeACoords, FixedString32 setNodeBName, float3 setNodeBCoords, double setWeight)
    {
        nodeACoords = setNodeACoords;
        nodeAName = setNodeAName;
        nodeBCoords = setNodeBCoords;
        nodeBName = setNodeBName;
        weight = setWeight;
    }
}