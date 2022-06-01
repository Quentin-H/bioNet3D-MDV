using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using UnityEngine;
using UnityEngine.UI;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using TMPro;



public class NodeFiltering : MonoBehaviour
{
    [SerializeField] private NetworkSceneManager networkSceneManager;
    [SerializeField] private NetworkCamera networkCamera;
    [SerializeField] private ECSBillboardManager ecsBillboardManager;
    [SerializeField] private ErrorMessenger errorMessenger;
    private EntityManager entityManager;
    //private ReadOnlyCollection<Entity> allEntities = new ReadOnlyCollection<Entity>();
    private List<Entity> allEntities = new List<Entity>();

    public Dropdown clusterOptionsField;
    public InputField minRankField;
    public int minRank = 0;
    public InputField maxRankField;
    public int maxRank = 0;
    public InputField minBaselineField;
    public double minBaselineScore = 0;
    public InputField maxBaselineField;
    public double maxBaselineScore = 0;
    public InputField minDegreeField;
    public int minDegree = 0;
    public InputField maxDegreeField;
    public int maxDegree = 0;
    public TMP_InputField keywordsField;
    public List<string> keywords = new List<string>();
    public Dropdown showingOptionsField;


    private async void Start() 
    { 
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager; 
        allEntities = networkSceneManager.GetSimpleNodeEntityList();

        int numberOfClusters = 0;
        foreach (Entity entity in allEntities)
        {
            if (entityManager.GetComponentData<NodeData>(entity).cluster > numberOfClusters)
            {
                numberOfClusters = entityManager.GetComponentData<NodeData>(entity).cluster;
            }
        }

        List<string> dropdownOptions = new List<string>{ "All Clusters" };

        for (int i = 0; i < numberOfClusters; i++)
        {
            dropdownOptions.Add("Cluster " + (i + 1));
        }

        clusterOptionsField.AddOptions(dropdownOptions);
    }

    public void UpdateKeywords(string rawKeywords)
    {
        keywords.Clear();
        string[] keywordArray = rawKeywords.Split(',');

        foreach (string keyword in keywordArray)
        {
            keywords.Add(keyword.Trim());
        } 
    }

    public void Reset()
    {
        clusterOptionsField.value = 0;

        minRankField.text = "";
        minRank = 0;

        maxRankField.text = "";
        maxRank = 1;

        minBaselineField.text = "";
        minBaselineScore = 0;

        maxBaselineField.text = "";
        maxBaselineScore = 1;

        minDegreeField.text = "";
        minDegree = 0;

        maxDegreeField.text = "";
        maxDegree = 1;

        keywordsField.text = "";

        showingOptionsField.value = 2;

        ecsBillboardManager.ClearFilteredHighlights();

        ShowAllNodes();
    }

    public void Apply()
    {
        ShowAllNodes();

        Int32.TryParse(minRankField.text, out minRank);
        Int32.TryParse(maxRankField.text, out maxRank);
        
        Double.TryParse(minBaselineField.text, out minBaselineScore);
        Double.TryParse(maxBaselineField.text, out maxBaselineScore);

        Int32.TryParse(minDegreeField.text, out minDegree);
        Int32.TryParse(maxDegreeField.text, out maxDegree);

        if (showingOptionsField.value == 0) // hide
        {
            HideNodes( Filter() );
        }

        if (showingOptionsField.value == 1) // highlight
        {
            List<Entity> filtered = Filter();
            List<Entity> nonFiltered = new List<Entity>();

            foreach (Entity curEntity in allEntities)
            {
                if (!filtered.Contains(curEntity))
                {
                    nonFiltered.Add(curEntity);
                }
            }
            HighlightNodes( nonFiltered );
        }

        if (showingOptionsField.value == 2) // Show all
        {
            ShowAllNodes();
        }
    }

    private List<Entity> Filter()
    {
        List<Entity> toHide = new List<Entity>();

        foreach (Entity entity in allEntities)
        {
            //if cluster
            if (clusterOptionsField.value != 0)
            {
                if (!(entityManager.GetComponentData<NodeData>(entity).cluster == clusterOptionsField.value))
                {
                    toHide.Add(entity);
                    continue;
                }
            }
            
            if (minRankField.text.Trim() != "" && minRankField.text.Trim() != "")
            {
                if (!(entityManager.GetComponentData<NodeData>(entity).networkRank >= minRank && entityManager.GetComponentData<NodeData>(entity).networkRank <= maxRank))
                {
                    toHide.Add(entity);
                    continue;
                }
            }

            if (minBaselineField.text.Trim() != "" && minBaselineField.text.Trim() != "")
            {
                if (!(entityManager.GetComponentData<NodeData>(entity).baselineScore >= minBaselineScore && entityManager.GetComponentData<NodeData>(entity).baselineScore <= maxBaselineScore))
                {
                    toHide.Add(entity);
                    continue;
                }
            }

            if (minDegreeField.text.Trim() != "" && minDegreeField.text.Trim() != "")
            {
                if (!(entityManager.GetComponentData<NodeData>(entity).degree >= minDegree && entityManager.GetComponentData<NodeData>(entity).degree <= maxDegree))
                {
                    toHide.Add(entity);
                    continue;
                }
            }

            if (keywordsField.text.Trim() != "")
            {
                bool containsKeyword = false;
                string nodeDescription = entityManager.GetComponentData<NodeData>(entity).description.ToString();
                foreach(string keyword in keywords)
                {
                    if (nodeDescription.Contains(keyword))
                    {
                        containsKeyword = true;
                    }
                }

                if (containsKeyword == false)
                {
                    toHide.Add(entity);
                    continue;
                }
            }
        }
        return toHide;
    }

    private void ShowAllNodes()
    {
        foreach (Entity entity in allEntities)
        {
            try { entityManager.RemoveComponent<Disabled>(entity); } catch { }
        }
    }

    private void HideNodes(List<Entity> entitiesToHide)
    {
        foreach (Entity entity in entitiesToHide)
        {
            entityManager.AddComponentData( entity, new Disabled() );
        }

        if (entitiesToHide.Count == allEntities.Count)
        {
            errorMessenger.DisplayWarning("All nodes filtered", "The filter settings you have set filter out all nodes in the network.");
        } 
        else if (entitiesToHide.Count == 0) 
        {
            errorMessenger.DisplayWarning("No nodes filtered", "All nodes in the network fit the parameters of the filter settings. All nodes remain shown.");
        }
    }

    private void HighlightNodes(List<Entity> entitiesToHighlight) // need to make remove highlight method
    {
        ecsBillboardManager.SetFilteredHighlights(entitiesToHighlight);
    }
}