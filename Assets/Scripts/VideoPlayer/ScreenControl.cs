using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScreenControl : MonoBehaviour, IPointerClickHandler
{
    public delegate void ScreenClicked();

    public event ScreenClicked OnScreenClicked;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        OnScreenClicked?.Invoke();
    }
}
