using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using System.Collections.ObjectModel;

public class NodeFiltering : MonoBehaviour
{
    [SerializeField] private NetworkSceneManager networkSceneManager;
    private EntityManager entityManager;
    private ReadOnlyCollection<Entity> allEntities;
    private List<Entity> hiddenEntities;

    public Dropdown clusterField;
    public InputField minRankField;
    public int minRank;
    public InputField maxRankField;
    public int maxRank;
    public InputField minBaselineField;
    public double minBaselineScore;
    public InputField maxBaselineField;
    public double maxBaselineScore;
    public InputField minDegreeField;
    public int minDegree;
    public InputField maxDegreeField;
    public int maxDegree;
    public InputField keywordsField;
    public string keywords;
    public Dropdown showingOptionsField;


    private void Start() { entityManager = World.DefaultGameObjectInjectionWorld.EntityManager; }

    public void Reset()
    {
        // show all nodes again
        // set fields to defaults
        // set drop downs to defaults and the showing one to show all
    }

    public void Apply()
    {
        
    }
}
