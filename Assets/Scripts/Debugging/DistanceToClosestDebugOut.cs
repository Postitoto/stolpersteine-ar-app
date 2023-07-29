using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DistanceToClosestDebugOut : MonoBehaviour
{
    public LocationHandler locationHandler;
    public Text textComponent;

    // Update is called once per frame
    void Update()
    {
        textComponent.text = string.Format("Distance to closest Stolperstein:\n{0:0.00}m" , locationHandler._distanceToClosestStolperstein);
    }
}
