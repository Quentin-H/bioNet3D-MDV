using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class FilterManager : MonoBehaviour
{
    private EntityManager entityManager => World.DefaultGameObjectInjectionWorld.EntityManager;
    [SerializeField] private NetworkSceneManager networkManager;
    private List<Entity> allEntities = new List<Entity>();
    private List<Entity> filteredEntities = new List<Entity>();
    private List<Entity> filteredOutEntities = new List<Entity>(); // entities to NOT show 

    // have public variables for all filtering stuff and have UI set these

    private void Start()
    {
        allEntities = networkManager.GetSimpleNodeEntityList();
    }

    public void Apply()
    {
        //for loop that checks every element in all entities and if any values dont match put it in filtered out

        // if hide is enabled, if its the others create other things
        foreach(Entity entity in filteredOutEntities)
        {
            EntityManager.SetEnabled(entity, false);
        }
    }

    public void Reset()
    {
        foreach(Entity entity in allEntities)
        {
            EntityManager.SetEnabled(entity, true);
        }
    }
}
