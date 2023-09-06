using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapOverlays : MonoBehaviour
{
    public ButtonListBehaviour buttonList;
    public StoneListBehaviour stoneList;
    public GameObject listContent;
    
    private void Awake()
    {
        StoneListBehaviour.OnClose += CloseStoneList;
    }

    private void OnDestroy()
    {
        StoneListBehaviour.OnClose -= CloseStoneList;
    }

    public void OpenStoneList()
    {
        CloseButtonList();
    }
    
    private void CloseStoneList()
    {
        //var resetPos = listContent.transform.localPosition;
        //resetPos = new Vector3(resetPos.x, 0, resetPos.z);
        listContent.transform.localPosition = Vector3.zero;
        stoneList.gameObject.SetActive(false);
        buttonList.gameObject.SetActive(true);
        buttonList.Show();
    }

    private void CloseButtonList()
    {
        buttonList.Hide();
        stoneList.gameObject.SetActive(true);
    }
}
