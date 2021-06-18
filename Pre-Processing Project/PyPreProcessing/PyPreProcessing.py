import sys
import igraph

print('Python version ', sys.version)
print('MDV PreProcessor Version 0.2')
print(' ')
print(' ')  # E:\Quentin\Github Repositories\bioNet3D-MDV\Sample Files\9606.reactome_PPI_reaction.edge


nodePath = input("Enter node file path... ")         # E:\Quentin\Github Repositories\bioNet3D-MDV\Sample Files\4932.node_map.txt
# scorePath = input("Enter node score file path... ")  # E:\Quentin\Github Repositories\bioNet3D-MDV\Sample Files\features_ranked_per_phenotype.txt
edgePath = input("Enter edge file path... ")         # E:\Quentin\Github Repositories\bioNet3D-MDV\Sample Files\4932.blastp_homology.edge

outputPath = input("Enter output destination, leave blank for default... ")
if not outputPath:
    outputPath = 'E:/Quentin/Github Repositories/bioNet3D-MDV/Sample Files/'
nodeFile = open(nodePath, 'r').read() # add error handling for invalid paths
# scoreFile = open(scorePath, 'r').read() 
edgeFile = open(edgePath, 'r').read()

print("-------------------------------")
print("layout_fruchterman_reingold_3d")
print("layout_kamada_kawai_3d") # kk3d
print("layout_random_3d") 
print("layout_sphere")
print("-------------------------------")
graphOption = input("Enter desired graphing algorithm... ")

# take edges and nodes from given files and put them into an igraph graph


# Creates a test graph
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