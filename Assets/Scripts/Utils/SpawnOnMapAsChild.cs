using UnityEngine;
using Mapbox.Utils;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Unity.Utilities;
using System.Collections.Generic;

// Spawns the objects on the map and makes them children of map GO
// Modified version of MapBox's SpawnOnMap

public class SpawnOnMapAsChild : MonoBehaviour
{
    public AbstractMap Map { get; set; }
    public float SpawnScale { get; set; }
    public List<(string, GameObject)> ObjectsToSpawn { get; set; }

    Vector2d[] _locations;
    List<GameObject> _spawnedObjects;

    void Start()
    {
        _locations = new Vector2d[ObjectsToSpawn.Count];
        _spawnedObjects = new List<GameObject>();
        for (int i = 0; i < ObjectsToSpawn.Count; i++)
        {
            _locations[i] = Conversions.StringToLatLon(ObjectsToSpawn[i].Item1);
            var instance = ObjectsToSpawn[i].Item2;
            instance.transform.localPosition = Map.GeoToWorldPosition(_locations[i], true);
            instance.transform.localScale = new Vector3(SpawnScale, SpawnScale, SpawnScale);
            // Set the instances as children of the map
            instance.SetActive(false);
            instance.transform.SetParent(Map.transform);
            _spawnedObjects.Add(instance);
        }
    }

    void Update()
    {
        int count = _spawnedObjects.Count;
        for (int i = 0; i < count; i++)
        {
            var spawnedObject = _spawnedObjects[i];
            var location = _locations[i];
            // Take the global position, since we have the map as parent
            spawnedObject.transform.position = Map.GeoToWorldPosition(location, true);
            spawnedObject.transform.localScale = new Vector3(SpawnScale, SpawnScale, SpawnScale);
            spawnedObject.SetActive(Map.gameObject.activeInHierarchy);
        }
    }
}