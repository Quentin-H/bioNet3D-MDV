using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class NodeObjectScaling : MonoBehaviour
{
    EntityManager entityManager => World.DefaultGameObjectInjectionWorld.EntityManager;
    public NetworkSceneManager networkSceneManager;
    public Slider scaleSlider;
    public float nodeScale {get; set;}
    
    
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
        
        foreach(Entity entity in nodes)
        {
            var ltw = entityManager.GetComponentData<LocalToWorld>(entity);
            entityManager.SetComponentData<LocalToWorld>( entity , new LocalToWorld{
                Value = float4x4.TRS(
                    translation:    ltw.Position,
                    rotation:       ltw.Rotation,
                    scale:          nodeScale
                )
            });
        }
    }
}