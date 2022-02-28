using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;


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

    public class Utilities
    {
        public float3 GetNodePositon(Entity entity, EntityManager entityManager)
        {
            return new float3(
                entityManager.GetComponentData<Translation>(entity).Value.x, 
                entityManager.GetComponentData<Translation>(entity).Value.y, 
                entityManager.GetComponentData<Translation>(entity).Value.z);
        }
    }
}