using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkSceneManager : MonoBehaviour
{
    private GameObject inputDataHolder;
    //private List<nodeItem> primeNumbers = new List<nodeItem>();

    private void Awake() 
    {
        inputDataHolder = GameObject.Find("InputDataHolder");
        ConvertRawInputNodesAndSpawn();
    }

    private void ConvertRawInputNodesAndSpawn()
    {
        string[] rawLayoutInputLines = inputDataHolder.GetComponent<DataHolder>().rawNodeLayoutFile.Split('\n');

        foreach(string line in rawLayoutInputLines)
        {
            string ID = line.Split(' ')[0];
            Vector3 coord;
            float value = float.Parse(line.Split(']')[1].Trim());
            coord.x = float.Parse(line.Split('[')[1].Split(',')[0].Trim());
            coord.y = float.Parse(line.Split(',')[1].Split(',')[0].Trim());
            coord.z = float.Parse(line.Split(',')[1].Split(']')[0].Trim());

            Debug.Log(ID);
            Debug.Log(coord);
            Debug.Log(value);
        }
    }
}