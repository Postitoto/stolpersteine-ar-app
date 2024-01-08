using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ChangeIconHandler : MonoBehaviour
{
    public Sprite startImage;
    public Sprite altImage;

    private Image target;

    private void Start()
    {
        target = GetComponent<Image>();
    }

    private void OnEnable()
    {
        MiniMapBehaviour.MapActiveChanged += ChangeIcon;
    }

    private void OnDisable()
    {
        MiniMapBehaviour.MapActiveChanged -= ChangeIcon;
    }
    
    private void ChangeIcon(bool active)
    {
        target.sprite = active ? altImage : startImage;
    }
}
