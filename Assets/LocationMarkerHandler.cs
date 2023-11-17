using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.CheapRulerCs;
using Mapbox.Unity.Location;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using LocationSO = Scriptables.Location;

public class LocationMarkerHandler : MonoBehaviour
{
    [SerializeField] private AbstractMap map;
    [SerializeField] private BackendInterface backendInterface;
    [SerializeField] private ARSessionOrigin originPoint;
    [SerializeField] private ARSessionOrigin aRSessionOrigin;
    [SerializeField] private GameObject locationPrefab;
    [SerializeField] private float detectionRadius;
    [SerializeField] private float markerDistanceFromOrigin;
    [SerializeField] private float spawnInterval;
    [SerializeField] private float updateInterval;
    
    private Dictionary<Vector2d, LocationSO> mapLocations;
    private Dictionary<Vector2d, GameObject> spawnedLocationMarkers;
    private CheapRuler ruler;
    private GameManager gameManager;
    private ARAnchorManager anchorManager;
    
    // Start is called before the first frame update
    void Awake()
    {
        BackendInterface.OnLocationsLoaded += () =>
        {
            var locations = backendInterface.Locations;
            mapLocations = new Dictionary<Vector2d, LocationSO>();
            foreach (var loc in locations)
            {
                var key = Conversions.StringToLatLon(loc.coordinates);
                mapLocations.Add(key, loc);
            }

            StartCoroutine(SpawnLocationsOnStart());
        };

        spawnedLocationMarkers = new Dictionary<Vector2d, GameObject>();
    }

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        anchorManager = aRSessionOrigin.GetComponent<ARAnchorManager>();

        StartCoroutine(UpdateARLocationObjects());
    }

    private IEnumerator SpawnLocationsOnStart()
    {
        yield return new WaitUntil(() => map.InitialZoom != 0);

        var playerPos = GetPlayerPosition();
        var lat = playerPos.x;
        var lon = playerPos.y;
        
        // Filtering out locations that are to far away
        var current = new double[] {lat, lon};
        var inRadiusLocations = mapLocations.Where(vec2d =>
        {
            var loc = new double[] {vec2d.Key.x, vec2d.Key.y};
            var dist = ruler.Distance(current, loc);
            gameManager.Log($"distance: {dist}");
            return dist <= detectionRadius;
        }).ToDictionary(loc => loc.Key, loc => loc.Value);
        
        
        // We spawn the invisible anchors at the actual location 
        // Since we don't want to up the clipping plane of the camera we 
        // create another visible GameObject between the location and the camera 
        // that is updated all the time
        foreach (var location in inRadiusLocations)
        {
            var parent = new GameObject(location.Value.address);
            parent.transform.SetParent(transform);
            parent.transform.position = Vector3.zero;
            
            // Spawn anchor at world position
            var worldPos = Conversions.GeoToWorldPosition(location.Key, map.CenterMercator).ToVector3xz();
            var anchor = new GameObject("Anchor");
            anchor.transform.position = worldPos;
            anchor.transform.SetParent(parent.transform);
            anchor.AddComponent<ARAnchor>();
            
            // Instantiate location marker GameObject
            var locationMarker = Instantiate(locationPrefab, parent.transform, false);
            locationMarker.name = "Marker";
            locationMarker.GetComponent<ARLocationBehaviour>().SetAddress(location.Value.address);

            spawnedLocationMarkers.Add(location.Key, parent);

            yield return new WaitForSeconds(spawnInterval);
        }
        
        gameManager.Log($"{spawnedLocationMarkers.Count} objects spawned");
    }

    private IEnumerator UpdateARLocationObjects()
    {
        while (true)
        {
            var originGeoPosition = GetPlayerPosition();
            var originPosArray = new double[] { originGeoPosition.x, originGeoPosition.y };
            var originWorldPosition = aRSessionOrigin.transform.position;
            
            foreach (var container in spawnedLocationMarkers)
            {
                var marker = container.Value.GetComponentInChildren<ARLocationBehaviour>();
                var anchor = container.Value.GetComponentInChildren<ARAnchor>();
                
                // Calculate the world position of the marker between the anchor and the origin
                // given a certain distance away from the origin
                var midpoint = (anchor.transform.position + originWorldPosition) / 2.0f;
                var direction = (midpoint - originWorldPosition).normalized;
                var position = originWorldPosition + direction * markerDistanceFromOrigin;
                marker.transform.position = position;
                
                // We need the geo position to fetch a distance in metric measurement units  
                var geoPosition = map.WorldToGeoPosition(anchor.transform.position);
                var loc = new double[] {geoPosition.x, geoPosition.y};
                var dist = ruler.Distance(loc, originPosArray);
                //var dist = GameManager.CalculateDistanceBetweenPoints(originGeoPosition, geoPosition);
                
                
                marker.SetDistance(dist);
                marker.SetScale((float) 1);
                marker.LookAt(originWorldPosition);
            }
            
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private Vector2d GetPlayerPosition()
    {
        var playerPos = map.WorldToGeoPosition(aRSessionOrigin.transform.position);
        ruler = new CheapRuler(playerPos.x, CheapRulerUnits.Meters);
        return playerPos;
    }
}
