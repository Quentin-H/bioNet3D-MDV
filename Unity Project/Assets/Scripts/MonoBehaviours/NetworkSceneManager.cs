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
    [SerializeField] private NetworkCamera networkCamera;

    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private Gradient nodeValueGradient;
    [SerializeField] private Gradient edgeValueGradient;
    // If set to low number ex. 10, creates beautiful patterns on layouts besides fr3d
    // Figure out a way to automatically set this based on the layout so points aren't too close together
    public float positionMultiplier;
    private Entity nodeEntityPrefab;

    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;
    private GameObjectConversionSettings gameObjectConversionSettings;

    private GameObject inputDataHolder;
    private bool edgesShowing = false;
    [SerializeField] private Button showHideNodeEdgesButton;
    [SerializeField] private Button showHideClusterEdgesButton;

    [SerializeField] private GameObject topNetworkRankObject;
    private List<GameObject> topNetworkRankObjects = new List<GameObject>();
    [SerializeField] private GameObject topBaselineScoreObject;
    private List<GameObject> topBaselineScoreObjects = new List<GameObject>();
    private float4[] topBaselineScoreLocations = new float4[200]; // first 3 values are coordinates, the last one is the value associated with the node here
    [SerializeField] private GameObject topDegreeObject;
    private List<GameObject> topDegreeObjects = new List<GameObject>();
    [SerializeField] private GameObject facetCircleObject;

    private IDictionary<FixedString32, Entity> fixedIDsToSceneNodeEntities = new Dictionary<FixedString32, Entity>(); // This is for use internally liek creating edges, since internally genes are identified by feature IDs
    private IDictionary<String, Entity> namesToSceneNodeEntities = new Dictionary<String, Entity>(); // This is for use with searching for nodes since the user would use names
    private Dictionary<Entity, List<Entity>> entitiesToConnectedEntities = new Dictionary<Entity, List<Entity>>();
    private List<Entity> allEntities = new List<Entity>();
    private Dictionary<int, List<Entity>> clusterNumbersToEntities = new Dictionary<int, List<Entity>>();

    private double maxAbsBlineScore = -1.0;
    private List<float4> blineList = new List<float4>(); // first 3 values are coordinates, last is value, populated when spawning nodes
    private List<float4> degreeList = new List<float4>();  // first 3 values are coordinates, last is value, populated when spawning nodes
 
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

        try { positionMultiplier = inputDataHolder.GetComponent<DataHolder>().positionMultiplier; } catch { positionMultiplier = 10; }

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        blobAssetStore = new BlobAssetStore();
        gameObjectConversionSettings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
        //nodeEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(nodePrefab, settings);
        
        ConvertRawInputNodes(); // this has to complete before convert raw input edges

        blineList = blineList.OrderByDescending(o => o.w).ToList();
        for (int i = 0; i < 200; i++)
        {
            GameObject newObject = Instantiate(topBaselineScoreObject, new float3(blineList[i].x, blineList[i].y, blineList[i].z), Quaternion.identity);
            topBaselineScoreObjects.Add(newObject);
        }

        degreeList = degreeList.OrderByDescending(o => o.w).ToList();
        for (int i = 0; i < 200; i++)
        {
            GameObject newObject = Instantiate(topDegreeObject, new float3(degreeList[i].x, degreeList[i].y, degreeList[i].z), Quaternion.identity);
            topDegreeObjects.Add(newObject);
        }

        ChangeNodeColors(nodeValueGradient);

        string rawLayoutInput = "";
        try { rawLayoutInput = inputDataHolder.GetComponent<DataHolder>().rawNodeLayoutFile; } catch { }
        try { SpawnFacetCircles(rawLayoutInput); } catch {}
        Debug.Log(maxAbsBlineScore);
        Debug.Log((-maxAbsBlineScore));
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

        Dictionary<Entity, List<String>> entitiesToConnectedIDs = new Dictionary<Entity, List<String>>();

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
            int clusterNum = -1;
            string[] connectionIDsFromFile = new string[0];

            try
            {
                fID = line.Split('|')[0].Trim();
                
                coord.x = float.Parse(line.Split('[')[1].Split(',')[0].Trim()) * positionMultiplier;
                coord.y = float.Parse(line.Split(',')[1].Split(',')[0].Trim()) * positionMultiplier;
                coord.z = float.Parse(line.Split(',')[2].Split(']')[0].Trim()) * positionMultiplier;

                dName = line.Split('|')[2];
                desc = line.Split('|')[3];
                nRank = int.Parse(line.Split('|')[4].Trim());
                blineScore = double.Parse(line.Split('|')[5].Trim());
                deg = int.Parse(line.Split('|')[6].Trim());
                clusterNum = int.Parse(line.Split('|')[7].Trim());
                connectionIDsFromFile = line.Split('|')[8].Trim().Split(',');

                if (System.Math.Abs(blineScore) > maxAbsBlineScore) { maxAbsBlineScore = blineScore; }

                Entity e = SpawnNode(fID, coord, dName, desc, nRank, blineScore, deg, clusterNum);

                List<string> connectedIDs = new List<string>(); 

                foreach(string connectedNode in connectionIDsFromFile) 
                {
                    connectedIDs.Add(connectedNode.Trim());
                }
                entitiesToConnectedIDs.Add(e, connectedIDs);
                // then in another thing create edge positions, must do this after when all nodes are spawned
            } catch { Debug.Log(line); } // Sometimes somehow the node has already been spawned and is present in the scene
        }

        foreach(KeyValuePair<Entity, List<string>> entry in entitiesToConnectedIDs) 
        {
            List<Entity> connectedEntities = new List<Entity>();
            foreach(string connectedID in entry.Value) 
            {
                try
                {
                    connectedEntities.Add(FindNode(connectedID)); 
                } catch {}
            }
            entitiesToConnectedEntities.Add(entry.Key, connectedEntities);
        }

        foreach(Entity curEntity in allEntities) // this can be any dictionary that has all entities as the keys
        {
            int clusterNumber = entityManager.GetComponentData<NodeData>(curEntity).cluster;
            // if key already exists, add to this entity to the list at this key
            try 
            {
                List<Entity> entitiesInCluster = clusterNumbersToEntities[clusterNumber];
                entitiesInCluster.Add(curEntity);
                clusterNumbersToEntities[clusterNumber] = entitiesInCluster;
            }
            catch   // if key doesn't exist, create new list with only this entity in it and add it at this key
            {
                List<Entity> entitiesInCluster = new List<Entity>();
                entitiesInCluster.Add(curEntity);
                clusterNumbersToEntities.Add(clusterNumber, entitiesInCluster);
            }
        }

        Debug.Log("Done Spawning");
    }

    private Entity SpawnNode(string fID, float3 coord, string dName, string desc, int nRank, double blineScore, int deg, int clusterNum)
    {
        if (nRank <= 200) // adds a billboard if the network rank is less than or equal to 200
        {
            GameObject newObject = Instantiate(topNetworkRankObject, new float3(coord.x, coord.y, coord.z), Quaternion.identity);
            topNetworkRankObjects.Add(newObject);
        }

        blineList.Add( new float4(coord.x, coord.y, coord.z, (float)blineScore ));
        degreeList.Add( new float4(coord.x, coord.y, coord.z, (float)deg ));

        float4 colorF = new float4( 0, 0, 0, 0 );

        nodeEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy( nodePrefab, gameObjectConversionSettings );
        Entity newNodeEntity = entityManager.Instantiate( nodeEntityPrefab );

        ColorOverride colorOverride = new ColorOverride()
        {
            Value = colorF
        };
        entityManager.AddComponentData( newNodeEntity, colorOverride );

        Translation translation = new Translation()
        {
            Value = coord
        };
        entityManager.AddComponentData( newNodeEntity, translation );

        entityManager.SetComponentData(newNodeEntity, new NodeData { 
            featureID = fID, 
            displayName = dName, 
            description = desc, 
            networkRank = nRank,
            baselineScore = blineScore,
            degree = deg,
            cluster = clusterNum
        });

        FixedString32 idAsFixed = fID;
        fixedIDsToSceneNodeEntities.Add(idAsFixed, newNodeEntity);
        namesToSceneNodeEntities.Add(dName, newNodeEntity);
        allEntities.Add(newNodeEntity);
        return newNodeEntity;
    }

    private void SpawnFacetCircles(string rawInput) 
    {
        //string[] facetCoordLines = rawInput.Split('#')[1].Split('$')[1].Split('\n');
        string[] facetCoordLines = rawInput.Split('$')[1].Split('\n');
        Debug.Log(facetCoordLines.Length);
        foreach(string line in facetCoordLines) 
        {
            try 
            {
                string[] coordStrings = line.Split('[')[1].Split(']')[0].Split(',');
                float x = float.Parse(coordStrings[0]) * 5f;
                float y = float.Parse(coordStrings[1]) * 5f;
                float z = float.Parse(coordStrings[2]) * 5f;
                Vector3 coords = new Vector3(x, y, z);
                GameObject newFacetCircle = Instantiate(facetCircleObject, coords, Quaternion.identity);
                // if second param set to Vector3.up it looks  cool
                newFacetCircle.transform.LookAt(Vector3.zero);
                Debug.Log(coords);
            } catch { 
                Debug.Log(line); 
            }
        }
    }

    public void ChangeNodeColors(Gradient gradient) 
    {
        foreach (KeyValuePair<FixedString32, Entity> entry in fixedIDsToSceneNodeEntities) 
        {            
            double blineScore = entityManager.GetComponentData<NodeData>(entry.Value).baselineScore;
            double normalizedblineScore = (blineScore - (-maxAbsBlineScore)) / (maxAbsBlineScore - (-maxAbsBlineScore));
            Color evaluatedColor = gradient.Evaluate((float)normalizedblineScore);
            float4 colorF = new float4( evaluatedColor.r, evaluatedColor.g, evaluatedColor.b, evaluatedColor.a );
            
            ColorOverride colorOverride = new ColorOverride()
            {
                Value = colorF
            };
            entityManager.AddComponentData(entry.Value, colorOverride);
        }
    }

    public Entity FindNode(string query)
    {
        try
        {
            return fixedIDsToSceneNodeEntities[query.Trim()];
        } 
        catch 
        {
            try 
            {
                return namesToSceneNodeEntities[query.Trim()];
            }
            catch
            {
                throw new ArgumentException("Invalid Node Query");
            }
        }
    }

    //  U      U    I
    //  U      U    I
    //  U      U    I
    //   UUUUUU     I
    public void showHideNodeEdges() // THIS SHOULD BE MOVED TO CAMERA SCRIPT PRIOR TO INTEGRATING WITH YANKUN/KE
    {
        Entity selectedEntity =  networkCamera.getSelectedEntity();

        float4 selectedEntityPosAs4 = entityManager.GetComponentData<LocalToWorld>(selectedEntity).Value[3];
        float3 selectedEntityPos = new float3(selectedEntityPosAs4.x, selectedEntityPosAs4.y, selectedEntityPosAs4.z);
        
        edgesShowing = !edgesShowing;

        if (edgesShowing) //show the edges
        {
            showHideNodeEdgesButton.GetComponentInChildren<Text>().text = "Hide Node Edges";

            try 
            {
                foreach(Entity connectedEntity in entitiesToConnectedEntities[selectedEntity])
                {
                    float4 connectedEntityPosAs4 = entityManager.GetComponentData<LocalToWorld>(connectedEntity).Value[3];
                    float3 connectedEntityPos = new float3(connectedEntityPosAs4.x, connectedEntityPosAs4.y, connectedEntityPosAs4.z);

                    GameObject line = new GameObject();
                    activeLines.Add(line);
                    line.transform.position = connectedEntityPos;
                    line.AddComponent<LineRenderer>();
                    LineRenderer lr = line.GetComponent<LineRenderer>();
                    //Color evaluatedColor = edgeValueGradient.Evaluate((float)nodeEdgePos.weight * 10000); // multiply by 100000, bc the edge weight numbers are too small to make a significant difference on the gradient
                    lr.material = new UnityEngine.Material(Shader.Find("HDRP/Unlit")); // add shader that supports transparency
                    //lr.GetComponent<Renderer>().material.color = evaluatedColor;
                    lr.GetComponent<Renderer>().material.color = Color.yellow;
                    lr.startWidth = 0.1f;
                    lr.endWidth = 0.1f;
                    lr.SetPosition(0, selectedEntityPos);
                    lr.SetPosition(1, connectedEntityPos);
                }
            } catch { } 
        }

        if (!edgesShowing) 
        {
            foreach(GameObject cur in activeLines) 
            {
                Destroy(cur);
                //Destroy(cur.GetComponent<Renderer>().material);
            }
            Resources.UnloadUnusedAssets(); // i think this gets rid of materials, prevents memory leak
            showHideNodeEdgesButton.GetComponentInChildren<Text>().text = "Show Node Edges";
        } 
    }

    public void showHideClusterEdges() // THIS SHOULD BE MOVED TO CAMERA SCRIPT PRIOR TO INTEGRATING WITH YANKUN/KE
    {
        Entity selectedEntity =  networkCamera.getSelectedEntity();
        int clusterNumber = entityManager.GetComponentData<NodeData>(selectedEntity).cluster;
        
        edgesShowing = !edgesShowing;

        if (edgesShowing) //show the edges
        {
            showHideClusterEdgesButton.GetComponentInChildren<Text>().text = "Hide Cluster Edges";

            foreach(Entity curEntity in clusterNumbersToEntities[clusterNumber])
            {
                try 
                {
                    float4 curEntityEntityPosAs4 = entityManager.GetComponentData<LocalToWorld>(curEntity).Value[3];
                    float3 curEntityEntityPos = new float3(curEntityEntityPosAs4.x, curEntityEntityPosAs4.y, curEntityEntityPosAs4.z);

                    foreach(Entity connectedEntity in entitiesToConnectedEntities[curEntity])
                    {
                        float4 connectedEntityPosAs4 = entityManager.GetComponentData<LocalToWorld>(connectedEntity).Value[3];
                        float3 connectedEntityPos = new float3(connectedEntityPosAs4.x, connectedEntityPosAs4.y, connectedEntityPosAs4.z);

                        GameObject line = new GameObject();
                        activeLines.Add(line);
                        line.transform.position = connectedEntityPos;
                        line.AddComponent<LineRenderer>();
                        LineRenderer lr = line.GetComponent<LineRenderer>();
                        //Color evaluatedColor = edgeValueGradient.Evaluate((float)nodeEdgePos.weight * 10000); // multiply by 100000, bc the edge weight numbers are too small to make a significant difference on the gradient
                        lr.material = new UnityEngine.Material(Shader.Find("HDRP/Unlit")); // add shader that supports transparency
                        //lr.GetComponent<Renderer>().material.color = evaluatedColor;
                        lr.GetComponent<Renderer>().material.color = Color.yellow;
                        lr.startWidth = 0.1f;
                        lr.endWidth = 0.1f;
                        lr.SetPosition(0, curEntityEntityPos);
                        lr.SetPosition(1, connectedEntityPos);
                    }
                } catch { } 
            }
        }

        if (!edgesShowing) 
        {
            foreach(GameObject cur in activeLines) 
            {
                Destroy(cur);
                //Destroy(cur.GetComponent<Renderer>().material);  
            }
            Resources.UnloadUnusedAssets(); // i think this gets rid of materials, prevents memory leak
            showHideClusterEdgesButton.GetComponentInChildren<Text>().text = "Show Cluster Edges";
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

    // Gradient Editing stuff
    public void editNodeGradient() 
    {
        GradientPicker.Create(nodeValueGradient, "Choose Node Baseline Value Gradient...", SetColor, editNodeGradientFinished);
    }

    private void editNodeGradientFinished(Gradient finalGradient)
    {
        nodeValueGradient = finalGradient;
        ChangeNodeColors(nodeValueGradient);
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