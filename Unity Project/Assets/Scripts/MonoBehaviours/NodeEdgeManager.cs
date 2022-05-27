using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

public class NodeEdgeManager : MonoBehaviour
{
    private EntityManager entityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

    public NetworkCamera networkCamera;
    public NetworkSceneManager networkSceneManager;

    public Button showHideNodeEdgesButton;
    public Button showHideClusterEdgesButton;

    private bool nodeEdgesShowing = false;
    private bool clusterEdgesShowing = false;

    private List<GameObject> activeLines = new List<GameObject>();

    public void ShowHideNodeEdges()
    {
        if (nodeEdgesShowing == true)
        {
            HideNodeEdges();
        } 
        else if (nodeEdgesShowing == false)
        {
            if (clusterEdgesShowing == true)
            {
                HideClusterEdges();
            }

            ShowNodeEdges(networkCamera.GetSelectedEntity());

            showHideNodeEdgesButton.GetComponentInChildren<Text>().text = "Hide Node Edges";
            nodeEdgesShowing = true;
        }
    }

    public void ShowHideClusterEdges()
    {
        if (clusterEdgesShowing == true)
        {
            HideClusterEdges();
        } 
        else if (clusterEdgesShowing == false)
        {
            if (nodeEdgesShowing)
            {
                HideNodeEdges();
            }

            Entity entity = networkCamera.GetSelectedEntity();
            int clusterNumber = entityManager.GetComponentData<NodeData>(entity).cluster;
            List<Entity> clusterEntities = networkSceneManager.GetEntitiesInCluster(clusterNumber);
            ShowClusterEdges(clusterEntities);

            showHideClusterEdgesButton.GetComponentInChildren<Text>().text = "Hide Cluster Edges";
            clusterEdgesShowing = true;
        }
    }

    private void ShowNodeEdges(Entity entity)
    {
            try 
            {
                float4 entityPosAs4 = entityManager.GetComponentData<LocalToWorld>(entity).Value[3];
                float3 entityPos = new float3(entityPosAs4.x, entityPosAs4.y, entityPosAs4.z);

                foreach(Entity connectedEntity in networkSceneManager.GetConnectedEntities(entity))
                {
                    float4 connectedEntityPosAs4 = entityManager.GetComponentData<LocalToWorld>(connectedEntity).Value[3];
                    float3 connectedEntityPos = new float3(connectedEntityPosAs4.x, connectedEntityPosAs4.y, connectedEntityPosAs4.z);

                    GameObject line = new GameObject();
                    activeLines.Add(line);
                    line.transform.position = entityPos;
                    line.AddComponent<LineRenderer>();
                    LineRenderer lr = line.GetComponent<LineRenderer>();
                    lr.material = new UnityEngine.Material(Shader.Find("HDRP/Unlit")); // add shader that supports transparency
                    lr.GetComponent<Renderer>().material.color = Color.yellow;
                    lr.startWidth = 0.1f;
                    lr.endWidth = 0.1f;
                    lr.SetPosition(0, entityPos);
                    lr.SetPosition(1, connectedEntityPos);
                }
            } catch (Exception e) { Debug.Log(e); } 
    }

    private void ShowClusterEdges(List<Entity> entities)
    {
        try 
        {
            foreach(Entity cur in entities)
            {
                ShowNodeEdges(cur);
            }
        }
        catch { }
    }

    private void HideNodeEdges()
    {
        foreach(GameObject cur in activeLines) 
        {
            Destroy(cur);
            //Destroy(cur.GetComponent<Renderer>().material); to prevent memory leak, causes error
        }
        Resources.UnloadUnusedAssets(); // i think this gets rid of materials, prevents memory leak
        showHideNodeEdgesButton.GetComponentInChildren<Text>().text = "Show Node Edges";
        nodeEdgesShowing = false;
    }

    private void HideClusterEdges()
    {
        foreach(GameObject cur in activeLines) 
        {
            Destroy(cur);
            //Destroy(cur.GetComponent<Renderer>().material); to prevent memory leak, causes error
        }
        Resources.UnloadUnusedAssets(); // i think this gets rid of materials, prevents memory leak
        showHideClusterEdgesButton.GetComponentInChildren<Text>().text = "Show Cluster Edges";
        clusterEdgesShowing = false;
    }
}