using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
//[ExecuteInEditMode]
public class CustomMatOverrider : MonoBehaviour
{
    /*public void Awake() 
    {
        var renderer = GetComponent<Renderer>();
        renderer.SetPropertyBlock(null);
        var propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetColor("_Color", Color.black);
        renderer.SetPropertyBlock(propertyBlock);
    }*/

    public void setOverrideColor(Color color) 
    {
        var renderer = GetComponent<Renderer>();
        renderer.SetPropertyBlock(null);
        var propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetColor("_Color", color);
        renderer.SetPropertyBlock(propertyBlock);
    }
}
