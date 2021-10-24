import sys
import igraph
import time
from datetime import datetime
from datetime import date
from decimal import Decimal

def testPrint():
    print( "Worked!")

#@staticmethod
def fileToGraph(nodePath, scorePath, edgePath):
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
        print(len(edgeFileLines))
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
            "coordinates": ""
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

def stackoverflow(self, i=None):
        if i is None:
            print ('g')
        else:
            print ('h')


