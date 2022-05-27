# bioNet3D-MDV
Massive Dataset Visualizer

Notes:

- Typically I use float3 rather than vector3 because in newer versions of Unity 
anything that takes vector3 as an argument also accepts float3 and float3 does
not have many methods and therefore has a smaller overhead.

- A lot of times I use entities and nodes interchangeably. Nodes in the network are
represented as spheres, these are entities. All nodes are entities but not all entities are nodes.

- Find a license that requires credit, bans for profit use, prevents closed source redistribution, requires all derivative work to be public and open source

using 
- https://github.com/yasirkula/UnitySimpleFileBrowser
- XASOSOFTWARE Easy Color & Gradient Picker
- iGraph/Unity
- RangeSlider.cs by Ben MacKinnon
