using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class StoneSelectionList : MonoBehaviour
{
    [SerializeField] private StolpersteinLocationHandler locationHandler;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private GameObject expandButton;
    
    
    private RectTransform rect;
    private Vector3 hiddenPosition;
    private Vector3 showPosition;
    private ARRaycastHit raycastHit;

    private int selectedId;
    private bool isSelected = false;
    private bool isHidden = true;
    
    private void Start()
    {
        rect = GetComponent<RectTransform>();
        hiddenPosition = rect.anchoredPosition3D;

        showPosition = rect.localPosition;
        showPosition.x = 30;
    }

    private void Update()
    {
        HandleTouchInput();
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0)
        {
            return;
        }

        var touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Ended)
        {
            return;
        }
        
        if (isSelected)
        {
            var locationOnScreen = touch.position;
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(locationOnScreen, hits))
            {
                locationHandler.ManuallyCreateScene(selectedId, hits.First());
                isSelected = false;
            }
        }
        else if (!isHidden)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, touch.position, null, out var touchStartPosition);
            if (!rect.rect.Contains(touchStartPosition))
            {
                Hide();
            }
        }
    }
    
    public void StoneSelected(int id)
    {
        Hide();
        selectedId = id;
        isSelected = true;
    }
    
    public void Show()
    {
        rect.anchoredPosition3D = showPosition;
        isHidden = false;
    }

    public void Hide()
    {
        rect.anchoredPosition3D = hiddenPosition;
        expandButton.SetActive(true);
        isHidden = true;
    }
}
