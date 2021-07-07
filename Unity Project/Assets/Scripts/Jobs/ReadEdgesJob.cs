using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using NodeViz;

public struct ReadEdgesJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<FixedString512> rawEdgeFileLines; // maybe we can use 128?
    public NativeArray<NodeEdgePosition> result;

    public void Execute(int i)
    {
        FixedString512 currentLine = rawEdgeFileLines[i];
        //result[i] = currentLine;
    }
}