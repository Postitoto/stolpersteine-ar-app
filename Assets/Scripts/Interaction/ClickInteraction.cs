using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// Specify events to be executed when GameObject with this script attached is clicked

public class ClickInteraction : MonoBehaviour, IPointerClickHandler
{ 
    [SerializeField]
    private UnityEvent onClick;
    public void OnPointerClick(PointerEventData eventData)
    {
        onClick.Invoke();
    }
}

