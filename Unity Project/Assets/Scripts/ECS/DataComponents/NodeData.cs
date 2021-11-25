using Unity.Entities;
using Unity.Collections;

[GenerateAuthoringComponent]
public struct NodeData : IComponentData
{
    public FixedString32 featureID; // column 4 (internal name for nodes)
    public FixedString32 displayName; // column 5?? (Name to show users)
    public FixedString512 description; // might need 4096
    public int networkRank; // i + 1
    public double baselineScore;
    public int degree;
    public int cluster;
}