using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Utils;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public TextMeshProUGUI logField;

    public void Log(string message)
    {
        logField.text = $"[LOG]\t\t{message}";
    }


    public static double CalculateDistanceBetweenPoints(Vector2d coord1, Vector2d coord2)
    {
        var earthRadius = 6371e3;
        var phiOne = DegreesToRadians(coord1.x);
        var phiTwo = DegreesToRadians(coord2.x);
        var deltaLat = DegreesToRadians(coord2.x - coord1.x);
        var deltaLon = DegreesToRadians(coord2.y - coord1.y);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(phiOne) * Math.Cos(phiTwo) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        var dist = earthRadius * c;
        return dist;
    }

    public static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180; 
    }
}
