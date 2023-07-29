using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Makes the object face the main camera

public class FaceCamera : MonoBehaviour
{
    private Transform _cam;
    private Vector3 _targetAngle = Vector3.zero;

    void Start()
    {
        _cam = Camera.main.transform;
    }

    void Update()
    {
        transform.LookAt(_cam);
        _targetAngle = transform.localEulerAngles;
        _targetAngle.x = 0;
        _targetAngle.z = 0;
        transform.localEulerAngles = _targetAngle;
    }

}