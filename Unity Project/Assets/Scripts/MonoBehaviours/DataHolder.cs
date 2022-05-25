using UnityEngine;
using UnityEngine.SceneManagement;

public class DataHolder : MonoBehaviour
{
    public string nodeLayoutFile = "";
    public string nodeInfoFile = "";
    public string nodeRankingFile = "";

    private void Start() { DontDestroyOnLoad(this.gameObject); }
}