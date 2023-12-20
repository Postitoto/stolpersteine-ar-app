using System;
using System.Collections.Generic;
using Mapbox.CheapRulerCs;
using Mapbox.Unity.Location;
using Mapbox.Unity.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using LocationSO = Scriptables.Location;


[Serializable]
public class StringEvent : UnityEvent<string> { }



// Use mapbox to get the current location and compare against locations of stolpersteine
// Fire events if stolperstein radius is entered or left

public class LocationHandler : MonoBehaviour
{
    public delegate void NotifyWithinRadiusChanged(bool inside);
    public static event NotifyWithinRadiusChanged OnInsideRadiusChanged;


    [SerializeField]
    private BackendInterface _backendInterface;
    [SerializeField]
    private TourManager tourManager;
    
    
    [SerializeField] 
    private LocationMarkerHandler locationMarkerHandler;
    
    [SerializeField]
    private float _stolpersteinRadius = 10;

    [SerializeField]
    public StringEvent stolpersteinRadiusEntered;
    [SerializeField]
    private UnityEvent stolpersteinRadiusLeft;

    // Prefab
    [SerializeField]
    private GameObject _locationNameObj;

    private List<LocationSO> locations;
    private LocationSO activeLocation;
    
    private Boolean insideLocation = false;
    public LocationSO ActiveLocation => activeLocation;
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
        locations = _backendInterface.Locations;
        if (locations == null || locations.Count == 0)
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
        LocationSO closestLocation = null;
        double closestDist = double.PositiveInfinity;
        foreach (var loc in locations)
        {
            // Use MapBox functions to calculate distance
            var mapBoxLocation = Conversions.StringToLatLon(loc.coordinates);
            var dist = _ruler.Distance(new double[] { lat, lon }, new double[] { mapBoxLocation.x, mapBoxLocation.y });

            if (dist >= closestDist) continue;
            closestDist = dist;
            closestLocation = loc;
        }

        // Check if the area of a Stolperstein was entered
        if (closestDist <= _stolpersteinRadius && (activeLocation == null || closestLocation != activeLocation))
        {
            insideLocation = true;
            activeLocation = closestLocation;
            if (activeLocation != null)
            {
                stolpersteinRadiusEntered.Invoke(activeLocation.coordinates);
                Debug.Log("Entered Location " + activeLocation.address);
            }
           
            OnInsideRadiusChanged?.Invoke(true);
        }
        // Check if the area of a Stolperstein was left
        else if (closestDist > _stolpersteinRadius && activeLocation != null)
        {
            insideLocation = false;             
            stolpersteinRadiusLeft.Invoke();
            OnInsideRadiusChanged?.Invoke(false);
            activeLocation = null;
        }

        // Sets Info Text for inside/outside location
        if (insideLocation)
        {       
            if (activeLocation != null)
            {
                SetLocationName("Inside Location: " + activeLocation.address); 
            } else
            {
                SetLocationName("Inside invalid location!");
            }
        }
        else
        {
            SetLocationName("Closest Location: " + closestLocation.address + " (" + Math.Round(closestDist, 2, MidpointRounding.ToEven) + "m)");
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
        _locationNameObj.GetComponent<TextMeshProUGUI>().text = name;      
    }

}
