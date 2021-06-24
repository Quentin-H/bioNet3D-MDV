using UnityEngine;
using UnityEngine.SceneManagement;

public class DataHolder : MonoBehaviour
{
    public string rawNodeLayoutFile = "";
    public string rawEdgeFile = "";
    private void Awake() { DontDestroyOnLoad(this.gameObject); }
}