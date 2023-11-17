using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneSelectionList : MonoBehaviour
{
    [SerializeField] private GameObject listItemPrefab;


    private RectTransform rect;

    private void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    public void AddStone(string name)
    {
        var item = Instantiate(listItemPrefab, transform, false);
    }
    
    public void Show()
    {
        var pos = rect.localPosition;
        pos.x = 30;
        rect.anchoredPosition3D = pos;
    }

    public void Hide()
    {
        
    }
}
