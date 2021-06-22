using System.Collections;
using UnityEngine;
using System.IO;
using SimpleFileBrowser;
using System;
using UnityEngine.SceneManagement;  

public class MainMenuManager : MonoBehaviour
{
    public void OpenLayoutFileDialog()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Massive Dataset Visualizer Layout Files", ".mdvl"));
        FileBrowser.SetDefaultFilter( ".mdvl" );
        StartCoroutine(ShowLoadDialogCoroutine("layout"));
    }

    public void OpenEdgeFileDialog()
    {
        FileBrowser.SetFilters( true, new FileBrowser.Filter("Edge Files", ".edge"));
        FileBrowser.SetDefaultFilter( ".edge" );
        StartCoroutine(ShowLoadDialogCoroutine("edge"));
    }

    IEnumerator ShowLoadDialogCoroutine(string argument)
	{
        if (argument == "layout") 
        {
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Load Files and Folders", "Load");

			// Dialog is closed
			// Print whether the user has selected some files/folders or cancelled the operation (FileBrowser.Success)
		    Debug.Log(FileBrowser.Success);

			if( FileBrowser.Success )
			{
				// Print paths of the selected files (FileBrowser.Result) (null, if FileBrowser.Success is false)
				for(int i = 0; i < FileBrowser.Result.Length; i++)
					Debug.Log(FileBrowser.Result[i]);

				// Read the bytes of the first file via FileBrowserHelpers
				// Contrary to File.ReadAllBytes, this function works on Android 10+, as well
				byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(FileBrowser.Result[0]);

				// Or, copy the first file to persistentDataPath
				string destinationPath = Path.Combine(Application.persistentDataPath, FileBrowserHelpers.GetFilename(FileBrowser.Result[0]));
				FileBrowserHelpers.CopyFile(FileBrowser.Result[0], destinationPath);
        	}
		} else { throw new Exception("argument was not edge or layout"); }

        if (argument == "edge")
        {
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Load Files and Folders", "Load");

			// Dialog is closed
			// Print whether the user has selected some files/folders or cancelled the operation (FileBrowser.Success)
			Debug.Log(FileBrowser.Success);

			if( FileBrowser.Success )
			{
				// Print paths of the selected files (FileBrowser.Result) (null, if FileBrowser.Success is false)
				for(int i = 0; i < FileBrowser.Result.Length; i++)
					Debug.Log(FileBrowser.Result[i]);

				// Read the bytes of the first file via FileBrowserHelpers
				// Contrary to File.ReadAllBytes, this function works on Android 10+, as well
				byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(FileBrowser.Result[0]);

				// Or, copy the first file to persistentDataPath
				string destinationPath = Path.Combine(Application.persistentDataPath, FileBrowserHelpers.GetFilename(FileBrowser.Result[0]));
				FileBrowserHelpers.CopyFile(FileBrowser.Result[0], destinationPath);
			}
        } else { throw new Exception("argument was not edge or layout"); }
        throw new Exception("argument was not edge or layout");
		// Show a load file dialog and wait for a response from user
		// Load file/folder: both, Allow multiple selection: true
		// Initial path: default (Documents), Initial filename: empty
		// Title: "Load File", Submit button text: "Load"
	}

	public void OpenNetworkScene()
	{
		SceneManager.LoadScene("NetworkScene");  
	}

	public void ExitApplication()
	{
		Application.Quit();  
	}
}