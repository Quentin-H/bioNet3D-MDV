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
    public Gradient edgeValueGradient;
    [ColorUsage(true, true)]
    public Color selectedGlowColor; // HDR
    //If set to low number ex. 10, creates beautiful patterns on layouts besides fr3d
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
    public LineRenderer lineRenderer;
    public GameObject top200Object;

    private IDictionary<FixedString32, Entity> sceneNodeEntities = new Dictionary<FixedString32, Entity>();
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

            edgeConversionSteps++;
            edgeConversionProgressText.text = "Edge importation in progress. (" + String.Format("{0:0.00}", ((float)edgeConversionSteps / (float)rawEdgeInputLines.Length) * 100.0f) + "%)";
            yield return null;
        }
        edgeConversionProgressText.gameObject.SetActive(false);
        Debug.Log("Done converting edges");
    }

    public void showHideEdges()
    {
        Entity selectedEntity = NetworkCamera.selectedEntity;

        edgesShowing = !edgesShowing;

        if (edgesShowing) //show the edges
        {
            showHideEdgesButton.GetComponentInChildren<Text>().text = "Hide Edges";

            try //if the degree is 0 then there will not be a key in the dictionary so it will throw an error, but that's fine, we don't need to do anything in that case
            {
                foreach(NodeEdgePosition nodeEdgePos in entitiesToEdges[selectedEntity])
                {
                    //Debug.Log(entityManager.GetComponentData<NodeData>(selectedEntity).displayName);
                    // test if it is a self connection and if it is , continuie and skip steps if it connects to another
                    //if ()

                    GameObject line = new GameObject();
                    activeLines.Add(line);
                    line.transform.position = nodeEdgePos.nodeACoords;
                    line.AddComponent<LineRenderer>();

                    LineRenderer lr = line.GetComponent<LineRenderer>();
                    //lr.material = new UnityEngine.Material(Shader.Find("Particles/Alpha Blended Premultiply"));
                    Color evaluatedColor = edgeValueGradient.Evaluate((float)nodeEdgePos.weight);
                    lr.startColor = evaluatedColor;
                    lr.endColor = evaluatedColor;
                    lr.SetWidth(0.25f, 0.25f);
                    Debug.Log(nodeEdgePos.nodeACoords);
                    lr.SetPosition(0, nodeEdgePos.nodeACoords);
                    Debug.Log(nodeEdgePos.nodeBCoords);
                    lr.SetPosition(1, nodeEdgePos.nodeBCoords);
                }
            } catch { }
            
        }
        if (!edgesShowing)
        {
            foreach(GameObject cur in activeLines)
            {
                Destroy(cur);
            }
            showHideEdgesButton.GetComponentInChildren<Text>().text = "Show Edges";
        } 
    }

    public void setViewAxis(int view)
    {
        if (view == 0) //x (830,290,535)
        {
            Camera.main.transform.position = new Vector3(830, 290, 535);
        }
        if (view == 1) //y
        {
            
        }
        if (view == 2) //z
        {
            
        }
        if (view == 3) //-x (-830,290,535) r = (0,90,0)
        {

        }
        if (view == 4) //-y
        {
            
        }
        if (view == 5) //-z
        {
            
        }
    }
}