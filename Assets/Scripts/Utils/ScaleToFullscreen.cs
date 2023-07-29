using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleToFullscreen : MonoBehaviour
{
    private RectTransform _rectTransform;
    private float _prevScreenWidth;
    private Rect _objectRectangle;
    void OnEnable()
    {
       _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            throw new MissingComponentException("Invalid GameObject! Object has no RectTransform");
        }
        _prevScreenWidth = Screen.width;
        SetRectTransformFullscreen();
    }

    // Update is called once per frame
    void Update()
    {
        if (_prevScreenWidth != Screen.width)
        {
            SetRectTransformFullscreen();
        }
    }

    private void SetRectTransformFullscreen()
    {
        _objectRectangle = _rectTransform.rect;
        _rectTransform.localScale = new Vector3(Screen.width / _objectRectangle.width, Screen.height / _objectRectangle.height);
    }
}
