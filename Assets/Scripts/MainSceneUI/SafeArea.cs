using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeArea : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    private RectTransform panelSafeArea;

    private Rect currentSafeArea;
    private ScreenOrientation currentOrientation = ScreenOrientation.AutoRotation;
    
    // Start is called before the first frame update
    void Start()
    {
        panelSafeArea = GetComponent<RectTransform>();
        
        // Store current values
        currentOrientation = Screen.orientation;
        currentSafeArea = Screen.safeArea;

        ApplySafeArea();
    }

    void ApplySafeArea()
    {
        if (panelSafeArea == null)
            return;

        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position - safeArea.size;

        anchorMin.x /= canvas.pixelRect.width;
        anchorMin.y /= canvas.pixelRect.height;
        
        anchorMax.x /= canvas.pixelRect.width;
        anchorMax.y /= canvas.pixelRect.height;

        panelSafeArea.anchorMin = anchorMin;
        panelSafeArea.anchorMax = anchorMax;

        currentOrientation = Screen.orientation;
        currentSafeArea = Screen.safeArea;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentOrientation != Screen.orientation || currentSafeArea != Screen.safeArea)
        {
            ApplySafeArea();
        }
            
    }
}
