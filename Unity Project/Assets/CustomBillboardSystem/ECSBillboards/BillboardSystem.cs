using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;


public class BillboardSystem : ComponentSystem
{
    public Camera sceneCamera;

    protected override void OnCreate()
    {
        sceneCamera = Camera.main;

        Entities.WithAll<BillboardData>().ForEach((ref CompositeScale cScale, ref BillboardData billboardData) =>
        {
            cScale.Value = float4x4.Scale(billboardData.initialScale, billboardData.initialScale, billboardData.initialScale);
        });
    }

    protected override void OnUpdate()
    {
        Entities.WithAll<BillboardData>().ForEach((ref LocalToWorld ltw, ref Translation trans, ref Rotation rot, ref BillboardData billboardData) =>
        {
            Vector3 relativePos = - new float3(trans.Value.x, trans.Value.y, trans.Value.z); //position of entity
            Quaternion rotation = Quaternion.LookRotation(relativePos, Vector3.zero);
            float3 asEuler = new float3
            (
                rotation.eulerAngles.x, 
                rotation.eulerAngles.y, 
                rotation.eulerAngles.z * billboardData.yawAngleOffset
            );
            rot.Value = rotation;
        });
    }
}