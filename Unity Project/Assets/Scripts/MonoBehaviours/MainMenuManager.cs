using System.Collections;
using UnityEngine;
using System.IO;
using SimpleFileBrowser;
using System;
using UnityEngine.SceneManagement;  

public class MainMenuManager : MonoBehaviour
{
	public DataHolder dataHolder;
	public ErrorMessenger errorMessenger;
	private bool nodeLayoutFileSet = false;
	private bool nodeInformationFileSet = false;
	public bool nodeScoreFileSet = false;

	//debugging only --------------------------
	public TextAsset nodeLayoutFile;
	public TextAsset nodeInformationFile;
	public TextAsset nodeScoreFile;
	//-----------------------------------------

	private void Start() 
	{
		//This only works in edit mode! Another way to set a quicklink for standalone needs to be found
		if (Application.isEditor)
		{
			string projectPath = Application.dataPath;
			//Removes the "Assets" section and makes it go to sample files
			projectPath = projectPath.Replace("/Unity Project/Assets", "/Sample Files");
			FileBrowser.AddQuickLink( "Sample Files", projectPath, null );
		}
		//add a random layout of points to rotate around in the background

		// debugging only (for including hardcoded files)
		SetDebugDefaultFiles();
	}

	private void SetDebugDefaultFiles()
	{
		try { dataHolder.nodeLayoutFile = nodeLayoutFile.text; nodeLayoutFileSet = true; } 
		catch { errorMessenger.DisplayError("No network layout file selected!", "Please set a network layout file before continuing."); }

		try { dataHolder.nodeInfoFile = nodeInformationFile.text; nodeInformationFileSet = true; } 
		catch { errorMessenger.DisplayError("No node info file selected!", "Please set a node info file before continuing."); }

		// viz can run without ranking/score file
		try { dataHolder.nodeRankingFile = nodeScoreFile.text; nodeScoreFileSet = true; } 
		catch { nodeScoreFileSet = false; } 
	}

    public void OpenLayoutFileDialog()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Massive Dataset Visualizer Layout Files", ".mdvl"));
        FileBrowser.SetDefaultFilter(".mdvl");
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    IEnumerator ShowLoadDialogCoroutine()
	{
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Load Files and Folders", "Load");

		if (FileBrowser.Success)
		{
			for(int i = 0; i < FileBrowser.Result.Length; i++)
			{
				string path = FileBrowser.Result[i];					
				dataHolder.nodeLayoutFile = File.ReadAllText(path);
				nodeLayoutFileSet = true;
			}
        }
	}

	public void OpenNetworkScene()
	{
		if (nodeLayoutFileSet) 
		{
			SceneManager.LoadScene("NetworkScene");  
		} else { errorMessenger.DisplayError("No network file selected!", "Please select a network layout file before continuing."); } 
	}

	public void ExitApplication()
	{
		Application.Quit();  
	}
}