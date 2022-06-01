using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class ECSBillboardManager : MonoBehaviour
{
    private EntityManager entityManager;
    private BlobAssetStore blobAssetStore;
    private GameObjectConversionSettings gameObjectConversionSettings;

    [SerializeField] private GameObject topRankPrefab;
    [SerializeField] private GameObject topBaselinePrefab;
    [SerializeField] private GameObject topDegreePrefab;
    [SerializeField] private GameObject filteredHighlightPrefab;
    [SerializeField] private GameObject selectionPrefab;

    private List<Entity> topRankBillboards = new List<Entity>();
    private List<Entity> topBaselineBillboards = new List<Entity>();
    private List<Entity> topDegreeBillboards = new List<Entity>();
    private List<Entity> filteredHighlightBillboards = new List<Entity>();

    private Entity selectionHighlight;

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        blobAssetStore = new BlobAssetStore();
        gameObjectConversionSettings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
    }

    public void HighlightSelectedNode(Entity selectedEntity)
    {
        try { entityManager.DestroyEntity(selectionHighlight); } catch { }

        float3 entityPos = new float3
        (
            entityManager.GetComponentData<Translation>(selectedEntity).Value.x, 
            entityManager.GetComponentData<Translation>(selectedEntity).Value.y, 
            entityManager.GetComponentData<Translation>(selectedEntity).Value.z
        );
        
        selectionHighlight = InstantiateHelper(selectionPrefab, entityPos);
        entityManager.SetComponentData<BillboardData>(selectionHighlight, new BillboardData 
        {
            initialScale = 4f,
            yawAngleOffset = 0.0f
        });
    }

    public void SetTopRankHighlights(List<float3> positions)
    {
        topRankBillboards = BatchInstantiateHelper(topRankPrefab, positions);
    }

    public void SetTopBaselineHighlights(List<float3> positions)
    {
        topBaselineBillboards = BatchInstantiateHelper(topBaselinePrefab, positions);
    }

    public void SetTopDegreeHighlights(List<float3> positions)
    {
        topDegreeBillboards = BatchInstantiateHelper(topDegreePrefab, positions);
    }

    public void SetFilteredHighlights(List<float3> positions)
    {
        filteredHighlightBillboards = BatchInstantiateHelper(filteredHighlightPrefab, positions);
    }

    public void ClearFilteredHighlights()
    {
        try 
        {
            foreach (Entity entity in filteredHighlightBillboards)
            {
                entityManager.DestroyEntity(entity);
            }
        } catch { }
        filteredHighlightBillboards.Clear();
    }

    private bool showingTopRankHighlights = true;
    public void ShowHideTopRankHighlights()
    {
        ShowHideHelper(topRankBillboards, !showingTopRankHighlights);
    }

    private bool showingTopBaselineHighlights = true;
    public void ShowHideTopBaselineHighlights()
    {
        ShowHideHelper(topBaselineBillboards, !showingTopBaselineHighlights);
    }

    private bool showingTopDegreeHighlights = true;
    public void ShowHideTopDegreeHighlights()
    {
        ShowHideHelper(topDegreeBillboards, !showingTopDegreeHighlights);
    }

    private bool showingFilteredHighlights = true;
    public void ShowHideFilteredHighlights()
    {
        ShowHideHelper(filteredHighlightBillboards, !showingFilteredHighlights);
    }

    private void ShowHideHelper(List<Entity> entities, bool showOrHide)
    {
        if (showOrHide) // shows
        {
            foreach (Entity entity in entities)
            {
                try { entityManager.RemoveComponent<Disabled>(entity); } catch { }
            }
        } 
        else if (!showOrHide) // hides
        {
            foreach (Entity entity in entities)
            {
                entityManager.AddComponentData( entity, new Disabled() );
            }
        }
    }

    private List<Entity> BatchInstantiateHelper(GameObject prefab, List<float3> positions)
    {
        List<Entity> entities = new List<Entity>();

        foreach (float3 position in positions)
        {
            Entity entity = InstantiateHelper(prefab, position);
            entities.Add(entity);
        }
        return entities;
    }

    private Entity InstantiateHelper(GameObject prefab, float3 position)
    {
        Entity newEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy( prefab, gameObjectConversionSettings );
        Entity newEntity = entityManager.Instantiate( newEntityPrefab );

        Translation translation = new Translation() { Value = position };
        entityManager.AddComponentData( newEntity, translation );

        return newEntity;
    }
}