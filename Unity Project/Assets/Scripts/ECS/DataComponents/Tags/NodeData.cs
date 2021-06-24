using Unity.Entities;
using Unity.Collections;
using System;

[GenerateAuthoringComponent]
public struct NodeData : IComponentData
{
    // fix error CS0723: Cannot declare a variable of static type 'FixedString'
    //public FixedString ID;
    public float value;
}