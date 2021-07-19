# bioNet3D-MDV
KnowENG Massive Dataset Visualizer Component

Notes:

- Typically I use float3 rather than vector3 because in newer versions of Unity 
anything that takes vector3 as an argument also accepts float3 and float3 does
not have many methods and therefore has a smaller overhead.

- A lot of times I use entities and nodes interchangeably. Nodes in the network are
represented as cubes, these are entities. All nodes are entities but not all entities are nodes.


Layout idea: http://www.hiveplot.com/


Point and click ECS: https://reeseschultz.com/pointing-and-clicking-with-unity-ecs/


Links for figuring out ECS Material Overrides for node coloring:

https://forum.unity.com/threads/per-instance-material-params-support-in-entities-0-2.782207/page-2

https://docs.unity3d.com/Packages/com.unity.rendering.hybrid@0.10/manual/material_overrides.html?_ga=2.259909705.272889376.1624905245-1386990202.1623186125&_gl=1*180a0nb*_ga*MTM4Njk5MDIwMi4xNjIzMTg2MTI1*_ga_1S78EFL1W5*MTYyNTI0Mjk4Ni4zNi4xLjE2MjUyNDM5MzguNTk.

https://forum.unity.com/threads/dots-render-pipeline.752198/

https://forum.unity.com/threads/changing-material-parameters-in-pure-ecs.764804/

https://forum.unity.com/threads/how-to-change-color-of-material.874921/

https://forum.unity.com/threads/how-to-change-entitys-color-by-code-in-ecs.1045345/


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
