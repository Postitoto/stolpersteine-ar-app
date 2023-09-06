using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.Directions;
using Mapbox.Examples;
using Mapbox.Unity;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Modifiers;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;

public class Navigation : MonoBehaviour
{
	public RoutingProfile currentRoutingProfile = RoutingProfile.Walking;
	public bool isNavigationMode;

	[SerializeField] private AbstractMap map;
	[SerializeField] private GameObject player;
	[SerializeField] private CameraBehaviour camera;
	[SerializeField] private MeshModifier[] meshModifiers;
	[SerializeField] private Material material;
	
	[SerializeField]
	[Range(1,10)]
	private float updateFrequency = 2;

	private SpawnStolpersteinMarkerOnMap spawner;
	private Dictionary<GameObject, Vector2d> spawnederMarkers;
	private Directions directions;
	private GameObject directionsGameObject;
	private Transform currentOrigin;
	private Transform currentDestination;
	
	private bool isPlayerToTargetMode;

	protected virtual void Awake()
	{
		if (map == null)
		{
			map = FindObjectOfType<AbstractMap>();
		}
		
		directions = MapboxAccess.Instance.Directions;
		map.OnInitialized += Query;
		map.OnUpdated += Query;
		spawner = map.gameObject.GetComponent<SpawnStolpersteinMarkerOnMap>();
	}
	
	private void Start()
	{
		foreach (var modifier in meshModifiers)
		{
			modifier.Initialize();
		}
		StartCoroutine(QueryTimer());
	}
	
	public void NavigateToClosestStoneAction()
	{
		if (isNavigationMode)
		{
			EndNavigationMode();
		}
		else
		{
			CalculateDirectionsToClosestStone();
		}
	}
	
	private void CalculateDirectionsToClosestStone()
	{
		GameObject closestStone = null;
		var dist = Double.MaxValue;
		foreach (var marker in spawner.SpawnedMarkers)
		{
			var tmp = Vector3.Distance(marker.Key.transform.position, player.transform.position);
			if (tmp > dist) continue;
			dist = tmp;
			closestStone = marker.Key;
		}
		CalculateDirectionsToSelectedStone(closestStone);
	}

	public void CalculateDirectionsToSelectedStone(GameObject stone)
	{
		currentOrigin = player.transform;
		currentDestination = stone.transform;
		isNavigationMode = true;
		isPlayerToTargetMode = true;
		
		camera.FocusOnTargetPoints(player.transform, stone.transform);
	}
	
	private void CalculateDirectionsFromAtoB(Vector2d origin, Vector2d destination)
	{
		// TODO If I ever implement a target to target navigation 
		
		// currentOrigin = origin;
		// currentDestination = destination;
		// isNavigationMode = true;
		// isPlayerToTargetMode = false;
	}
	public void EndNavigationMode()
	{
		isNavigationMode = false;
		if (directionsGameObject != null)
		{
			directionsGameObject.Destroy();
		}
	}
	
	private IEnumerator QueryTimer()
	{
		while (true)
		{
			yield return new WaitForSeconds(updateFrequency);
			Query();
		}
	}
	
	private void Query()
	{
		if (!isNavigationMode)
			return;

		if (isPlayerToTargetMode)
		{
			currentOrigin = player.transform;
		}
		
		Vector2d[] waypoints = new Vector2d[2];
		waypoints[0] = currentOrigin.GetGeoPosition(map.CenterMercator, map.WorldRelativeScale);
		waypoints[1] = currentDestination.GetGeoPosition(map.CenterMercator, map.WorldRelativeScale);
		
		DirectionResource route = new DirectionResource(waypoints, currentRoutingProfile);
		route.Steps = true;
		directions.Query(route, HandleDirectionsResponse);
	}
	
	private void HandleDirectionsResponse(DirectionsResponse response)
	{
		if (response == null || null == response.Routes || response.Routes.Count < 1)
		{
			return;
		}

		var meshData = new MeshData();
		var dat = new List<Vector3>();
		foreach (var point in response.Routes[0].Geometry)
		{
			dat.Add(Conversions.GeoToWorldPosition(point.x, point.y, map.CenterMercator, map.WorldRelativeScale)
				.ToVector3xz());
		}

		var feat = new VectorFeatureUnity();
		feat.Points.Add(dat);

		foreach (MeshModifier mod in meshModifiers.Where(x => x.Active))
		{
			mod.Run(feat, meshData, map.WorldRelativeScale);
		}

		CreateGameObject(meshData);
	}
	
	private void CreateGameObject(MeshData data)
	{
		if (directionsGameObject != null)
		{
			directionsGameObject.Destroy();
		}

		directionsGameObject = new GameObject("routeGameObject");
		directionsGameObject.layer = 3; // need to set this to the map layer or else the map camera doesn't render it
		var mesh = directionsGameObject.AddComponent<MeshFilter>().mesh;
		mesh.subMeshCount = data.Triangles.Count;

		mesh.SetVertices(data.Vertices);
		for (int i = 0; i < data.Triangles.Count; i++)
		{
			var triangle = data.Triangles[i];
			mesh.SetTriangles(triangle, i);
		}

		for (int i = 0; i < data.UV.Count; i++)
		{
			var uv = data.UV[i];
			mesh.SetUVs(i, uv);
		}

		mesh.RecalculateNormals();
		directionsGameObject.AddComponent<MeshRenderer>().material = material;
	}
}

