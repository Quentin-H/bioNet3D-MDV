using UnityEngine;
using UnityEngine.SceneManagement;

public class DataHolder : MonoBehaviour
{
    public string rawNodeLayoutFile = "";
    public float positionMultiplier = 100;
    private void Start() { DontDestroyOnLoad(this.gameObject); }
}