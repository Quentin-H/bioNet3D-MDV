using Unity.Entities;
using Unity.Collections;

[GenerateAuthoringComponent]
public struct BillboardData : IComponentData
{
    public float initialScale;
    public float yawAngleOffset;
}