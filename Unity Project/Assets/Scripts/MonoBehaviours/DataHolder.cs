using UnityEngine;
using UnityEngine.SceneManagement;

public class DataHolder : MonoBehaviour
{
    public string rawNodeLayoutFile = "";
    public string rawEdgeFile = "";
    public float positionMultiplier = 8;
    private void Awake() { DontDestroyOnLoad(this.gameObject); }
}