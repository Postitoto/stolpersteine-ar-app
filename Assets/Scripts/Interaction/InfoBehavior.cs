using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// When active, object gets scaled up, rotates and displays the information

public class InfoBehavior : MonoBehaviour
{
    const float SPEED = 6f;
    const float ROTATION_DEGREES_PER_SECOND = 15f;

    [SerializeField]
    private Transform _info;
    [SerializeField]
    private float _objectScalingFactor = 1f;
    [SerializeField]
    private bool _scaleUp = true;
    [SerializeField]
    private bool _rotate = true;

    private Vector3 _infoScale = Vector3.zero;
    private Vector3 _objectScale;
    private float _rotationSpeed = 0f;
    private bool _initializing;


    void OnEnable()
    {
        _initializing = true;
    }

    void Update()
    {
        _info.localScale = Vector3.Lerp(_info.localScale, _infoScale, Time.deltaTime * SPEED);
        if (!_initializing && _scaleUp)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _objectScale, Time.deltaTime * SPEED);
        }
        if (_rotate)
        {
            transform.Rotate(0f, _rotationSpeed * Time.deltaTime, 0f, Space.Self);

        }
    }

    public void DisplayInfo()
    {
        _infoScale = Vector3.one;
        _objectScale = Vector3.one * _objectScalingFactor;
        _rotationSpeed = ROTATION_DEGREES_PER_SECOND;
        _initializing = false;
    }

    public void HideInfo()
    {
        _infoScale = Vector3.zero;
        _objectScale = Vector3.one;
        _rotationSpeed = 0f;
    }

    public void SetInfoText(string text)
    {
        var textComponent = _info.GetComponentsInChildren<TextMeshPro>(true)[0];
        textComponent.text = text;
    }

}