using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine.UI;
using Unity.Transforms;

public class NetworkSceneManager : MonoBehaviour
{
    public static NetworkSceneManager instance;

    public GameObject nodePrefab;
    private Entity nodeEntityPrefab;
    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;
    private GameObject inputDataHolder;

    //If set to low number ex. 10, creates beautiful patterns
    // Figure out a way to automatically set this based on the layout so points aren't too close together
    public float positionMultiplier;


    private void Awake() 
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
    }

    private void OnDestroy() 
    {
        blobAssetStore.Dispose();
    }

    private void ConvertRawInputNodes()
    {
        string[] rawLayoutInputLines = inputDataHolder.GetComponent<DataHolder>().rawNodeLayoutFile.Split('\n');

        foreach(string line in rawLayoutInputLines)
        {
            string id = line.Split(' ')[0];
            float3 coord;
            float value = float.Parse(line.Split(']')[1].Trim());
            coord.x = float.Parse(line.Split('[')[1].Split(',')[0].Trim()) * positionMultiplier;
            coord.y = float.Parse(line.Split(',')[1].Split(',')[0].Trim()) * positionMultiplier;
            coord.z = float.Parse(line.Split(',')[2].Split(']')[0].Trim()) * positionMultiplier;

            Debug.Log(id);
            Debug.Log(coord);
            Debug.Log(value);
            SpawnNode(id, coord, value);
        }
    }

    private void SpawnNode(string id, float3 coord, float value)
    {
        Entity newNodeEntity = entityManager.Instantiate(nodeEntityPrefab);

        Translation translation = new Translation()
        {
            Value = coord
        };

        entityManager.AddComponentData(newNodeEntity, translation);
        //set data component
        //entityManager.SetComponentData(spawnedEntity, new Translation() { Value = randomPosition });
    }
}