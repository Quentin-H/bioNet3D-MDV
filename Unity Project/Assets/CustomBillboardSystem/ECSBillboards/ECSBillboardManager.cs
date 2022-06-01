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

    private void Awake()
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
            initialScale = 10f,
            yawAngleOffset = 0.0f
        });
    }

    public void SetTopRankHighlights(List<float3> positions)
    {
        topRankBillboards = BatchInstantiateHelper(topRankPrefab, positions);

        foreach(Entity entity in topRankBillboards)
        {
            entityManager.SetComponentData<BillboardData>(entity, new BillboardData 
            {
                initialScale = 10f,
                yawAngleOffset = 0.0f
            });
        }
    }

    public void SetTopBaselineHighlights(List<float3> positions)
    {
        topBaselineBillboards = BatchInstantiateHelper(topBaselinePrefab, positions);

        foreach(Entity entity in topBaselineBillboards)
        {
            entityManager.SetComponentData<BillboardData>(entity, new BillboardData 
            {
                initialScale = 10f,
                yawAngleOffset = 45.0f
            });
        }
    }

    public void SetTopDegreeHighlights(List<float3> positions)
    {
        topDegreeBillboards = BatchInstantiateHelper(topDegreePrefab, positions);

        foreach(Entity entity in topDegreeBillboards)
        {
            entityManager.SetComponentData<BillboardData>(entity, new BillboardData 
            {
                initialScale = 10f,
                yawAngleOffset = 0.0f
            });
        }
    }

    public void SetFilteredHighlights(List<Entity> entitiesToHighlight) // not working
    {
        filteredHighlightBillboards = BatchInstantiateHelper(filteredHighlightPrefab, entitiesToHighlight);

        foreach(Entity entity in filteredHighlightBillboards)
        {
            entityManager.SetComponentData<BillboardData>(entity, new BillboardData 
            {
                initialScale = 1.5f,
                yawAngleOffset = 0.0f
            });
        }
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
        showingTopRankHighlights = !showingTopRankHighlights;
    }

    private bool showingTopBaselineHighlights = true;
    public void ShowHideTopBaselineHighlights()
    {
        ShowHideHelper(topBaselineBillboards, !showingTopBaselineHighlights);
        showingTopBaselineHighlights = !showingTopBaselineHighlights;
    }

    private bool showingTopDegreeHighlights = true;
    public void ShowHideTopDegreeHighlights()
    {
        ShowHideHelper(topDegreeBillboards, !showingTopDegreeHighlights);
        showingTopDegreeHighlights = !showingTopDegreeHighlights;
    }

    private bool showingFilteredHighlights = true;
    public void ShowHideFilteredHighlights()
    {
        ShowHideHelper(filteredHighlightBillboards, !showingFilteredHighlights);
        showingFilteredHighlights = !showingFilteredHighlights;
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
        List<Entity> billBoardEntities = new List<Entity>();

        foreach (float3 position in positions)
        {
            Entity entity = InstantiateHelper(prefab, position);
            billBoardEntities.Add(entity);
        }
        return billBoardEntities;
    }

    private List<Entity> BatchInstantiateHelper(GameObject prefab, List<Entity> entitiesToHighlight)
    {
        List<Entity> billboardEntities = new List<Entity>();

        foreach (Entity entity in entitiesToHighlight)
        {
            float3 position = entityManager.GetComponentData<Translation>(entity).Value;
            Entity newEntity = InstantiateHelper(prefab, position);
            billboardEntities.Add(newEntity);
        }
        return billboardEntities;
    }

    private Entity InstantiateHelper(GameObject prefab, float3 position)
    {
        //Entity newEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy( prefab, gameObjectConversionSettings );
        Entity newEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy( prefab, gameObjectConversionSettings );

        Entity newEntity = entityManager.Instantiate( newEntityPrefab );

        Translation translation = new Translation() { Value = position };
        entityManager.AddComponentData( newEntity, translation );

        return newEntity;
    }
}