using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using TMPro;
using UnityEngine;

public class ARLocationBehaviour : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI addressField;
    [SerializeField] private TextMeshProUGUI distanceField;

    private Canvas canvas;

    private void Start()
    {
        canvas = GetComponentInChildren<Canvas>();
        canvas.worldCamera = Camera.main;
    }

    public void SetAddress(string address)
    {
        addressField.text = address;
    }
    
    public void SetDistance(double distance)
    {
        if (distance < 0)
            return;
        
        if (distance < 1000)
        {
            distanceField.text = $"{(int) distance} meters";
        }
        else
        {
            var rounded = Math.Round(distance / 1000, 2);
            distanceField.text = $"{rounded} kilometers";
        }
    }
    
    public void SetScale(float scale)
    {
        SetScale(Vector3.one * scale); 
    }
    
    public void SetScale(Vector3 scale)
    {
        transform.localScale = scale;
    }

    public void LookAt(Vector3 target)
    {
        transform.LookAt(target);
    }
}
