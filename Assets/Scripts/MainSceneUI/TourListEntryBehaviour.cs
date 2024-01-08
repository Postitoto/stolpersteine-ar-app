using System;
using System.Collections;
using System.Collections.Generic;
using Scriptables;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class TourListEntryBehaviour : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI amtText;

    private Tour tour;
    private TourListBehaviour list;
    
    
    private void Start()
    {
        list = FindObjectOfType<TourListBehaviour>();
    }
    
    public void Init(Tour tour)
    {
        this.tour = tour;
        nameText.text = tour.name;
        amtText.text = tour.locationsInOrder.Count.ToString();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        list.LoadDescription(tour);
    }
}
