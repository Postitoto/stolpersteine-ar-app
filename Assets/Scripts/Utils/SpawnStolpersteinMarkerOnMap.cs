namespace Mapbox.Examples
{
    using UnityEngine;
    using Mapbox.Utils;
    using Mapbox.Unity.Map;
    using Mapbox.Unity.MeshGeneration.Factories;
    using Mapbox.Unity.Utilities;
    using System.Collections.Generic;


    // Adapted SpawnOnMap from MapBox Examples and modified
    // Spawns specified gameObjects at stolperstein locations from given database
    // Also makes sure, that these GOs inherit the same layers as the GO that this script is attached to

    public class SpawnStolpersteinMarkerOnMap : MonoBehaviour
    {
        [SerializeField]
        AbstractMap _map;

        [SerializeField]
        private BackendInterface backendInterface;
        private List<string> locationStrings;
        Vector2d[] _locations;

        [SerializeField]
        float _spawnScale = 100f;

        [SerializeField]
        GameObject _markerPrefab;

        List<GameObject> _spawnedObjects;


        // Spawn the objects for the first time (not in Start(), since they might not be loaded from the database at that point)
        void SpawnInitially()
        {
            // Dont spawn, if no locations provided
            var locationStrings = backendInterface.StolpersteinLocations;
            if (locationStrings == null)
            {
                return;
            }

            _locations = new Vector2d[locationStrings.Count];
            _spawnedObjects = new List<GameObject>();
            for (int i = 0; i < locationStrings.Count; i++)
            {
                _locations[i] = Conversions.StringToLatLon(locationStrings[i]); ;
                var instance = Instantiate(_markerPrefab);
                instance.transform.localPosition = _map.GeoToWorldPosition(_locations[i], true);
                instance.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
                // Make sure, the gameobject and its children inherit the same layers
                instance.layer = this.gameObject.layer;
                instance.AddComponent<AssignLayerToChildren>();
                _spawnedObjects.Add(instance);
            }
        }

        private void Update()
        {
			// If no objects spawned yet, spawn for the first time
            if (_spawnedObjects == null)
            {
                SpawnInitially();
				return;
            }

            int count = _spawnedObjects.Count;
            for (int i = 0; i < count; i++)
            {
                var spawnedObject = _spawnedObjects[i];
                var location = _locations[i];
                spawnedObject.transform.localPosition = _map.GeoToWorldPosition(location, true);
                spawnedObject.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
                // Synchronize layers with the gameobject that the script is attached to
                spawnedObject.layer = this.gameObject.layer;
            }
        }
    }
}