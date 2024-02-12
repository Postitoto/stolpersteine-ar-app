using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonListBehaviour : MonoBehaviour
{
    [Range(0.1f, 1.0f)] public float animationTime = 0.1f;
    [Range(1, 100)] public int animationFrameCount = 10;

    private RectTransform rect;
    private Vector2 activePosition;
    private Vector2 inactivePosition;

    private float deltaTime;
    private float posXShift;

    private void Start()
    {
        rect = GetComponent<RectTransform>();
        activePosition = rect.anchoredPosition;
        inactivePosition = new Vector2(activePosition.x * -1, activePosition.y);
    }

    public void Show()
    {
        StartCoroutine(MoveTowards(false));
    }

    public void Hide()
    {
        StartCoroutine(MoveTowards(true));
    }

    private IEnumerator MoveTowards(bool hide)
    {
        SetDeltaValues();
        
        for (int i = 0; i < animationFrameCount; i++)
        {
            // Position
            var newPos = rect.anchoredPosition;
            newPos = hide ? new Vector2(newPos.x - posXShift, newPos.y) : new Vector2(newPos.x + posXShift, newPos.y);
            rect.anchoredPosition = newPos;
            
            yield return new WaitForSeconds(deltaTime);   
        }

        rect.anchoredPosition = hide ? inactivePosition : activePosition;
        if(hide) gameObject.SetActive(false);
    }

    private void SetDeltaValues()
    {
        deltaTime = animationTime / animationFrameCount;
        posXShift = (activePosition.x - inactivePosition.x) / animationFrameCount;
    }
}
