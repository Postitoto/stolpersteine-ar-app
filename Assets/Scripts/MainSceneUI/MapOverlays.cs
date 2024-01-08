using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapOverlays : MonoBehaviour
{
    public ButtonListBehaviour buttonList;
    public StoneListBehaviour stoneList;
    public TourListBehaviour tourList;
    public GameObject listContent;
    
    private void Awake()
    {
        StoneListBehaviour.OnClose += CloseStoneList;
        TourListBehaviour.OnClose += CloseStoneList;
    }

    private void OnDestroy()
    {
        StoneListBehaviour.OnClose -= CloseStoneList;
        TourListBehaviour.OnClose -= CloseStoneList;
    }

    public void OpenStoneList()
    {
        CloseButtonList(stoneList.gameObject);
    }

    public void OpenTourList()
    {
        CloseButtonList(tourList.gameObject);
    }
    
    private void CloseStoneList()
    {
        listContent.transform.localPosition = Vector3.zero;
        stoneList.gameObject.SetActive(false);
        tourList.gameObject.SetActive(false);
        buttonList.gameObject.SetActive(true);
        buttonList.Show();
    }

    private void CloseButtonList(GameObject list)
    {
        buttonList.Hide();
        list.SetActive(true);
    }
}
