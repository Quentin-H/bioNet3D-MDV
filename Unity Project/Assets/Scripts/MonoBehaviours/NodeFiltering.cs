using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using System.Collections.ObjectModel;
using TMPro;

public class NodeFiltering : MonoBehaviour
{
    [SerializeField] private NetworkSceneManager networkSceneManager;
    private EntityManager entityManager;
    //private ReadOnlyCollection<Entity> allEntities = new ReadOnlyCollection<Entity>();
    private List<Entity> allEntities = new List<Entity>();

    public Dropdown clusterOptionsField;
    public int cluster;
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


    private void Start() 
    { 
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager; 
        allEntities = networkSceneManager.GetSimpleNodeEntityList();
    }

    public void UpdateKeywords(string rawKeywords)
    {
        keywords.Clear();
        string[] keywordArray = rawKeywords.Split(',');

        foreach(string keyword in keywordArray)
        {
            keywords.Add(keyword.Trim());
        } 
    }

    public void Reset()
    {
        clusterOptionsField.value = 0;

        minRankField.text = Convert.ToString(0);
        minRank = 0;

        maxRankField.text = Convert.ToString(1);
        maxRank = 1;

        minBaselineField.text = Convert.ToString(0);
        minBaselineScore = 0;

        maxBaselineField.text = Convert.ToString(1);
        maxBaselineScore = 1;

        minDegreeField.text = Convert.ToString(0);
        minDegree = 0;

        maxDegreeField.text = Convert.ToString(1);
        maxDegree = 1;

        keywordsField.text = "";

        showingOptionsField.value = 2;

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

        keywordsField.text = "";

        if (showingOptionsField.value == 0) // hide
        {
            HideNodes( Filter() );
        }

        if (showingOptionsField.value == 1) // highlight
        {

        }

        if (showingOptionsField.value == 2) // Show all
        {
            ShowAllNodes();
        }
    }

    private List<Entity> Filter()
    {
        List<Entity> toHide = new List<Entity>();

        foreach(Entity entity in allEntities)
        {
            //if cluster
            if (clusterOptionsField.value != 0)
            {
                if (!(entityManager.GetComponentData<NodeData>(entity).cluster == clusterOptionsField.value + 1))
                {
                    toHide.Add(entity);
                    continue;
                }
            }
            
            if (!(entityManager.GetComponentData<NodeData>(entity).networkRank >= minRank && entityManager.GetComponentData<NodeData>(entity).networkRank <= maxRank))
            {
                Debug.Log("on hide list");
                toHide.Add(entity);
                continue;
            }

            if (!(entityManager.GetComponentData<NodeData>(entity).baselineScore >= minBaselineScore && entityManager.GetComponentData<NodeData>(entity).baselineScore <= maxBaselineScore))
            {
                toHide.Add(entity);
                continue;
            }

            if (!(entityManager.GetComponentData<NodeData>(entity).degree >= minDegree && entityManager.GetComponentData<NodeData>(entity).degree <= maxDegree))
            {
                toHide.Add(entity);
                continue;
            }

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
        
        return toHide;
    }

    private void ShowAllNodes()
    {
        foreach(Entity entity in allEntities)
        {
            try 
            {
                entityManager.RemoveComponent<Disabled>(entity);
            } 
            catch { } //maybe make this catch specifically for error when an netity doesn't have disabled
        }
    }

    private void HideNodes(List<Entity> entitiesToHide)
    {
        foreach(Entity entity in entitiesToHide)
        {
            entityManager.AddComponentData( entity, new Disabled() );
        }
    }

    private void HighlightNodes(List<Entity> entitiesToHide) // need to make remove highlight method
    {
        foreach(Entity entity in entitiesToHide)
        {
            //entityManager.AddComponentData( entity,  );
        }
    }
}