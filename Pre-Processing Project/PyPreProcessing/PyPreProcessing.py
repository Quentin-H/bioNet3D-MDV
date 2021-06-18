import sys
import igraph

print('Version is', sys.version)
print('MDV PreProcessor Version 0.1')
print(' ')
print(' ')

# E:\Quentin\Github Repositories\bioNet3D-MDV\Sample Files\4932.node_map.txt
# E:\Quentin\Github Repositories\bioNet3D-MDV\Sample Files\features_ranked_per_phenotype.txt
# E:\Quentin\Github Repositories\bioNet3D-MDV\Sample Files\4932.blastp_homology.edge

# Taking input from the user /Users/username/Desktop/sample.txt
nodePath = input("Enter node file path... ")

scorePath = input("Enter node score file path... ")

edgePath = input("Enter edge file path... ")

outputPath = input("Enter output destination, leave blank for default... ")
if not outputPath:
    outputPath = 'E:/Quentin/Github Repositories/bioNet3D-MDV/Sample Files/'

# Prompt user for what graphing option they want
print("-------------------------------")
print("layout_fruchterman_reingold_3d")
print("layout_kamada_kawai_3d")
print("layout_random_3d")
print("layout_sphere")
print("-------------------------------")

graphOption = input("Enter desired graphing algorithm... ")

# Get files from input file paths (Add error handling here)
nodeFile = open(nodePath, 'r').read()
scoreFile = open(scorePath, 'r').read()
edgeFile = open(edgePath, 'r').read()

# Convert files to format for iGraph
# igraphNodes (What to do with score?)
# igraphEdges = Graph.Read_Edgelist(edgeFile)

# Generates the graph layout
graph = igraph.Graph(n=5, edges=[[0, 1], [2, 3]])
graph.vs["knowENG_ID"] = ["Gene1", "Gene2", "Gene3", "Gene4", "Gene5"]
graph.vs["Node_Value"] = [2, 3, 1, 4, 2]
graph.es["Edge_Weight"] = [-0.2, 0.3]
graphLayout = graph.layout(graphOption)

# Converts the layout object to a string 
# The string only has node data, but they are layed out according to the inputted edges,
# So we can get the edges from the original edge file in Unity and everything will be fine and dandy
layoutString = ''

i = 0
for coordinate in graphLayout:
    currentLine = graph.vs[i]["knowENG_ID"]  + " " + str(coordinate) + " " + str(graph.vs[i]["Node_Value"]) + "\n"
    layoutString = layoutString + currentLine
    i += 1

print("\n" + layoutString)

# Saves the string we created as a "massive dataset visualizer layout file"
outputFile = open(outputPath + "output.mdvl", "w")
outputFile.write(layoutString)
outputFile.close