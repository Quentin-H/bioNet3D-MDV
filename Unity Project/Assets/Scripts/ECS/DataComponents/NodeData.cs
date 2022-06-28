using Unity.Entities;
using Unity.Collections;

[GenerateAuthoringComponent]
public struct NodeData : IComponentData
{
    public FixedString32 featureID; // (internal unique identifier for nodes)
    public FixedString32 displayName; // (Name to show users, sometimes different from featureID)
    public FixedString512 description; // might need 4096
    public int networkRank;
    public double baselineScore;
    public int degree;
    public int cluster;
}