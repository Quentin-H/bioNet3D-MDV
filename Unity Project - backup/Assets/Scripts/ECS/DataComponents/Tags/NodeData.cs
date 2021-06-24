using Unity.Entities;
using Unity.Collections;

[GenerateAuthoringComponent]
public struct NodeData : IComponentData
{
    public NativeString64 ID;
    public float value;
}