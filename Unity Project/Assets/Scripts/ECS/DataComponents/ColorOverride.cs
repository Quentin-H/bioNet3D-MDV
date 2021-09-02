using Unity.Entities;
//using Unity.Collections;
using Unity.Rendering;
using Unity.Mathematics;
using System;

[Serializable]
[MaterialProperty("_Color", MaterialPropertyFormat.Float4)]
public struct ColorOverride : IComponentData
{
    public float4 Value;
}