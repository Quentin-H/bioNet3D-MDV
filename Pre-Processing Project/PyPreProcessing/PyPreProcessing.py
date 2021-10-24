import sys
import igraph
import time
from Functions import *
from datetime import datetime
from datetime import date
from decimal import Decimal

print('Python version ', sys.version)
print('MDV PreProcessor Version 0.9')
print(' ')
print(' ')  
testPrint()
# for final release
# nodePath = input("Enter node file path... ")         # C:\Users\Quentin Herzig\GitHub Repositories\bioNet3D-MDV\Sample Files\Yeast Sample\4932.node_map.txt
# scorePath = input("Enter node score file path... ")  # C:\Users\Quentin Herzig\GitHub Repositories\bioNet3D-MDV\Sample Files\Yeast Sample\features_ranked_per_phenotype.txt
# edgePath = input("Enter edge file path... ")         # C:\Users\Quentin Herzig\GitHub Repositories\bioNet3D-MDV\Sample Files\Yeast Sample\4932.blastp_homology.edge

# for yeast
nodePath = "C:/Users/Quentin/GitHub Repositories/bioNet3D-MDV/Sample Files/Yeast Sample/4932.node_map.txt"
scorePath = "C:/Users/Quentin/GitHub Repositories/bioNet3D-MDV/Sample Files/Yeast Sample/features_ranked_per_phenotype.txt"
edgePath = "C:/Users/Quentin/GitHub Repositories/bioNet3D-MDV/Sample Files/Yeast Sample/4932.blastp_homology.edge"

# for small human
#nodePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Small Human Sample/9606.node_map.txt"
#scorePath = ""
#edgePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Small Human Sample/9606.reactome_PPI_reaction.edge"

# for large human
#scorePath = ""
#nodePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Big Human Sample (Amin's Dataset)/9606.node_map.txt"
#scorePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Big Human Sample (Amin's Dataset)/Doxorubicin_bootstrap_net_correlation_pearson (Score file).txt"
#edgePath = "C:/Users/Quentin Herzig/GitHub Repositories/bioNet3D-MDV/Sample Files/Big Human Sample (Amin's Dataset)/BDI_PPI.edge"

outputPath = input("Enter output destination, leave blank for default... ")
if not outputPath.strip():
    outputPath = 'C:/Users/Quentin/GitHub Repositories/bioNet3D-MDV/Sample Files/'

analysisRawInput = input("Do clustering analysis? (y/n) ")
doClusteringAnalysis = False;
if analysisRawInput.strip() == "y":
    doClusteringAnalysis = True

import_time = time.time()

nodeFileLines = []
try:
    nodeFileLines = open(nodePath, 'r').readlines()
except:
    print("Opening node file failed, quitting...")
    sys.exit()

scoreFileLines = []
try:
    scoreFileLines = open(scorePath, 'r').readlines() 
except:
    print("Opening score file failed, continuing without...")

edgeFileLines = []
try:
    edgeFileLines = open(edgePath, 'r').readlines()
    print(len(edgeFileLines))
except:
    print("Opening edge file failed, quitting...")
    sys.exit()

timeToImport = "%s" % (time.time() - import_time)
print("\nTook " +  "%s seconds to import data" % (time.time() - import_time))

graph_start_time = time.time()

# create an empty igraph
graph = igraph.Graph(
    vertex_attrs={
        "displayName": "",
        "description": "",
        "networkRank": 0,
        "baselineScore": 0,
        # degrees is fetched with a function and the feature ID is the name of the vertex
    }, edge_attrs={"Edge_Weight": 0})

scoreParseFails = 0
nodeParseFails = 0
edgeParseFails = 0

node_time = time.time()
nodeParsePercent = 0
i = 1 # first line has headers
for nodeLine in nodeFileLines: # go through every gene in the file and add it as a node to the graph
    nodeParsePercent = round(100 * (i / len(nodeFileLines)), 3)
    sys.stdout.write("\r{0}".format("Node parsing: "+ str(nodeParsePercent) + "% done"))
    sys.stdout.flush()

    try:
        featureID = nodeLine.split()[0].strip()
        dName = nodeLine.split()[3].strip()
        desc = nodeLine.split("\t")[4].strip()
        nRank = i
        bScore = 0

        if not(len(scoreFileLines) < 2):
            try:
                for scoreLine in scoreFileLines: # searches for the baseline score in the score file 
                    if scoreLine.split()[1] == featureID: 
                        bScore = Decimal(scoreLine.split()[4].strip())
                        break
            except:
                scoreParseFails += 1
        # Sets the name of the vertex as the knowENG ID, this lets us refer to the vertex by ID rather than index, has one attribute
        graph.add_vertex(name = featureID, displayName = dName, description = desc, networkRank = nRank, baselineScore = bScore)
        i += 1
    except:
        nodeParseFails += 1
print("\nNode parsing took " +  "%s seconds" % (time.time() - node_time))

edge_time = time.time()
edgeParsePercent = 0;
i = 0;
# go through the edge input file for each edge and create an edge between both genes (which are nodes in the network due to the previous step)
for edgeLine in edgeFileLines:
    edgeParsePercent = round(100 * (i / len(edgeFileLines)), 3)
    sys.stdout.write("\r{0}".format("Edge parsing: "+ str(edgeParsePercent) + "% done"))
    sys.stdout.flush()

    try:
        node1 = edgeLine.split()[0].strip()
        node2 = edgeLine.split()[1].strip()

        if node1 != node2: # we don't need self connections 
            #weight = Decimal(edgeLine.split()[2])
            #graph.add_edge(node1, node2, Edge_Weight = weight) we don't care about weights for now
            graph.add_edge(node1, node2, Edge_Weight = 0)
    except:
        #print(node1)
        #print(node2)
        edgeParseFails += 1

    i += 1
print("\n" + str(i))

print("\nEdge parsing took " +  "%s seconds" % (time.time() - edge_time))

to_delete_ids = [v.index for v in graph.vs if  v.degree() < 2]
graph.delete_vertices(to_delete_ids)

timeToGenerateiGraph = "%s" % (time.time() - graph_start_time)
print("Took " +  "%s seconds to generate iGraph" % (time.time() - graph_start_time))
 
print("Connected components: " + str(len(graph.clusters()))) 

louvain_start_time = time.time()
clusteredGraph = graph.community_multilevel() # clusteredGraph is a vertex clustering object
timeToLouvainCluster = "%s" % (time.time() - louvain_start_time)
print("Took " + "%s seconds to cluster with Louvain" % (time.time() - louvain_start_time))
print("Clusters after Louvain: " + str(clusteredGraph.__len__()))
print("Modularity: " + str(graph.modularity(clusteredGraph, weights = "Edge_Weight")))

miscBucketGraph = igraph.Graph(
        vertex_attrs={
            "displayName": "",
            "description": "",
            "networkRank": 0,
            "baselineScore": 0,
        }, edge_attrs={"Edge_Weight": 0})

graphString = ""

clusterNum = 0 # 0 will be the <5 bucket graph
for vClusterAsGraph in clusteredGraph.subgraphs():

    if vClusterAsGraph.vcount() > 5:
        vClusterLayout = vClusterAsGraph.layout("fr3d")
        #print(vClusterLayout.boundaries())
        #vClusterLayout.center()
        j = 0
        for coordinate in vClusterLayout:
            connectionListStr = ""
            for neighbor in vClusterAsGraph.vs[j].neighbors():
                connectionListStr += neighbor["name"] + "," 
                                      
            k = 0
            for c in coordinate :
                coordinate[k] = c + (25 * clusterNum)
                k += 1
            modCoordinate = coordinate

            currentLine = (vClusterAsGraph.vs[j]["name"] # feature ID
            + "|" + str(modCoordinate) 
            #+ "|" + str(coordinate)
            + "|" + str(vClusterAsGraph.vs[j]["displayName"]) 
            + "|" + str(vClusterAsGraph.vs[j]["description"])
            #+ "|" + str(clusterNum)
            + "|" + str(vClusterAsGraph.vs[j]["networkRank"]) 
            + "|" + str(vClusterAsGraph.vs[j]["baselineScore"]) 
            + "|" + str(vClusterAsGraph.vs[j].degree())
            + "|" + str(clusterNum)
            + "|" + connectionListStr
            + "\n")
            graphString = graphString + currentLine
            j += 1
        clusterNum += 1
    else:
         miscBucketGraph.__or__(vClusterAsGraph)

miscBucketLayout = miscBucketGraph.layout("fr3d")
j = 0
for coordinate in miscBucketLayout:
    connectionListStr = ""
    for neighbor in vClusterAsGraph.vs[j].neighbors():
        connectionListStr += neighbor["name"] + "," 
                                      
        k = 0
        for c in coordinate :
            coordinate[k] = c + (25 * j)
            k += 1

        modCoordinate = coordinate
        currentLine = (vClusterAsGraph.vs[j]["name"] # feature ID
        + "|" + str(modCoordinate) 
        + "|" + str(vClusterAsGraph.vs[j]["displayName"]) 
        + "|" + str(vClusterAsGraph.vs[j]["description"])
        #+ "|" + str(clusterNum)
        + "|" + str(vClusterAsGraph.vs[j]["networkRank"]) 
        + "|" + str(vClusterAsGraph.vs[j]["baselineScore"]) 
        + "|" + str(vClusterAsGraph.vs[j].degree())
        + "|" + str(clusterNum)
        + "|" + connectionListStr
        + "\n")
        graphString = graphString + currentLine
        j += 1


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
    
    clusterSizeHistString = "Times (s): ," + timeToImport + "," + timeToGenerateiGraph + "," + timeToLouvainCluster + "\n"
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