using UnityEngine;
using UnityEngine.SceneManagement;

public class DataHolder : MonoBehaviour
{
    private void Awake() { DontDestroyOnLoad(this.gameObject); }
    public string rawNodeLayoutFile = "";
    public string rawEdgeFile = "";
}