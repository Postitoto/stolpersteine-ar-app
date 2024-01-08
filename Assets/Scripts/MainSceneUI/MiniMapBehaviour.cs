using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MiniMapBehaviour : MonoBehaviour
{
    public delegate void NotifyMapActive(bool active);
    public static event NotifyMapActive MapActiveChanged;
    
    [SerializeField] private GameObject map;
    [SerializeField] private GameObject minimap;

    [Range(0.1f, 1.0f)] public float animationTime = 0.1f;
    [Range(1, 100)] public int animationFrameCount = 10;
    
    private RectTransform miniMapRect;
    private Vector2 minimapPosition;

    private float deltaTime;
    private float dxWidth;
    private float dxHeight;
    private float posXShift;
    private float posYShift;
    
    private float shrunkenWidth;
    private float shrunkenHeight;
    private float expandedWidth;
    private float expandedHeight;
    private Vector2 startPosition;
    private Vector2 endPosition;

    private void Start()
    {
        Canvas.ForceUpdateCanvases();

        miniMapRect = minimap.GetComponent<RectTransform>();
        var rect = gameObject.GetComponent<RectTransform>();
        minimapPosition = miniMapRect.anchoredPosition;
        shrunkenWidth = miniMapRect.rect.width;
        shrunkenHeight = miniMapRect.rect.height;
        expandedHeight = rect.rect.height * 0.95f;
        expandedWidth = rect.rect.width * 0.95f;
        startPosition = miniMapRect.anchoredPosition;
        endPosition = Vector2.zero;

        SetDeltaValues();
    }
    
    public void MapInteraction()
    {
        SetDeltaValues();
        StartCoroutine(map.activeSelf ? Shrinking() : Expanding());
    }
    
    private IEnumerator Expanding()
    {
        while (miniMapRect.rect.width < expandedWidth * 0.9 && miniMapRect.rect.height < expandedHeight * 0.9)
        {
            var sizeDelta = miniMapRect.sizeDelta;
            sizeDelta = new Vector2(sizeDelta.x + dxWidth, sizeDelta.y + dxHeight);
            miniMapRect.sizeDelta = sizeDelta;

            var newPos = miniMapRect.anchoredPosition;
            newPos = new Vector2(newPos.x - posXShift, newPos.y - posYShift);
            miniMapRect.anchoredPosition = newPos;
            
            yield return new WaitForSeconds(deltaTime);
        }
        
        miniMapRect.anchoredPosition = new Vector2(0, 0);
        
        minimap.gameObject.SetActive(false);
        map.SetActive(true);
        MapActiveChanged?.Invoke(true);
    }

    private IEnumerator Shrinking()
    {
        map.SetActive(false);
        MapActiveChanged?.Invoke(false);
        minimap.gameObject.SetActive(true);

        while (miniMapRect.rect.width > shrunkenWidth * 1.1 && miniMapRect.rect.height > shrunkenHeight * 1.1)
        {
            // Size
            var sizeDelta = miniMapRect.sizeDelta;
            sizeDelta = new Vector2(sizeDelta.x - dxWidth, sizeDelta.y - dxHeight);
            miniMapRect.sizeDelta = sizeDelta;
            
            // Position
            var newPos = miniMapRect.anchoredPosition;
            newPos = new Vector2(newPos.x + posXShift, newPos.y + posYShift);
            miniMapRect.anchoredPosition = newPos;
            
            yield return new WaitForSeconds(deltaTime);
        }
        
        miniMapRect.anchoredPosition = new Vector2(minimapPosition.x, minimapPosition.y);
        miniMapRect.sizeDelta = new Vector2(shrunkenWidth, shrunkenHeight);
    }

    private void SetDeltaValues()
    {
        deltaTime = animationTime / animationFrameCount;
        dxWidth = (expandedWidth - shrunkenWidth) / animationFrameCount;
        dxHeight = (expandedHeight - shrunkenHeight) / animationFrameCount;
        posXShift = (startPosition.x - endPosition.x) / animationFrameCount;
        posYShift = (startPosition.y - endPosition.y) / animationFrameCount;
    }
}
