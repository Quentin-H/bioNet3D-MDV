using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Collections;

using NodeViz;


public class NetworkSceneManager : MonoBehaviour
{
    // Scene stuff
    [HideInInspector] public static NetworkSceneManager instance;
    [SerializeField] private NetworkCamera networkCamera;
    [SerializeField] private ECSBillboardManager ecsBillboardManager;
    [SerializeField] private ErrorMessenger errorMessenger;
    [SerializeField] private GameObject innerSphere;
    [SerializeField] private GameObject nodePrefab;
    private GameObject inputDataHolder;
    [SerializeField] private GameObject topNetworkRankObject;
    [HideInInspector] public List<GameObject> topNetworkRankObjects = new List<GameObject>();
    [SerializeField] private GameObject topBaselineScoreObject;
    [HideInInspector] public List<GameObject> topBaselineScoreObjects = new List<GameObject>();
    [SerializeField] private GameObject topDegreeObject;
    [HideInInspector] public List<GameObject> topDegreeObjects = new List<GameObject>();
    [SerializeField] private GameObject facetCircleObject;
    private List<GameObject> facetCircles = new List<GameObject>();

    // ECS stuff
    private Entity nodeEntityPrefab;
    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;
    private GameObjectConversionSettings gameObjectConversionSettings;

    //UI stuff
    [SerializeField] private Text gradientMinBaselineText;
    [SerializeField] private Text gradientMaxBaselineText;
    [SerializeField] private Gradient nodeValueGradient;
    [SerializeField] private Gradient edgeValueGradient;
    
    // dictionaries
    private IDictionary<FixedString32, Entity> fixedIDsToSceneNodeEntities = new Dictionary<FixedString32, Entity>(); // This is for use internally liek creating edges, since internally genes are identified by feature IDs
    private IDictionary<String, Entity> namesToSceneNodeEntities = new Dictionary<String, Entity>(); // This is for use with searching for nodes since the user would use names
    private Dictionary<Entity, List<Entity>> entitiesToConnectedEntities = new Dictionary<Entity, List<Entity>>();
    private Dictionary<int, List<Entity>> clusterNumbersToEntities = new Dictionary<int, List<Entity>>();

    // misc
    private List<Entity> allNodeEntities = new List<Entity>();
    private double maxAbsBlineScore = -1.0;
    private double maxBlineScore = Double.NegativeInfinity;
    private double minBlineScore = Double.PositiveInfinity;

    

    private void Awake()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        blobAssetStore = new BlobAssetStore();
        gameObjectConversionSettings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
    }

    private void Start() 
    {
        inputDataHolder = GameObject.Find("InputDataHolder");
        string rawLayoutInput = "";
        try { rawLayoutInput = inputDataHolder.GetComponent<DataHolder>().nodeLayoutFile; } catch { Debug.Log("Failed to import node layout file"); }
        try { SpawnFacetCircles(rawLayoutInput); } catch { Debug.Log("Facet Circle Spawn Failed"); }

        if (instance != null && instance != this) 
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        ConvertRawInputNodes(); 
        ChangeNodeColors(nodeValueGradient);
        AutoScaleNetwork();
        GenerateTopLists();
        PlaceCamera(Camera.main);

        gradientMinBaselineText.text = (-maxAbsBlineScore).ToString("0.00");
        gradientMaxBaselineText.text = maxAbsBlineScore.ToString("0.00");
    }

    private void OnDestroy()
    {
        Destroy(inputDataHolder);
        entityManager.DestroyEntity(entityManager.UniversalQuery);
        blobAssetStore.Dispose();
    }

    private void PlaceCamera(Camera camera)
    {
        float maxDist = 0;

        foreach(Entity entity in allNodeEntities)
        {
            float3 entityPosition = new float3(
                entityManager.GetComponentData<Translation>(entity).Value.x, 
                entityManager.GetComponentData<Translation>(entity).Value.y, 
                entityManager.GetComponentData<Translation>(entity).Value.z);

            if (Vector3.Distance(entityPosition, Vector3.zero) > maxDist)
            {
                maxDist = Vector3.Distance(entityPosition, Vector3.zero);
            }
        }
        camera.transform.position = new float3(0, 0, 1.25f * maxDist);
    }

    private void AutoScaleNetwork() 
    {
        const float minIntraClusterDistance = 5;
        const float minInterClusterDistance = 20;
        
        /*float minIntraDistance =  Mathf.Infinity;
        foreach(KeyValuePair<int,List<Entity>> clustersToEntityLists in clusterNumbersToEntities)
        {
            foreach(Entity clusterMember1 in clustersToEntityLists.Value)
            {
                float3 entity1Position = new float3(
                    entityManager.GetComponentData<Translation>(clusterMember1).Value.x, 
                    entityManager.GetComponentData<Translation>(clusterMember1).Value.y, 
                    entityManager.GetComponentData<Translation>(clusterMember1).Value.z);
                
                foreach(Entity clusterMember2 in clustersToEntityLists.Value)
                {
                    float3 entity2Position = new float3(
                        entityManager.GetComponentData<Translation>(clusterMember2).Value.x, 
                        entityManager.GetComponentData<Translation>(clusterMember2).Value.y, 
                        entityManager.GetComponentData<Translation>(clusterMember2).Value.z);

                    float distance = Vector3.Distance(entity1Position, entity2Position);
                    if(distance < minIntraDistance)
                    {
                        minIntraDistance = distance;
                    }
                }
            }
        }*/


        float minInterDistance =  Mathf.Infinity;
        foreach(Entity entity in allNodeEntities)
        {
            float3 entityPosition = new float3(
                entityManager.GetComponentData<Translation>(entity).Value.x, 
                entityManager.GetComponentData<Translation>(entity).Value.y, 
                entityManager.GetComponentData<Translation>(entity).Value.z);

            if (entityManager.GetComponentData<NodeData>(entity).cluster != 0)
            {
                int cluster = entityManager.GetComponentData<NodeData>(entity).cluster;

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

                            if(distance < minInterDistance)
                            {
                                minInterDistance = distance;
                            }
                        }
                    }
                }
            }  
        }

        //float scalingValue = (minInterClusterDistance / minInterDistance) + (minIntraClusterDistance / minIntraDistance);
        float scalingValue = minInterClusterDistance / minInterDistance;

        ScaleFacetCircles(scalingValue); 
        foreach (int key in clusterNumbersToEntities.Keys)
        {
            if (key != 0) 
            {
                ScaleNodeDistanceByCluster(key, scalingValue);
            }
        }

        // write seperate algorithm for misc cluster, maybe find minimum distances in it and use that?
    }

    private void GenerateTopLists()
    {
        // first 3 values are coordinates, last is value, populated when spawning nodes
        List<float4> blineList = new List<float4>(); // uses absolute values
        List<float4> rankList = new List<float4>();
        List<float4> degreeList = new List<float4>();  

        foreach(Entity entity in allNodeEntities)
        {
            float3 entityPos = new float3(
                entityManager.GetComponentData<Translation>(entity).Value.x, 
                entityManager.GetComponentData<Translation>(entity).Value.y, 
                entityManager.GetComponentData<Translation>(entity).Value.z);

            float rank = (float)entityManager.GetComponentData<NodeData>(entity).networkRank;
            float blineScore = (float)entityManager.GetComponentData<NodeData>(entity).baselineScore;
            float degree = (float)entityManager.GetComponentData<NodeData>(entity).degree;

            if (rank != -1)
                rankList.Add(new float4(entityPos.x, entityPos.y, entityPos.z, rank));
            blineList.Add(new float4(entityPos.x, entityPos.y, entityPos.z, blineScore));
            degreeList.Add(new float4(entityPos.x, entityPos.y, entityPos.z, degree));
        }

        rankList = rankList.OrderBy(o => o.w).ToList();
        blineList = blineList.OrderByDescending(o => o.w).ToList();
        degreeList = degreeList.OrderByDescending(o => o.w).ToList();

        List<float3> rankPos = new List<float3>();
        List<float3> blinePos = new List<float3>();
        List<float3> degreePos = new List<float3>();
        for(int i = 1; i <= 200; i++)
        {
            rankPos.Add(new float3(rankList[i].x, rankList[i].y, rankList[i].z));
            blinePos.Add(new float3(blineList[i].x, blineList[i].y, blineList[i].z));
            degreePos.Add(new float3(degreeList[i].x, degreeList[i].y, degreeList[i].z));
        }
        ecsBillboardManager.SetTopRankHighlights(rankPos);
        ecsBillboardManager.SetTopBaselineHighlights(blinePos);
        ecsBillboardManager.SetTopDegreeHighlights(degreePos);
    }

    private async void ConvertRawInputNodes() 
    {
        Debug.Log("started node conversion");

        string[] rawLayoutInputLines = new string[0];
        try { rawLayoutInputLines = inputDataHolder.GetComponent<DataHolder>().nodeLayoutFile.Split('$')[0].Split('#')[0].Split('\n'); } catch { }

        string[] rawNodeDescriptiveInfoInputLines = new string[0];
        try { rawNodeDescriptiveInfoInputLines = inputDataHolder.GetComponent<DataHolder>().nodeInfoFile.Split('\n'); } catch { }
        Dictionary<string, string> fIDsToNodeDescriptiveInfoLines = new Dictionary<string, string>();
        foreach (string curLine in rawNodeDescriptiveInfoInputLines.Skip(1))
        {
            try { fIDsToNodeDescriptiveInfoLines.Add(curLine.Split()[0].Trim(), curLine); } 
            catch { Debug.Log("Error parsing node info line: " + curLine); }
        }

        string[] rawNumericInfoInputLines = new string[0];
        try { rawNumericInfoInputLines = inputDataHolder.GetComponent<DataHolder>().nodeRankingFile.Split('\n'); } catch { }
        Dictionary<string, double> fIDsToBlineScoresLines = new Dictionary<string, double>();
        Dictionary<string, int> fIDsToRanks = new Dictionary<string, int>();
        try 
        {
            int j = 1;
            foreach (string curLine in rawNumericInfoInputLines.Skip(1))
            {
                try 
                {
                    fIDsToBlineScoresLines.Add(curLine.Split()[1].Trim(), double.Parse(curLine.Split()[4].Trim()));
                    fIDsToRanks.Add(curLine.Split()[1].Trim(), j);
                } catch (Exception e) { Debug.Log(e + " | Error parsing node ranking line: " + curLine); }
                j++;
            }
        } catch { Debug.Log("No ranking file"); }

        Dictionary<Entity, List<String>> entitiesToConnectedIDs = new Dictionary<Entity, List<String>>();

        int i = 0;
        foreach (string nodeLayoutFileLine in rawLayoutInputLines) 
        {
            if (String.IsNullOrWhiteSpace(nodeLayoutFileLine))
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
                // from layout file
                fID = nodeLayoutFileLine.Split('|')[0].Trim();
                try { clusterNum = Int32.Parse(nodeLayoutFileLine.Split('|')[2].Trim()); } catch { }
                connectionIDsFromFile = nodeLayoutFileLine.Split('|')[3].Trim().Split(',');
                coord.x = float.Parse(nodeLayoutFileLine.Split('[')[1].Split(',')[0].Trim());
                coord.y = float.Parse(nodeLayoutFileLine.Split(',')[1].Split(',')[0].Trim());
                coord.z = float.Parse(nodeLayoutFileLine.Split(',')[2].Split(']')[0].Trim());
                deg = connectionIDsFromFile.Length;
                //From descriptive file
                dName = fIDsToNodeDescriptiveInfoLines[fID].Split('\t')[3].Trim(); 
                desc = fIDsToNodeDescriptiveInfoLines[fID].Split('\t')[4].Trim(); 
                //From rank file
                try 
                {
                    nRank = fIDsToRanks[fID];
                    blineScore = fIDsToBlineScoresLines[fID];
                } 
                catch 
                { 
                    nRank = -1;
                    // set to some hyperspecific unique value if wanting to 
                    // add functionality to mark "unscored" nodes as such in user facing UI
                    blineScore = 0; 
                }

                Entity e = SpawnNode(fID, coord, dName, desc, nRank, blineScore, deg, clusterNum);

                List<string> connectedIDs = new List<string>(); 
                foreach(string connectedNode in connectionIDsFromFile) 
                {
                    connectedIDs.Add(connectedNode.Trim().Trim());
                }
                entitiesToConnectedIDs.Add(e, connectedIDs);

                if (blineScore < minBlineScore) minBlineScore = blineScore;
                if (blineScore > maxBlineScore) maxBlineScore = blineScore;
                if (System.Math.Abs(blineScore) > maxAbsBlineScore) { maxAbsBlineScore = System.Math.Abs(blineScore); }
            } catch { } 
            i++;
        }

        if (System.Math.Abs(maxBlineScore) > System.Math.Abs(minBlineScore))
        {
            maxAbsBlineScore = System.Math.Abs(maxBlineScore);
        }
        else
        {
            maxAbsBlineScore = System.Math.Abs(minBlineScore);
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

        foreach(Entity curEntity in allNodeEntities)
        {
            int clusterNumber = entityManager.GetComponentData<NodeData>(curEntity).cluster;
            
            try // if key already exists, add to this entity to the list at this key
            {
                List<Entity> entitiesInCluster = clusterNumbersToEntities[clusterNumber];
                entitiesInCluster.Add(curEntity);
                clusterNumbersToEntities[clusterNumber] = entitiesInCluster;
            }
            catch // if key doesn't exist, create new list with only this entity in it and add it at this key
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
        float4 colorF = new float4( 0, 0, 0, 0 );

        nodeEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy( nodePrefab, gameObjectConversionSettings );
        Entity newNodeEntity = entityManager.Instantiate( nodeEntityPrefab );

        ColorOverride colorOverride = new ColorOverride() { Value = colorF };
        entityManager.AddComponentData( newNodeEntity, colorOverride );

        Translation translation = new Translation() { Value = coord };
        entityManager.AddComponentData( newNodeEntity, translation );

        entityManager.SetComponentData(newNodeEntity, new NodeData 
        { 
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
        string[] facetCoordLines = rawInput.Split('$')[1].Split('\n');

        foreach(string line in facetCoordLines) 
        {
            try 
            {
                //get facet circle position
                string[] coordStrings = line.Split('[')[1].Split(']')[0].Split(',');
                float x = float.Parse(coordStrings[0]);
                float y = float.Parse(coordStrings[1]);
                float z = float.Parse(coordStrings[2]);
                Vector3 coords = new Vector3(x, y, z);

                //scale facet circle
                GameObject newFacetCircle = Instantiate(facetCircleObject, coords, Quaternion.identity);
                float scale = float.Parse(line.Split('|')[2].Trim());
                newFacetCircle.transform.localScale = new float3(scale, scale, scale);

                //spawn cluster letter
                int cluster = Int32.Parse(line.Split('|')[1].Trim());
                
                //rotate 
                newFacetCircle.transform.LookAt(Vector3.zero);
                facetCircles.Add(newFacetCircle);
            } catch { 
                Debug.Log("failed to import coordinate"); 
            }
        }
    }

    public List<Entity> GetSimpleNodeEntityList() { return allNodeEntities; }

    public void ChangeNodeColors(Gradient gradient) 
    {
        foreach (KeyValuePair<FixedString32, Entity> entry in fixedIDsToSceneNodeEntities) 
        {   
            Color evaluatedColor = Color.white;
  
            // the if statement makes it so unscored/unranked nodes are coloured white.
            // if this is not desired just remove the if condition.
            if (entityManager.GetComponentData<NodeData>(entry.Value).networkRank != -1)
            {
                double blineScore = entityManager.GetComponentData<NodeData>(entry.Value).baselineScore;
                double normalizedblineScore = (blineScore - (-maxAbsBlineScore)) / (maxAbsBlineScore - (-maxAbsBlineScore));
                
                if (inputDataHolder.GetComponent<DataHolder>().nodeRankingFile.Trim() != "")
                {
                    evaluatedColor = gradient.Evaluate((float)normalizedblineScore);
                } 
            }
            
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
                throw new ArgumentException("Node not found in ID or name dictionaries!");
            }
        }
    }

    public List<Entity> GetConnectedEntities(Entity entity)
    {
        return entitiesToConnectedEntities[entity];
    }

    public List<Entity> GetEntitiesInCluster(int clusterNumber)
    {
        return clusterNumbersToEntities[clusterNumber];
    }

    public void ScaleNodeDistanceByCluster(int clusterNumber, float scale)
    {
        foreach(Entity entity in clusterNumbersToEntities[clusterNumber])
        {
            float3 newCoordinate = new float3(
                entityManager.GetComponentData<Translation>(entity).Value.x * scale, 
                entityManager.GetComponentData<Translation>(entity).Value.y * scale, 
                entityManager.GetComponentData<Translation>(entity).Value.z * scale);

            Translation translation = new Translation() { Value = newCoordinate };
            entityManager.AddComponentData( entity, translation );
        }
    }

    public void ScaleFacetCircles(float scale)
    {
        foreach(GameObject circle in facetCircles)
        {
            circle.transform.position = new Vector3(
                circle.transform.position.x * scale, 
                circle.transform.position.y * scale, 
                circle.transform.position.z * scale);
            circle.transform.localScale *= scale;
        }
        innerSphere.transform.localScale *= scale * 2;
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
}