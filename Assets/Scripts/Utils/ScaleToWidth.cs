using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleToWidth : MonoBehaviour
{
    [SerializeField]
    private GameObject _gameObjectToScale;
    [SerializeField]
    private float _relativeScale;

    private float _scaleFactorToWidth;
    private float _objectWidth, _objectHeight;
    private float _oldScreenReferenceSize = 0, _screenReferenceSize = 0;
    private RectTransform _rectTransformOfObject;

    void OnEnable()
    {
        _gameObjectToScale.transform.localScale = Vector3.one;
        _rectTransformOfObject = (RectTransform)_gameObjectToScale.transform;
        _objectWidth = _rectTransformOfObject.rect.width;
        _objectHeight = _rectTransformOfObject.rect.height;
        SetScreenReferenceSize();
        Debug.Log("Width " + _objectWidth + " | Height " + _objectHeight + " | Screen Width, Height " + Screen.width + " " + Screen.height);
        CalculateScaleFactor();
        ScaleToScreenWidth();
    }

    // Update is called once per frame
    void Update()
    { 
        if (Screen.width != _screenReferenceSize)
        {
            Debug.Log("Screen Width, Height " + Screen.width + " " + Screen.height);
            SetScreenReferenceSize();
            CalculateScaleFactor();
            ScaleToScreenWidth();
        }
        
    }

    private void ScaleToScreenWidth()
    {
        _gameObjectToScale.transform.localScale = new Vector3(_scaleFactorToWidth, _scaleFactorToWidth, _scaleFactorToWidth);
    }

    private void CalculateScaleFactor()
    {
        _scaleFactorToWidth = _screenReferenceSize / _objectWidth * _relativeScale;
    }

    private void SetScreenReferenceSize()
    {
        if (_screenReferenceSize > 0)
        {
            _oldScreenReferenceSize = _screenReferenceSize;
        }
        if (Screen.width < Screen.height)
        {
            _screenReferenceSize = Screen.width;
        }
        else
        {
            _oldScreenReferenceSize = _screenReferenceSize;
            _screenReferenceSize = Screen.height;
        }
    }
}
