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
    private float positionMultiplier;
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
    private List<Entity> allNodeEntities = new List<Entity>();
    private Dictionary<int, List<Entity>> clusterNumbersToEntities = new Dictionary<int, List<Entity>>();

    private double maxAbsBlineScore = -1.0;
    private List<float4> blineList = new List<float4>(); // first 3 values are coordinates, last is value, populated when spawning nodes, uses absolute values
    private List<float4> degreeList = new List<float4>();  // first 3 values are coordinates, last is value, populated when spawning nodes
    private List<GameObject> facetCircles = new List<GameObject>();


    private void Start() 
    {
        if (instance != null && instance != this) 
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        inputDataHolder = GameObject.Find("InputDataHolder");

        try { positionMultiplier = inputDataHolder.GetComponent<DataHolder>().positionMultiplier; } catch { positionMultiplier = 100; }

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        blobAssetStore = new BlobAssetStore();
        gameObjectConversionSettings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
        
        ConvertRawInputNodes(); 

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
        try { SpawnFacetCircles(rawLayoutInput); } catch { Debug.Log("Facet Circle Spawn Failed"); }
    
        AutoScaling();
    }

    private void AutoScaling() 
    {
        const float minDesiredDistance = 10;
        float minDistance =  Mathf.Infinity;

        foreach(Entity entity in allNodeEntities)
        {
            float3 entityPosition = new float3(
                entityManager.GetComponentData<Translation>(entity).Value.x, 
                entityManager.GetComponentData<Translation>(entity).Value.y, 
                entityManager.GetComponentData<Translation>(entity).Value.z);

            if (entityManager.GetComponentData<NodeData>(entity).cluster != 0)
            {
                cluster = entityManager.GetComponentData<NodeData>(entity).cluster;

                foreach(KeyValuePair<int,List<Entity>> clustersToEntityLists in clusterNumbersToEntities)
                {
                    if (clustersToEntityLists.Key != cluster) 
                    {
                        foreach(Entity clusterMember in clustersToEntityLists.Value)
                        {
                            float3 clusterMemberPos = new float3(
                                entityManager.GetComponentData<Translation>(clusterMember).Value.x, 
                                entityManager.GetComponentData<Translation>(clusterMember).Value.y, 
                                entityManager.GetComponentData<Translation>(clusterMember).Value.z);
                            
                            float distance = Vector3.Distance(clusterMemberPos, entityPosition);

                            if(distance < minDistance)
                            {
                                minDistance = distance;
                            }
                        }
                    }
                }
            }  
        }

        float scalingValue = minDesiredDistance / minDistance;

        ScaleFacetCircles(scalingValue); // should probably multiply by less than positionMultiplier to ensure nodes are above facet circles
        foreach (int key in clusterNumbersToEntities.Keys)
        {
            if (key != 0) 
            {
                ScaleNodesByCluster(key, scalingValue);
            }
        }

        // write seperate algorithm for misc cluster, maybe find minimum distances in it and use that?
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

                if (clusterNum == 0)
                {
                    coord.x = (float.Parse(line.Split('[')[1].Split(',')[0].Trim()) - 160.0f) ;
                    coord.y = float.Parse(line.Split(',')[1].Split(',')[0].Trim());
                    coord.z = float.Parse(line.Split(',')[2].Split(']')[0].Trim());
                } 
                else 
                {
                    coord.x = float.Parse(line.Split('[')[1].Split(',')[0].Trim());
                    coord.y = float.Parse(line.Split(',')[1].Split(',')[0].Trim());
                    coord.z = float.Parse(line.Split(',')[2].Split(']')[0].Trim());
                }

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
            } catch { Debug.Log(line); } 
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

        foreach(Entity curEntity in allNodeEntities) // this can be any dictionary that has all entities as the keys
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

        blineList.Add( new float4(coord.x, coord.y, coord.z, System.Math.Abs((float)blineScore) ));
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
        allNodeEntities.Add(newNodeEntity);
        return newNodeEntity;
    }

    private void SpawnFacetCircles(string rawInput) 
    {
        //string[] facetCoordLines = rawInput.Split('#')[1].Split('$')[1].Split('\n');
        string[] facetCoordLines = rawInput.Split('$')[1].Split('\n');

        foreach(string line in facetCoordLines) 
        {
            try 
            {
                string[] coordStrings = line.Split('[')[1].Split(']')[0].Split(',');

                float x = float.Parse(coordStrings[0]);
                float y = float.Parse(coordStrings[1]);
                float z = float.Parse(coordStrings[2]);

                Vector3 coords = new Vector3(x, y, z);
                GameObject newFacetCircle = Instantiate(facetCircleObject, coords, Quaternion.identity);
                // if second param set to Vector3.up it looks  cool
                newFacetCircle.transform.LookAt(Vector3.zero);
                facetCircles.Add(newFacetCircle);
            } catch { 
                Debug.Log("%%"); 
            }
        }
    }

    public List<Entity> GetSimpleNodeEntityList() { return allNodeEntities; }

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
                throw new ArgumentException("Node not found");
            }
        }
    }

    //private Dictionary<Entity, List<Entity>> entitiesToConnectedEntities = new Dictionary<Entity, List<Entity>>();
    public List<Entity> GetConnectedEntities(Entity entity)
    {
        return entitiesToConnectedEntities[entity];
    }

    public List<Entity> GetEntitiesInCluster(int clusterNumber)
    {
        return clusterNumbersToEntities[clusterNumber];
    }

    public void ScaleNodesByCluster(int clusterNumber, float scale)
    {
        foreach(Entity entity in clusterNumbersToEntities[clusterNumber])
        {
            float3 newCoordinate = new float3(
                entityManager.GetComponentData<Translation>(entity).Value.x * positionMultiplier, 
                entityManager.GetComponentData<Translation>(entity).Value.y * positionMultiplier, 
                entityManager.GetComponentData<Translation>(entity).Value.z * positionMultiplier);

            Translation translation = new Translation() { Value = newCoordinate };
            entityManager.AddComponentData( entity, translation );
        }
    }

    // maybe seperate into coord scaling and scale scaling?
    public void ScaleFacetCircles(float scale)
    {
        foreach(GameObject circle in facetCircles)
        {
            circle.transform.position = new Vector3(
                circle.transform.position.x * positionMultiplier, 
                circle.transform.position.y * positionMultiplier, 
                circle.transform.position.z * positionMultiplier);

            //circle.transform.localScale = new float3(positionMultiplier, positionMultiplier, positionMultiplier);
        }
    }

    //  U      U    I
    //  U      U    I
    //  U      U    I
    //   UUUUUU     I
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