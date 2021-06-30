using Unity.Entities;
using Unity.Collections;

[GenerateAuthoringComponent]
public struct NodeData : IComponentData
{
    public FixedString32 nodeName;
    public double nodeValue;
}