# bioNet3D-MDV
KnowENG Massive Dataset Visualizer Component

Notes:

- Typically I use float3 rather than vector3 because in newer versions of Unity 
anything that takes vector3 as an argument also accepts float3 and float3 does
not have many methods and therefore has a smaller overhead.

- A lot of times I use entities and nodes interchangeably. Nodes in the network are
represented as cubes, these are entities. All nodes are entities but not all entities are nodes.


using https://github.com/yasirkula/UnitySimpleFileBrowser

using XASOSOFTWARE Easy Color & Gradient Picker

using Star by Ali Coşkun from the Noun Project (Sprite)

using iGraph

using Python

using C# / Mono / .NET / Unity

using FlyCam (Writen by Windexglow 11-13-10.  Use it, edit it, steal it I don't care.
    Converted to C# 27-02-13 - no credit wanted.
    Reformatted and cleaned by Ryan Breaker 23-6-18) 
    https://gist.github.com/RyanBreaker/932dc35302787d2f39df6b614a50c0c9
