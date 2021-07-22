import sys
import igraph

print('Python version ', sys.version)
print('MDV PreProcessor Version 0.9')
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
print("layout_fruchterman_reingold_3d, fr3d (force directed)")
print("layout_kamada_kawai_3d, kk3d")
print("layout_random_3d, random_3d") 
print("layout_sphere, sphere")
print("-------------------------------")
graphOption = input("Enter desired graphing algorithm... ")

# create an igraph containing just the nodes from the inputfile 
graph = igraph.Graph(
    vertex_attrs={
        "displayName": "",
        "description": "",
        "networkRank": 0,
        "baselineScore": 0,
        # degrees is fetched with a function and the feature ID is the name of the vertex
    }, 
    edge_attrs={
        "Edge_Weight": 0
    })

i = 1 # first line has headers
for nodeLine in nodeFileLines: # go through every gene in the file and add it as a node to the graph
    featureID = nodeLine.split()[0]
    dName = nodeLine.split()[3]
    desc = nodeLine.split("\t")[4]
    nRank = i
    bScore = 0

    #for scoreLine in scoreFileLines: # searches for the baseline score in the score file
    #    if scoreLine.split()[1] == featureID: 
    #        bScore = scoreLine.split()[4]
    #        break
    
    # Sets the name of the vertex as the knowENG ID, this lets us refer to the vertex by ID rather than index, has one attribute
    graph.add_vertex(name = featureID, displayName = dName, description = desc, networkRank = nRank, baselineScore = bScore)
    i += 1

# go through the edge input file for each edge and create an edge between both genes (which are nodes in the network due to the previous step)
for edgeLine in edgeFileLines:
    node1 = edgeLine.split()[0]
    node2 = edgeLine.split()[1]
    weight = edgeLine.split()[2]
    graph.add_edge(node1, node2, Edge_Weight = weight)

# Runs the layout algorythm the user chose earlier
graphLayout = graph.layout(graphOption)

# Converts the layout object to a string 
# The string only has node data, but they are layed out according to the inputted edges,
# so we can get the edges from the original edge file in Unity and everything will be fine and dandy
layoutString = ''
i = 0
for coordinate in graphLayout:
    currentLine = (graph.vs[i]["name"] # feature ID
    + "|" + str(coordinate) 
    + "|" + str(graph.vs[i]["displayName"]) 
    + "|" + str(graph.vs[i]["description"]) 
    + "|" + str(graph.vs[i]["networkRank"]) 
    + "|" + str(graph.vs[i]["baselineScore"]) 
    + "|" + str(graph.vs[i].degree())
    + "\n")

    layoutString = layoutString + currentLine
    i += 1

# Saves the string we created as a "massive dataset visualizer layout file"
outputFile = open(outputPath + ("output - " + graphOption + ".mdvl"), "w")
outputFile.write(layoutString)
outputFile.close