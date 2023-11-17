using System.Collections;
using System.Collections.Generic;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using Scriptables;
using UnityEngine;

namespace Mapbox.Examples
{
    // Adapted SpawnOnMap from MapBox Examples and modified
    // Spawns specified gameObjects at stolperstein locations from given database
    // Also makes sure, that these GOs inherit the same layers as the GO that this script is attached to

    public class SpawnStolpersteinMarkerOnMap : MonoBehaviour
    {
        public Dictionary<GameObject, Vector2d> SpawnedMarkers => spawnedMarkers;
        
        [SerializeField] private GameObject markerPrefab;
        [SerializeField] private AbstractMap map;
        [SerializeField] private BackendInterface backendInterface;
        [SerializeField] private Transform mapStoneParent;
        [SerializeField] private GameObject listElementPrefab;
        [SerializeField] private Transform listElementParent;
        [SerializeField] private float spawnScale = 10f;
        [SerializeField] private float spawnInterval = 0.1f;

        private Dictionary<GameObject, Vector2d> spawnedMarkers;
        private List<Stolperstein> stolpersteine;
        private List<Location> locations;
        private Vector3 scaleVector;

        private void Awake()
        {
            scaleVector = new Vector3(spawnScale, 10, spawnScale);
            BackendInterface.OnLocationsLoaded += () =>
            {
                StartCoroutine(SpawnInitially());
            };
        }
        
        private IEnumerator SpawnInitially()
        {
            spawnedMarkers = new Dictionary<GameObject, Vector2d>();
            stolpersteine = backendInterface.Stolpersteine;
            locations = backendInterface.Locations;

            // Waiting for the map to map Zoom to be initialized, otherwise the stone positions will always be (0,0,0)
            // It's not very clean but whatever, it's way cleaner than the previous solution
            yield return new WaitUntil(() => map.InitialZoom != 0);
            
            foreach (var stone in locations)
            {
                var loc = Conversions.StringToLatLon(stone.coordinates);
                
                // Instantiate the 3D stone on the map
                var instance = Instantiate(markerPrefab, mapStoneParent, true);
                instance.name = stone.address.Replace(" ", "");
                instance.transform.position = map.GeoToWorldPosition(loc, false) * map.WorldRelativeScale;
                instance.transform.localScale = scaleVector;
                
                // Make sure, the gameobject and its children inherit the same layers
                instance.layer = gameObject.layer;
                instance.AddComponent<AssignLayerToChildren>();
                spawnedMarkers.Add(instance, loc);
                
                // Create the corresponding 2D List element
                var entry = Instantiate(listElementPrefab, listElementParent, true);
                entry.transform.localScale = new Vector3(1, 1, 1);
                entry.GetComponent<StoneListEntryBehaviour>().Init(instance, stone.address);
                
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void Update()
        {
            if (spawnedMarkers != null)
            {
                UpdateMarkerPositions();
            }
        }

        private void UpdateMarkerPositions()
        {
            foreach (var marker in spawnedMarkers)
            {
                var location = marker.Value;
                var spawnedObject = marker.Key;
                spawnedObject.transform.localPosition = map.GeoToWorldPosition(location, false) * map.WorldRelativeScale;
                spawnedObject.transform.localScale = scaleVector;
            }
            
            /*int count = spawnedMarkers.Count;
          for (int i = 0; i < count; i++)
          {
              var spawnedObject = spawnedMarkers[i];
              var location = _locations[i];
              spawnedObject.transform.localPosition = _map.GeoToWorldPosition(location, true);
              spawnedObject.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
              
              
              
              
              // Synchronize layers with the gameobject that the script is attached to
              // spawnedObject.layer = this.gameObject.layer;
          }*/
        }
        

       
    }
}