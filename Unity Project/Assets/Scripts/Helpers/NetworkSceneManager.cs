using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkSceneManager : MonoBehaviour
{
    private GameObject dataHolder;

    private void Awake() 
    {
        dataHolder = GameObject.Find("DataHolder");
    }
}