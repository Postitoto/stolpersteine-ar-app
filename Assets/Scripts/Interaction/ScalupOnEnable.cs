using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Makes the object "pop up" on enable/disable, by scaling it up to 1 / down to 0

public class ScalupOnEnable : MonoBehaviour
{
    private float _velocity = 0f;
    private float _smoothTime = 0.3f;


    void OnEnable()
    {
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        var lScale = transform.localScale;
        if (lScale.x < 1 && lScale.y < 1 && lScale.z < 1)
        {
            var scaleX = Mathf.SmoothDamp(lScale.x, 1f, ref _velocity, _smoothTime);
            var scaleY = Mathf.SmoothDamp(lScale.y, 1f, ref _velocity, _smoothTime);
            var scaleZ = Mathf.SmoothDamp(lScale.z, 1f, ref _velocity, _smoothTime);
            transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
        }
    }

}
