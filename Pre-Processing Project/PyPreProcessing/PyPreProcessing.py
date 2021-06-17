
import sys

print('version is', sys.version)
print('sys.argv is', sys.argv)

# Taking input from the user
nodePath = input("Enter node file path... ")
scorePath = input("Enter node score file path... ")
edgePath = input("Enter edge file path... ")
outputPath = input("Enter output destination, leave blank for default... ")

# Debug Printout
print("Node file path: ", nodePath)
print("Score file path: ", scorePath)
print("Edge file path: ", edgePath)
print("Output file path: ", outputPath)

# Prompt user for what graphing option they want
print("-------------------------------")
print("[1] Placeholder Graphing Option")
print("[2] Placeholder Graphing Option")
print("[3] Placeholder Graphing Option")
print("-------------------------------")


graphOption = input("Enter graphing option... ")

# Turn number into graph type

# Process data and generate output file + place it in outputPath location