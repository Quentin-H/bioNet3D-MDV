import sys
import time
from Functions import *
from datetime import datetime
from datetime import date

print('Python version ', sys.version)
print('MDV PreProcessor Version 0.9')
print(' ')
print(' ')  
# for final release
# nodePath = input("Enter node file path... ")         # C:\Users\Quentin Herzig\GitHub Repositories\bioNet3D-MDV\Sample Files\Yeast Sample\4932.node_map.txt
# scorePath = input("Enter node score file path... ")  # C:\Users\Quentin Herzig\GitHub Repositories\bioNet3D-MDV\Sample Files\Yeast Sample\features_ranked_per_phenotype.txt
# edgePath = input("Enter edge file path... ")         # C:\Users\Quentin Herzig\GitHub Repositories\bioNet3D-MDV\Sample Files\Yeast Sample\4932.blastp_homology.edge

# for yeast
nPath = "C:/Users/Quentin/GitHub Repositories/bioNet3D-MDV/Sample Files/Yeast Sample/4932.node_map.txt"
sPath = "C:/Users/Quentin/GitHub Repositories/bioNet3D-MDV/Sample Files/Yeast Sample/features_ranked_per_phenotype.txt"
ePath = "C:/Users/Quentin/GitHub Repositories/bioNet3D-MDV/Sample Files/Yeast Sample/4932.blastp_homology.edge"

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

analysisRawInput = input("Do clustering analysis? (y/n)")
doClusteringAnalysis = False;
if analysisRawInput.strip() == "y":
	doClusteringAnalysis = True

graph = Functions.FileToGraph(nodePath = nPath, scorePath = sPath, edgePath = ePath)
graphList = Functions.lvnProcessing(graph)
graphList = Functions.genNodePos(graphList)
Functions.outputData(graphList, outputPath)

if doClusteringAnalysis == True:
	outputHist(graphList, outputPath)