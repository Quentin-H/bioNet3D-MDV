using UnityEngine.SceneManagement;
using UnityEngine;

public class SwitchToScene : MonoBehaviour
{
    public int SceneToSwitchTo = 0;

    public void SwitchScenes()
    {
        SceneManager.LoadScene(SceneToSwitchTo, LoadSceneMode.Single);
    }
}