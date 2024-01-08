using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuImageBehaviour : MonoBehaviour
{
    [Range(0, 100)]
    public float marginPercentage;
    
    private RectTransform imageRect;
    private float dimension;
    
    // Start is called before the first frame update
    void Awake()
    {
        imageRect = GetComponent<RectTransform>();
        AdjustSize();
    }
    
    void Update()
    {
        AdjustSize();
    }

    private void AdjustSize()
    {
        var screenHeight = Screen.height;
        var screenWidth = Screen.width;
        
        if (screenHeight > screenWidth)
        {
            dimension = screenWidth - (screenWidth * (2 * marginPercentage / 100));
        }
        else
        {
            dimension = screenHeight - (screenHeight * (2 * marginPercentage / 100));
        }
        
        Vector2 vector = new Vector2(dimension, dimension);
        imageRect.sizeDelta = vector;
    }

    public float GetDimension()
    {
        return dimension;
    }
}
