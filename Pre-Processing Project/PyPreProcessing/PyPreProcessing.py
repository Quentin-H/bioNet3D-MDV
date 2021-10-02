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

# for debugging add error handling for incorrect paths because it causes a bunch of impoosible to understand errors if they are wrong

# for yeast
#nodePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Yeast Sample/4932.node_map.txt"
#scorePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Yeast Sample/features_ranked_per_phenotype.txt"
#edgePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Yeast Sample/4932.blastp_homology.edge"

# for small human
#nodePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Small Human Sample/9606.node_map.txt"
#scorePath = ""
#edgePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Small Human Sample/9606.reactome_PPI_reaction.edge"

# for large human
nodePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Big Human Sample (Amin's Dataset)/9606.node_map.txt"
scorePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Big Human Sample (Amin's Dataset)/BDI_PPI.edge"
edgePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Big Human Sample (Amin's Dataset)/Doxorubicin_bootstrap_net_correlation_pearson (Score file).txt"

outputPath = input("Enter output destination, leave blank for default... ")
if not outputPath.strip():
    outputPath = 'C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/'

analysisRawInput = input("Do clustering analysis? (y/n) ")
doClusteringAnalysis = False;
if analysisRawInput.strip() == "y":
    doClusteringAnalysis = True

start_time = time.time()

nodeFileLines = []
try:
    nodeFileLines = open(nodePath, 'r').readlines()
except:
    print("Opening node file failed, quitting...")
    sys.exit()

scoreFileLines = [];
try:
    scoreFileLines = open(scorePath, 'r').readlines() 
except:
    print("Opening score file failed, continuing without...")

edgeFileLines = []
try:
    edgeFileLines = open(edgePath, 'r').readlines()
except:
    print("Opening edge file failed, quitting...")
    sys.exit()

timeToImport = "%s seconds to import data" % (time.time() - start_time)
print("Took " +  "%s seconds to import data" % (time.time() - start_time))

start_time = time.time()

# create an empty igraph
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

scoreParseFails = 0
nodeParseFails = 0
edgeParseFails = 0
i = 1 # first line has headers
for nodeLine in nodeFileLines: # go through every gene in the file and add it as a node to the graph
    try:
        featureID = nodeLine.split()[0]
        dName = nodeLine.split()[3]
        desc = nodeLine.split("\t")[4]
        nRank = i
        bScore = 0

        if not(len(scoreFileLines) < 2):
            try:
                for scoreLine in scoreFileLines: # searches for the baseline score in the score file 
                    if scoreLine.split()[1] == featureID: 
                        bScore = Decimal(scoreLine.split()[4])
                        break
            except:
                scoreParseFails += 1
# Sets the name of the vertex as the knowENG ID, this lets us refer to the vertex by ID rather than index, has one attribute
        graph.add_vertex(name = featureID, displayName = dName, description = desc, networkRank = nRank, baselineScore = bScore)
        i += 1
    except:
        nodeParseFails += 1

# go through the edge input file for each edge and create an edge between both genes (which are nodes in the network due to the previous step)
for edgeLine in edgeFileLines:
    try:
        node1 = edgeLine.split()[0]
        node2 = edgeLine.split()[1]
        if node1 != node2: # we don't need self connections
            weight = Decimal(edgeLine.split()[2])
            graph.add_edge(node1, node2, Edge_Weight = weight)
    except:
        edgeParseFails += 1
timeToGenerateiGraph = "%s seconds to generate iGraph" % (time.time() - start_time)
print("Took " +  "%s seconds to generate iGraph" % (time.time() - start_time))
 
print("Connected components: " + str(len(graph.clusters()))) 

start_time = time.time()
clusteredGraph = graph.community_multilevel(weights = "Edge_Weight") # clusteredGraph is a vertex clustering object
timeToLouvainCluster = "%s seconds to cluster with Louvain" % (time.time() - start_time)
print("Took " + "%s seconds to cluster with Louvain" % (time.time() - start_time))
print("Clusters after Louvain: " + str(clusteredGraph.__len__()))
print("Modularity: " + str(graph.modularity(clusteredGraph, weights = "Edge_Weight")))

# Find each cluster 





# Converts the graph to a string 
# The string only has node data, but they are layed out according to the inputted edges,
# so we can get the edges from the original edge file in Unity and everything will be fine and dandy
graphString = ""
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
    histogramDict = { 1 : 0 } # create histogram of cluster sizes
    
    for subGraph in clusteredGraph.subgraphs():
        try: 
            histogramDict[subGraph.vcount()] = histogramDict[subGraph.vcount()] + 1
        except:
            histogramDict[subGraph.vcount()] = 1
    
    clusterSizeHistString = "Times: ," + timeToImport + "," + timeToGenerateiGraph + "," + timeToLouvainCluster + "\n"
    clusterSizeHistString += "\n"
    clusterSizeHistString += "node parse fails,score parse fails,edge parse fails\n"
    clusterSizeHistString += str(nodeParseFails) + "," + str(scoreParseFails) + "," + str(edgeParseFails) + "\n"
    clusterSizeHistString += "\n"
    clusterSizeHistString += "connected components,clusters after louvain,modularity after louvain\n"
    clusterSizeHistString += str(len(graph.clusters())) + "," + str(clusteredGraph.__len__()) + "," + str(graph.modularity(clusteredGraph, weights = "Edge_Weight")) + "\n"
    clusterSizeHistString += "\n"
    clusterSizeHistString += "cluster size,number of clusters\n"
    for clusterSize, numClusters in histogramDict.items():
        clusterSizeHistString  += str(clusterSize) + "," + str(numClusters) + "\n"

    # find if there is a function that gets the total amount of vertices in all the subgraphs of a vertex clustering object
    #clusterSizeHistString += "\n" + "Nodes after Louvain clustering (should not be different than nodes in node file - node parse fails: " + str(clusteredGraph.subgraphs()) + "\n"
 
    clusterSizeHistOutputFile = open(outputPath + ("cluster size histogram - " + str(date.today()) + ".csv"), "w")
    clusterSizeHistOutputFile.write(clusterSizeHistString)
    clusterSizeHistOutputFile.close()