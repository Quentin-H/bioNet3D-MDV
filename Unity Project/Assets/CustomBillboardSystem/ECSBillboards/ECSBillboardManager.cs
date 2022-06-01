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
        
        Entity selectionPrefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy( selectionPrefab, gameObjectConversionSettings );
        selectionHighlight = entityManager.Instantiate( selectionPrefabEntity );
        
        Translation translation = new Translation() { Value = entityPos };
        entityManager.AddComponentData( selectionHighlight, translation );
        entityManager.SetComponentData<BillboardData>(selectionHighlight, new BillboardData 
        {
            initialScale = 3f,
            yawAngleOffset = 0.0f
        });

        Debug.Log(selectionHighlight);
    }

    public void SetTopRankHighlights(List<float3> positions)
    {

    }

    public void SetTopBaselineHighlights(List<float3> positions)
    {

    }

    public void SetTopDegreeHighlights(List<float3> positions)
    {

    }

    public void ClearFilteredHighlights()
    {

    }

    public void SetFilteredHighlights()
    {

    }

    private bool showingTopRankHighlights;
    public void ShowHideTopRankHighlights()
    {

    }

    private bool showingTopBaselineHighlights;
    public void ShowHideTopBaselineHighlights()
    {

    }

    private bool showingTopDegreeHighlights;
    public void ShowHideTopDegreeHighlights()
    {

    }

    private bool showingFilteredHighlights;
    public void ShowHideFilteredHighlights()
    {

    }
}