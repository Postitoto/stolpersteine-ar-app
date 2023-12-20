using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Unity.Utilities;
using Scriptables;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class TourManager : MonoBehaviour
{
    [SerializeField] private BackendInterface backendInterface;
    [SerializeField] private LocationHandler locationHandler;
    [SerializeField] private ARSessionOrigin arSessionOrigin;
    [SerializeField] private TourListBehaviour tourList;
    [SerializeField] private Navigation navigation;
    [SerializeField] private GameObject tourMenu;
    [SerializeField] private GameObject tourGuideButton;
    [SerializeField] private GameObject tourGuidePrefab;
    
    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private ARAnchorManager anchorManager;
    
    private Tour currentTour;
    private TourGuideBehaviour currentTourGuide;
    private int tourLocationIndex;
    private bool isActiveTour;
    private bool isGuideSelected;
    
    private void Awake()
    {
        raycastManager = arSessionOrigin.GetComponent<ARRaycastManager>();
        planeManager = arSessionOrigin.GetComponent<ARPlaneManager>();
        anchorManager = arSessionOrigin.GetComponent<ARAnchorManager>();

        BackendInterface.OnToursLoaded += () =>
        {
            tourList.PopulateTourList(backendInterface.Tours);
        };

        locationHandler.stolpersteinRadiusEntered.AddListener(CheckEnteredLocation);
    }

    public void StartTour(Tour tour)
    {
        currentTour = tour;
        isActiveTour = true;
        tourLocationIndex = 0;
        tourMenu.SetActive(true);
        
        Debug.Log($"Started tour {tour.name}");
        
        DisableLocationsOutsideOfTour();
        
        NavigateToNextLocation();
    }

    public void EndTour()
    {
        currentTour = null;
        isActiveTour = false;
        tourMenu.SetActive(false);
        
        SetAllLocationsActive();
    }

    public void ContinueTour()
    {
        // Disable current location
        
        // Destroy Tour Guide
        if (currentTourGuide.gameObject != null)
        {
            Destroy(currentTourGuide.gameObject);
        }
        
        tourLocationIndex++;
        NavigateToNextLocation();
    }

    private void NavigateToNextLocation()
    {
        var coordVector = Conversions.StringToLatLon(currentTour.locationsInOrder[tourLocationIndex].coordinates);
        navigation.CalculateDirectionsToSelectedStone(coordVector);
    }

    private void CheckEnteredLocation(string coordinates)
    {
        var currentTourLoc = currentTour.locationsInOrder[tourLocationIndex];
        if (!coordinates.Equals(currentTourLoc.coordinates))
        {
            return;
        }

        if (!string.IsNullOrEmpty(currentTourLoc.audio))
        {
            StartCoroutine(backendInterface.DownloadAudio(currentTourLoc.audio, clip =>
            {
                currentTourLoc.audioClip = clip;
                tourGuideButton.SetActive(true);
            }));
        }
    }

    private void HandleTouchInput()
    {
        if (!isGuideSelected)
        {
            return;
        }

        if (Input.touchCount == 0)
        {
            return;
        }

        var touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Ended)
        {
            return;
        }


        var locationOnScreen = touch.position;
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(locationOnScreen, hits))
        {
            InstantiateGuide(hits.First());
            tourGuideButton.SetActive(false);
            isGuideSelected = false;
        }
    }

    private void InstantiateGuide(ARRaycastHit raycastHit)
    {
        if (currentTourGuide.gameObject != null)
        {
            Destroy(currentTourGuide.gameObject);
        }
        
        var hitPose = raycastHit.pose;
        var hitTrackableId = raycastHit.trackableId;
        var hitPlane = planeManager.GetPlane(hitTrackableId);
        
        var direction = Vector3.ProjectOnPlane(Camera.main.transform.forward, hitPlane.normal);
        var pose = new Pose(hitPose.position, Quaternion.LookRotation(direction));
        var currentAnchor = anchorManager.AttachAnchor(hitPlane, pose);

        currentTourGuide = Instantiate(tourGuidePrefab, currentAnchor.transform, false).GetComponent<TourGuideBehaviour>();
        currentTourGuide.SetAudioClip(currentTour.locationsInOrder[tourLocationIndex].audioClip);
        
        tourGuideButton.SetActive(false);
    }

    private void SetAllLocationsActive()
    {
        
    }
    
    private void DisableLocationsOutsideOfTour()
    {
        
    }
}
