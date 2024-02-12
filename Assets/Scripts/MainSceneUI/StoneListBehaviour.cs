using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoneListBehaviour : MonoBehaviour
{
    public delegate void NotifyClose();
    public static event NotifyClose OnClose;
    public delegate void NotifyOpen();
    public static event NotifyOpen OnOpen;

    public RectTransform listBackgroundRect;
    public GameObject playerObject;
    public List<StoneListEntryBehaviour> entries;
    
    private Vector2 touchStartPosition;
    private bool isTouching;

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            CheckTouchLocation(Input.GetTouch(0));
        }
    }

    private void CheckTouchLocation(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                // Saves the position where the touch was started
                RectTransformUtility.ScreenPointToLocalPointInRectangle(listBackgroundRect, touch.position, null,
                    out touchStartPosition);
                isTouching = true;
                break;
            case TouchPhase.Ended:
                if (!isTouching) return;
                if (!listBackgroundRect.rect.Contains(touchStartPosition))
                    Close();
                isTouching = false;
                break;
            case TouchPhase.Canceled:
                isTouching = false;
                break;
        }
    }

    private void OnEnable()
    {
        OnOpen?.Invoke();
    }

    public void Close()
    {
        OnClose?.Invoke();
    }
}
