using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

using Mapbox.CheapRulerCs;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Location;
using UnityEngine.UI;



[System.Serializable]
public class StringEvent : UnityEvent<string> { }

// Use mapbox to get the current location and compare against locations of stolpersteine
// Fire events if stolperstein radius is entered or left

public class LocationHandler : MonoBehaviour
{
    [SerializeField]
    private BackendInterface _backendInterface;
    [SerializeField]
    private float _stolpersteinRadius = 10;

    [SerializeField]
    private StringEvent stolpersteinRadiusEntered;
    [SerializeField]
    private UnityEvent stolpersteinRadiusLeft;

    // Prefab
    [SerializeField]
    private GameObject _locationNameObj;

    private List<string> _stolpersteinLocations;
    private Dictionary<string, string> _stolpersteinLocationNames;
    private string _activeLocation = null;
    private Boolean insideLocation = false;
    public string ActiveLocation { get { return _activeLocation; } }
    public double _distanceToClosestStolperstein = double.PositiveInfinity; // only for debug usage


    // CheapRuler.Distance allows approximation for GPS distances that are relatively close
    private CheapRuler _ruler;

    void Start()
    {
        // Access location from mapbox locationProvider
        LocationProviderFactory.Instance.DefaultLocationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;

        // Start the application with stolpersteinRadiusLeft event to reset everything
        stolpersteinRadiusLeft.Invoke();

        // Initially display location info text
        _locationNameObj.SetActive(true);
    }


    ///<summary>
    ///     Find the closest stolperstein and check if the user has entered or left its radius.
    ///     Fires according events
    ///<summary>
    void LocationProvider_OnLocationUpdated(Location currentLocation)
    {
        // Update the stolperstein locations, if available
        _stolpersteinLocations = _backendInterface.StolpersteinLocations;
        _stolpersteinLocationNames = _backendInterface.StolpersteinLocationNames;
        if (_stolpersteinLocations == null || _stolpersteinLocations.Count == 0)
        {
            return;
        }

        double lat = currentLocation.LatitudeLongitude.x;
        double lon = currentLocation.LatitudeLongitude.y;

        if (_ruler == null)
        {
            _ruler = new CheapRuler(lat, CheapRulerUnits.Meters);
        }

        // Determine closest stolperstein
        string closestStolperstein = null;
        double closestDist = double.PositiveInfinity;
        foreach (string location_string in _stolpersteinLocations)
        {
            // Use MapBox functions to calculate distance
            var loc = Conversions.StringToLatLon(location_string);
            double dist = _ruler.Distance(new double[] { lat, lon }, new double[] { loc.x, loc.y });

            if (dist < closestDist)
            {
                closestDist = dist;
                closestStolperstein = location_string;
            }
        }

        // Check if the area of a stolperstein was entered
        if (closestDist <= _stolpersteinRadius && (_activeLocation == null || closestStolperstein != _activeLocation))
        {
            insideLocation = true;
            _activeLocation = closestStolperstein;
            stolpersteinRadiusEntered.Invoke(_activeLocation);
            string locName = _stolpersteinLocationNames[_activeLocation];
            Debug.Log("Entered Location " + locName);
        }
        // Check if the area of a stolperstein was left
        else if (closestDist > _stolpersteinRadius && _activeLocation != null)
        {
            insideLocation = false;             
            stolpersteinRadiusLeft.Invoke();
            _activeLocation = null;
        }

        // Sets Info Text for inside/outside location
        if (insideLocation)
        {       
            if (_stolpersteinLocationNames[_activeLocation] != null)
            {
                SetLocationName("Inside Location: " + _stolpersteinLocationNames[_activeLocation]); 
            } else
            {
                SetLocationName("Inside invalid location!");
            }
        }
        else
        {
            SetLocationName("Closest Location: " + _stolpersteinLocationNames[closestStolperstein] + " (" + Math.Round(closestDist, 2, MidpointRounding.ToEven) + "m)");
        }
        // Debug Info
        _distanceToClosestStolperstein = closestDist;
    }

    // Sets the radius for a stolperstein, e.g. for testing purposes
    public void setRadius(int radius)
    {
        _stolpersteinRadius = radius;
    }

    private void SetLocationName(string name)
    {
        // Initializing Text
        _locationNameObj.GetComponent<Text>().text = name;      
    }

}
