using Unity.Collections;
using Unity.Mathematics;

namespace NodeViz
{
    public struct NodeEdgePosition
    {
        public FixedString32 nodeAName;
        public float3 nodeACoords;
    
        public FixedString32 nodeBName;
        public float3 nodeBCoords;

        public double weight;

        public NodeEdgePosition(FixedString32 setNodeAName, float3 setNodeACoords, FixedString32 setNodeBName, float3 setNodeBCoords, double setWeight)
        {
            nodeACoords = setNodeACoords;
            nodeAName = setNodeAName;
            nodeBCoords = setNodeBCoords;
            nodeBName = setNodeBName;
            weight = setWeight;
        }
    }
}