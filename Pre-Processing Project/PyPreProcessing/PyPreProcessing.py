import sys
import igraph

print('Python version ', sys.version)
print('MDV PreProcessor Version 0.7')
print(' ')
print(' ')  # E:\Quentin\Github Repositories\bioNet3D-MDV\Sample Files\9606.reactome_PPI_reaction.edge


nodePath = input("Enter node file path... ")         # E:\Quentin\Github Repositories\bioNet3D-MDV\Sample Files\4932.node_map.txt
scorePath = input("Enter node score file path... ")  # E:\Quentin\Github Repositories\bioNet3D-MDV\Sample Files\features_ranked_per_phenotype.txt
edgePath = input("Enter edge file path... ")         # E:\Quentin\Github Repositories\bioNet3D-MDV\Sample Files\4932.blastp_homology.edge

outputPath = input("Enter output destination, leave blank for default... ")
if not outputPath:
    outputPath = 'E:/Quentin/Github Repositories/bioNet3D-MDV/Sample Files/'
nodeFileLines = open(nodePath, 'r').readlines() # add error handling for invalid paths
scoreFileLines = open(scorePath, 'r').readlines() 
edgeFileLines = open(edgePath, 'r').readlines()

print("-------------------------------")
print("layout_fruchterman_reingold_3d")
print("layout_kamada_kawai_3d") # kk3d
print("layout_random_3d") 
print("layout_sphere")
print("-------------------------------")
graphOption = input("Enter desired graphing algorithm... ")

# create an igraph containing just the nodes from the inputfile 
graph = igraph.Graph(vertex_attrs={"Node_Value": 0}, edge_attrs={"Edge_Weight": 0})

i = 0
for nodeLine in nodeFileLines:
    nodeName = nodeLine.split()[0]

    vizScore = 0
    for scoreLine in scoreFileLines:
        if scoreLine.split()[1] == nodeName:
            vizScore = scoreLine.split()[5]
    
    # Sets the name of the vertex as the knowENG ID, this lets us refer to the vertex by ID rather than index, has one attribute
    graph.add_vertex(name = nodeName, Node_Value = vizScore)

# go through the edge input file for each edge the edges aren't changing the coordinates? at least with kk3d layout option
for edgeLine in edgeFileLines:
    node1 = edgeLine.split()[0]
    node2 = edgeLine.split()[1]
    weight = edgeLine.split()[2]
    graph.add_edge(node1, node2, Edge_Weight = weight)
# us the nodeIDs as indexes and make edges between connected nodes with the given weight


# Creates a test graph
testGraph = igraph.Graph(n=5, edges=[[0, 1], [2, 3]])
testGraph.vs["knowENG_ID"] = ["Gene1", "Gene2", "Gene3", "Gene4", "Gene5"]
testGraph.vs["Node_Value"] = [2, 3, 1, 4, 2]
testGraph.es["Edge_Weight"] = [-0.2, 0.3]
graphLayout = graph.layout(graphOption)

# Converts the layout object to a string 
# The string only has node data, but they are layed out according to the inputted edges,
# So we can get the edges from the original edge file in Unity and everything will be fine and dandy
layoutString = ''

i = 0
for coordinate in graphLayout:
    # print(graph.vs[i]["Node_Value"])
    currentLine = graph.vs[i]["name"]  + " " + str(coordinate) + " " + str(graph.vs[i]["Node_Value"]) + "\n"
    layoutString = layoutString + currentLine
    i += 1

# print("\n" + layoutString)

# Saves the string we created as a "massive dataset visualizer layout file"
outputFile = open(outputPath + "output.mdvl", "w")
outputFile.write(layoutString)
outputFile.close