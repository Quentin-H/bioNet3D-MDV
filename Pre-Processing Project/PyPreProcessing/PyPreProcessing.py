import sys
import igraph
import time
from datetime import date
from decimal import Decimal

print('Python version ', sys.version)
print('MDV PreProcessor Version 0.9')
print(' ')
print(' ')  

# for final release
# nodePath = input("Enter node file path... ")         # C:\Users\Quentin Herzig\GitHub Repositories\bioNet3D-MDV\Sample Files\Yeast Sample\4932.node_map.txt
# scorePath = input("Enter node score file path... ")  # C:\Users\Quentin Herzig\GitHub Repositories\bioNet3D-MDV\Sample Files\Yeast Sample\features_ranked_per_phenotype.txt
# edgePath = input("Enter edge file path... ")         # C:\Users\Quentin Herzig\GitHub Repositories\bioNet3D-MDV\Sample Files\Yeast Sample\4932.blastp_homology.edge

# for debugging
nodePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Yeast Sample/4932.node_map.txt"
scorePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Yeast Sample/features_ranked_per_phenotype.txt"
edgePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Yeast Sample/4932.blastp_homology.edge"

outputPath = input("Enter output destination, leave blank for default... ")
if not outputPath:
    outputPath = 'C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/'

analysisRawInput = input("Do clustering analysis? (y/n)")
doClusteringAnalysis = False;
if analysisRawInput.strip() == "y":
    doClusteringAnalysis = True

start_time = time.time()
nodeFileLines = open(nodePath, 'r').readlines() # add error handling for invalid paths
scoreFileLines = open(scorePath, 'r').readlines() 
edgeFileLines = open(edgePath, 'r').readlines()

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

    for scoreLine in scoreFileLines: # searches for the baseline score in the score file 
        if scoreLine.split()[1] == featureID: 
            bScore = Decimal(scoreLine.split()[4])
            break
    
    # Sets the name of the vertex as the knowENG ID, this lets us refer to the vertex by ID rather than index, has one attribute
    graph.add_vertex(name = featureID, displayName = dName, description = desc, networkRank = nRank, baselineScore = bScore)
    i += 1

# go through the edge input file for each edge and create an edge between both genes (which are nodes in the network due to the previous step)
for edgeLine in edgeFileLines:
    node1 = edgeLine.split()[0]
    node2 = edgeLine.split()[1]
    if node1 != node2: # we don't need self connections
        weight = Decimal(edgeLine.split()[2])
        graph.add_edge(node1, node2, Edge_Weight = weight)


print("Took " +  "%s seconds to import data" % (time.time() - start_time))

start_time = time.time()
# Runs the community detection
clusteredGraph = graph.community_multilevel(weights = "Edge_Weight") # clusteredGraph is a vertext clustering object
print("Number of clusters: " + str(clusteredGraph.__len__()))
# print("Largest cluster:" + str(clusteredGraph.size(clusteredGraph.giant()))) not working idk why
print("Modularity: " + str(graph.modularity(clusteredGraph, weights = "Edge_Weight")))
print("Took " + "%s seconds to cluster" % (time.time() - start_time))


#
# Find each cluster 





# Converts the graph to a string 
# The string only has node data, but they are layed out according to the inputted edges,
# so we can get the edges from the original edge file in Unity and everything will be fine and dandy
graphString = ''
#i = 0
for i in range(graph.vcount()):
    currentLine = (graph.vs[i]["name"] # feature ID
    # + "|" + str(coordinate) 
    + "|" + str(graph.vs[i]["displayName"]) 
    + "|" + str(graph.vs[i]["description"]) 
    + "|" + str(graph.vs[i]["networkRank"]) 
    + "|" + str(graph.vs[i]["baselineScore"]) 
    + "|" + str(graph.vs[i].degree())
    + "\n")

    graphString = graphString + currentLine
    i += 1

# Saves the string we created as a "massive dataset visualizer layout file"
outputFile = open(outputPath + ("output - " + str(date.today()) + ".mdvl"), "w")
outputFile.write(graphString)
outputFile.close()


# go through each subgraph
if doClusteringAnalysis == True:
    statsString = "cluster number,vertices,edges\n";
    i = 1
    for subGraph in clusteredGraph.subgraphs():
        statsString += str(i) + "," + str(subGraph.vcount()) + "," + str(subGraph.ecount()) + "\n"
        i += 1

    statsOutput = open(outputPath + ("stats - " + str(date.today()) + ".csv"), "w")
    statsOutput.write(statsString)
    statsOutput.close()