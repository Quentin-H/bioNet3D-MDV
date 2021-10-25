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

graph = fileToGraph(nodePath = nodePath, scorePath = scorePath, edgePath = edgePath)
 
print("Connected components: " + str(len(graph.clusters()))) 

louvain_start_time = time.time()
lvnClusteredGraph = graph.community_multilevel() # clusteredGraph is a vertex clustering object
timeToLouvainCluster = "%s" % (time.time() - louvain_start_time)
print("Took " + "%s seconds to cluster with Louvain" % (time.time() - louvain_start_time))
print("Clusters after Louvain: " + str(lvnClusteredGraph.__len__()))
print("Modularity: " + str(graph.modularity(lvnClusteredGraph, weights = "Edge_Weight")))

miscBucketGraph = igraph.Graph(
        vertex_attrs={
            "displayName": "",
            "description": "",
            "networkRank": 0,
            "baselineScore": 0,
            "coordinates": 0
        }, edge_attrs={"Edge_Weight": 0})
graphString = ""

#change this so that exporting to layout file is in a different block, and this block
#just assigns coordinates

clusterNum = 0 # 0 will be the <5 bucket graph
for vClusterAsGraph in lvnClusteredGraph.subgraphs():

    if vClusterAsGraph.vcount() > 5:
        vClusterLayout = vClusterAsGraph.layout("fr3d")
        
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
            vClusterAsGraph.vs[j]["coordinates"] = modCoordinate
            currentLine = (vClusterAsGraph.vs[j]["name"] # feature ID
            + "|" + str(vClusterAsGraph.vs[j]["coordinates"]) 
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
        vClusterAsGraph.vs[j]["coordinates"] = modCoordinate
        currentLine = (vClusterAsGraph.vs[j]["name"] # feature ID
        + "|" + str(vClusterAsGraph.vs[j]["coordinates"]) 
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
    
    for subGraph in lvnClusteredGraph.subgraphs():
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
    clusterSizeHistString += str(len(graph.clusters())) + "," + str(lvnClusteredGraph.__len__()) + "," + str(graph.modularity(lvnClusteredGraph, weights = "Edge_Weight")) + "\n"
    clusterSizeHistString += "\n"
    clusterSizeHistString += "cluster size,number of clusters\n"
    for clusterSize, numClusters in histogramDict.items():
        clusterSizeHistString  += str(clusterSize) + "," + str(numClusters) + "\n"

    # find if there is a function that gets the total amount of vertices in all the subgraphs of a vertex clustering object
    #clusterSizeHistString += "\n" + "Nodes after Louvain clustering (should not be different than nodes in node file - node parse fails: " + str(clusteredGraph.subgraphs()) + "\n"
 
    clusterSizeHistOutputFile = open(outputPath + ("cluster size histogram - " + str(date.today()) + ".csv"), "w")
    clusterSizeHistOutputFile.write(clusterSizeHistString)
    clusterSizeHistOutputFile.close()