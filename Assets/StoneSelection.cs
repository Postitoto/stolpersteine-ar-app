using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class StoneSelection : MonoBehaviour, IPointerClickHandler
{
    public int Id { get; set; }

    private string name;

    public TextMeshProUGUI nameField;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }

    public void SetName(string value)
    {
        name = value;
        nameField.text = value;
    }
}
