using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using SphereCollider = Unity.Physics.SphereCollider;

public class NodeObjectScaling : MonoBehaviour
{
    private EntityManager entityManager => World.DefaultGameObjectInjectionWorld.EntityManager;
    [SerializeField] private NetworkSceneManager networkSceneManager;
    [SerializeField] private NetworkCamera networkCamera;
    [SerializeField] private Slider scaleSlider;
    public float nodeScale { get; set;} = 1;
    

    
    public void SetNodeScaleMax(string maxString)
    {
        scaleSlider.maxValue = float.Parse(maxString);
        ScaleNodes();
    }

    public void SetNodeScaleMin(string minString)
    {
        scaleSlider.minValue = float.Parse(minString);
        ScaleNodes();
    }

    public void ScaleNodes()
    {
        List<Entity> nodes = networkSceneManager.GetSimpleNodeEntityList();

        List<GameObject> topRankObjects = networkSceneManager.topNetworkRankObjects;
        List<GameObject> topBaselineObjects = networkSceneManager.topBaselineScoreObjects;
        List<GameObject> topDegreeObjects = networkSceneManager.topDegreeObjects;
        List<GameObject> highlightedBillboardObjects = networkCamera.highlightBillboardObjects;
        
        foreach(Entity entity in nodes)
        {
            //Modify entity's scale
            var ltw = entityManager.GetComponentData<LocalToWorld>(entity);
            entityManager.SetComponentData<LocalToWorld>( entity , new LocalToWorld{
                Value = float4x4.TRS(
                    translation:    ltw.Position,
                    rotation:       ltw.Rotation,
                    scale:          nodeScale
                )
            });

            //Modify entity's collider scale (this is supposed to fix the node selection bug when nodes are scaled larger)
            unsafe
            {
                // grab the sphere pointer
                SphereCollider* scPtr = (SphereCollider*)entityManager.GetComponentData<PhysicsCollider>(entity).ColliderPtr;              
                 // update the collider geometry
                var sphereGeometry = scPtr->Geometry;
                sphereGeometry.Radius = nodeScale;
                scPtr->Geometry = sphereGeometry;
            }
        }

        float3 scaleAsFloat3 = new float3(nodeScale, nodeScale, nodeScale);

        foreach(GameObject curObject in topRankObjects)
        {
            curObject.transform.localScale = scaleAsFloat3 * curObject.GetComponent<Billboard>().initialScale;
        }

        foreach(GameObject curObject in topBaselineObjects)
        {
            curObject.transform.localScale = scaleAsFloat3 * curObject.GetComponent<Billboard>().initialScale;
        }

        foreach(GameObject curObject in topDegreeObjects)
        {
            curObject.transform.localScale = scaleAsFloat3 * curObject.GetComponent<Billboard>().initialScale;
        }

        foreach(GameObject curObject in highlightedBillboardObjects)
        {
            curObject.transform.localScale = scaleAsFloat3 * curObject.GetComponent<Billboard>().initialScale;
        }
    }
}