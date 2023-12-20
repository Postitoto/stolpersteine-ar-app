using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class StoneSelection : MonoBehaviour, IPointerClickHandler
{
    public int Id { get; set; }

    private StoneSelectionList list;
    private string name;

    public TextMeshProUGUI nameField;
    
    private void Awake()
    {
        list = GetComponentInParent<StoneSelectionList>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        list.StoneSelected(Id);
    }

    public void SetName(string value)
    {
        name = value;
        nameField.text = value;
    }
}
