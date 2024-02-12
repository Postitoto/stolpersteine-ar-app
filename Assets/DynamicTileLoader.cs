
using Mapbox.Map;
using Mapbox.Platform;
using Mapbox.Unity.Map;
using Mapbox.Utils;

using UnityEngine;

public class DynamicTileLoader : MonoBehaviour
{
    public AbstractMap map; // Reference to your Mapbox map component

    private Vector3 lastLoadedLocation;

    private void Start()
    {
        // Initialize lastLoadedLocation with an initial position
        lastLoadedLocation = transform.position;
    }

    private void Update()
    {
        // Check if the camera view has moved to a new location
        var currentLocation = transform.position;
        if (currentLocation != lastLoadedLocation)
        {
            // Camera view has moved, load new tiles
            LoadNewTiles(currentLocation);

            // Update lastLoadedLocation to the current location
            lastLoadedLocation = currentLocation;
        }
    }

    private void LoadNewTiles(Vector3 currentLocation)
    {
        var latLong = map.WorldToGeoPosition(currentLocation);
        //map.UpdateMap(latLong);
        
    }
}
