using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.Utilities;
using Scriptables;
using UnityEngine;

public class TourManager : MonoBehaviour
{
    [SerializeField] private BackendInterface backendInterface;
    [SerializeField] private TourListBehaviour tourList;
    [SerializeField] private Navigation navigation;

    private Tour currentTour;
    private int tourLocationIndex;
    private bool isActiveTour = false;
    
    private void Awake()
    {
        BackendInterface.OnToursLoaded += () =>
        {
            tourList.PopulateTourList(backendInterface.Tours);
        };
    }

    public void StartTour(Tour tour)
    {
        currentTour = tour;
        isActiveTour = true;
        tourLocationIndex = 0;
        
        Debug.Log($"Started tour {tour.name}");
        
        DisableLocationsOutsideOfTour();
        
        NavigateToNextLocation();
    }

    public void EndTour()
    {
        currentTour = null;
        isActiveTour = false;
        
        SetAllLocationsActive();
    }

    public void ContinueTour()
    {
        // Disable current location
        
        tourLocationIndex++;
        NavigateToNextLocation();
    }

    private void NavigateToNextLocation()
    {
        var coordVector = Conversions.StringToLatLon(currentTour.locationsInOrder[tourLocationIndex].coordinates);
        navigation.CalculateDirectionsToSelectedStone(coordVector);
    }

    private void SetAllLocationsActive()
    {
        
    }
    
    private void DisableLocationsOutsideOfTour()
    {
        
    }
}
