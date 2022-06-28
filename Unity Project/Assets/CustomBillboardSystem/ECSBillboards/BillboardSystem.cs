using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;


public class BillboardSystem : ComponentSystem
{
    private Camera sceneCamera;
    public float userScaling = 1f;

    protected override void OnCreate()
    {
        sceneCamera = Camera.main;
    }

    protected override void OnUpdate()
    {
        Entities.WithAll<BillboardData>().ForEach((ref LocalToWorld ltw, ref Translation trans, ref Rotation rot, ref BillboardData billboardData) =>
        {
            sceneCamera = Camera.main;
            Vector3 relativePos = sceneCamera.transform.position - new Vector3(trans.Value.x, trans.Value.y, trans.Value.z); //position of entity
            Quaternion rotation = Quaternion.LookRotation(relativePos, Vector3.up);
            float3 asEuler = new float3
            (
                rotation.eulerAngles.x, 
                rotation.eulerAngles.y, 
                rotation.eulerAngles.z + billboardData.yawAngleOffset
            );
            rotation.eulerAngles = asEuler;

            // if you want to scale the billboards with the camera distance (so its in screenspace instead of world space
            // add code here to the scale part)
            ltw = new LocalToWorld
            {
                Value = float4x4.TRS
                (
                    translation:    ltw.Position,
                    rotation:       rotation,
                    scale:          billboardData.initialScale * userScaling //* whatever needed for screenspace
                )
            };
        });
    }
}