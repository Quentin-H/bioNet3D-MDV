using System.Collections;
using UnityEngine;
using System.IO;
using SimpleFileBrowser;
using System;
using UnityEngine.SceneManagement;  

public class MainMenuManager : MonoBehaviour
{
	private DataHolder dataHolder;
	private bool nodeLayoutFileSet;
	private bool edgeFileSet;

    public void OpenLayoutFileDialog()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Massive Dataset Visualizer Layout Files", ".mdvl"));
        FileBrowser.SetDefaultFilter(".mdvl");
        StartCoroutine(ShowLoadDialogCoroutine(1));
    }

    public void OpenEdgeFileDialog()
    {
        FileBrowser.SetFilters( true, new FileBrowser.Filter("Edge Files", ".edge"));
        FileBrowser.SetDefaultFilter(".edge");
        StartCoroutine(ShowLoadDialogCoroutine(2));
    }

    IEnumerator ShowLoadDialogCoroutine(int argument)
	{
		Debug.Log(argument);

		//1 == layout
        if (argument == 1) 
        {
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Load Files and Folders", "Load");
		    Debug.Log(FileBrowser.Success);

			if (FileBrowser.Success)
			{
				for(int i = 0; i < FileBrowser.Result.Length; i++)
				{
					string path = FileBrowser.Result[i];					
					dataHolder.rawNodeLayoutFile = File.ReadAllText(path);
					Debug.Log(dataHolder.rawNodeLayoutFile);
					nodeLayoutFileSet = true;
				}
        	}
		} 

		//layout == 2
        if (argument == 2)
        {
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Load Files and Folders", "Load");
			Debug.Log(FileBrowser.Success);

			if( FileBrowser.Success )
			{
				for(int i = 0; i < FileBrowser.Result.Length; i++)
				{
					string path = FileBrowser.Result[i];
					dataHolder.rawEdgeFile = File.ReadAllText(path);
					Debug.Log(dataHolder.rawEdgeFile);
					edgeFileSet = true;
				}
			}
        } 
	}

	public void OpenNetworkScene()
	{
		if (nodeLayoutFileSet && edgeFileSet) 
		{
			SceneManager.LoadScene("NetworkScene");  
		}
		/*take out bottom line for final release so it only goes to the next scene 
		if both files are set, and make a pop up message appear if user attempts to start network scene without setting files*/
		SceneManager.LoadScene("NetworkScene");  
		throw new Exception("Not all files set");
	}

	public void ExitApplication()
	{
		Application.Quit();  
	}
}