import sys
from igraph import *
import igraph
import time
import pyhull
import pyhull.convex_hull
from datetime import datetime
from datetime import date
from decimal import Decimal
import sphvoronoi


def FileToGraph(nodePath, scorePath, edgePath):
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
        scoreFileLines = None
        print("Opening score file failed, continuing without...")

    edgeFileLines = []
    try:
        edgeFileLines = open(edgePath, 'r').readlines()
    except:
        print("Opening edge file failed, quitting...")
        print(edgePath)
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
            "coordinates": "",
			"cluster": -1
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

    timeToGenerateiGraph = "%s" % (time.time() - graph_start_time)
    print("Took " +  "%s seconds to generate iGraph" % (time.time() - graph_start_time))

    to_delete_ids = [v.index for v in graph.vs if  v.degree() < 2]
    graph.delete_vertices(to_delete_ids)

    return graph


def outputData(graphList, outputPath):
	graphString = ""
	i = 0
	for subgraph in graphList:
		graphString += GraphToStr(subgraph)
		i +=1

	# "massive dataset visualizer layout file"
	outputFile = open(outputPath + ("output - " + str(date.today()) + ".mdvl"), "w")
	outputFile.write(graphString)
	outputFile.close()


def outputHist(graphList, outputPath):
	histogramDict = { 1 : 0 }

	for subGraph in graphList:
		try: 
			histogramDict[subGraph.vcount()] = histogramDict[subGraph.vcount()] + 1
		except:
			histogramDict[subGraph.vcount()] = 1

	clusterSizeHistString = "cluster size,number of clusters\n"
	for clusterSize, numClusters in histogramDict.items():
		clusterSizeHistString  += str(clusterSize) + "," + str(numClusters) + "\n"

	clusterSizeHistOutputFile = open(outputPath + ("cluster size histogram - " + str(date.today()) + ".csv"), "w")
	clusterSizeHistOutputFile.write(clusterSizeHistString)
	clusterSizeHistOutputFile.close()


# maybe automate scale arg by basing it on number of nodes in each subgraph somehow
def generateOnSpherePos(numOfPos, scale):
	graph = igraph.Graph(numOfPos)
	posList = []
	layout = graph.layout("sphere")
	layout.center(0,0,0)
	layout.scale(scale)

	for coordinate in layout:
		posList.append(coordinate)

	return posList

#runs louvain and puts small clusters into a seperate graph
def lvnProcessing(graph):
	lvnClusteredGraph = graph.community_multilevel() # clusteredGraph is a vertex clustering object
	amountOfBiggerThan5 = 0
	for subgraph in lvnClusteredGraph.subgraphs():
		if subgraph.vcount() > 5:
			amountOfBiggerThan5 += 1
	amountOfClusters = amountOfBiggerThan5 + 1
	lessThan5List = []
	graphList = []
	clusterPosList = generateOnSpherePos(amountOfClusters, 75)
	clusterNum = 1 # 0 will be the <5 bucket graph
	for subgraph in lvnClusteredGraph.subgraphs():
		if subgraph.vcount() > 5:
			graphList.append(subgraph)
			
		else:
			for v in subgraph.vs():
				v["cluster"] = clusterNum
			lessThan5List.append(subgraph.copy())
			clusterNum += 1

	miscBucketGraph = igraph.Graph(vertex_attrs={"displayName": "","description": "","networkRank": 0,"baselineScore": 0,"coordinates": 0,"cluster": 0}, edge_attrs={"Edge_Weight": 0})
	miscBucketGraph = miscBucketGraph.disjoint_union(lessThan5List)
	graphList.insert(0, miscBucketGraph)
	return graphList

def genNodePos(inputGraphList):
	graphListWithPos = []
	graphList = inputGraphList
	miscBucketLayout = graphList[0].layout("fr3d") # maybe try drl_3d too
	miscBucketLayout.center(-160,0,0)
	newMiscGraph = graphList[0]

	i = 0
	for coordinate in miscBucketLayout:
		graphList[0].vs[i]["coordinates"] = coordinate
		i += 1 
	graphListWithPos.append(newMiscGraph)

	posList = generateOnSpherePos(len(graphList), 75)

	i = 0
	for graph in graphList:
		newGraph = graph
		if i != 0:
			layout = graph.layout_fruchterman_reingold()
			# first number is diameter of circle
			sideL = (10 * math.sqrt(2)) # side length of largest square that fits in a r = 1 circle
			layout.fit_into(bbox = ( sideL, sideL) )

			j = 0
			for coordinate in layout: # for each node in the mini graph
				coord = [0, i * 5, 0]
				coord[0] = coordinate[0]
				coord[2] = coordinate[1]
				#do transformation here
				newGraph.vs[j]["coordinates"] = coord
				j += 1

			graphListWithPos.append(newGraph)
		i += 1
	
	#get pos on sphere for length - 1 and do fr3d on each cluster and set center to a pos on sphere
	return graphListWithPos


def genHullPos(inputGraphList):
	hull = pyhull.convex_hull.ConvexHull(generateOnSpherePos(320, 75), joggle=True )


	return ""


def GraphToStr(graph):
    outputStr = ""

    for node in graph.vs:
        connectionListStr = ""

        for neighbor in node.neighbors():
            connectionListStr += neighbor["name"] + "," 

        currentLine = (node["name"] # feature ID
            + "|" + str(node["coordinates"]) 
            + "|" + node["displayName"] 
            + "|" + node["description"]
            + "|" + str(node["networkRank"]) 
            + "|" + str(node["baselineScore"]) 
            + "|" + str(node.degree())
			+ "|" + str(node["cluster"])
            + "|" + connectionListStr
            + "\n")
        outputStr += currentLine
    return outputStr