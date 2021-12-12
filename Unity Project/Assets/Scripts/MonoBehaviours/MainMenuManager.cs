using System.Collections;
using UnityEngine;
using System.IO;
using SimpleFileBrowser;
using System;
using UnityEngine.SceneManagement;  

public class MainMenuManager : MonoBehaviour
{
	public DataHolder dataHolder;
	private bool nodeLayoutFileSet;

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
				dataHolder.rawNodeLayoutFile = File.ReadAllText(path);
				nodeLayoutFileSet = true;
			}
        }
	}

	public void ChangePositionMultiplier(string multiplier)
	{
		dataHolder.positionMultiplier = float.Parse(multiplier);
	}

	public void OpenNetworkScene()
	{
		if (nodeLayoutFileSet) 
		{
			SceneManager.LoadScene("NetworkScene");  
		} else { Debug.Log("Select a valid file first!"); } 
	}

	public void ExitApplication()
	{
		Application.Quit();  
	}
}