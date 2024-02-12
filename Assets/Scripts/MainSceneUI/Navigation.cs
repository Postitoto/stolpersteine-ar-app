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
using Unity.Mathematics;
using UnityEngine;

public class Navigation : MonoBehaviour
{
	public RoutingProfile currentRoutingProfile = RoutingProfile.Walking;
	public bool isNavigationMode;

	[SerializeField] private AbstractMap map;
	[SerializeField] private GameObject player;
	[SerializeField] private Camera arCamera;
	[SerializeField] private CameraBehaviour camera;
	[SerializeField] private MeshModifier[] meshModifiers;
	[SerializeField] private Material material;
	[SerializeField] private GameObject navigationArrowPrefab;
	[SerializeField] private GameObject footPrintPrefab;
	
	[SerializeField]
	[Range(1,10)]
	private float updateFrequency = 2;

	private SpawnStolpersteinMarkerOnMap spawner;
	private Dictionary<GameObject, Vector2d> spawnederMarkers;
	private Directions directions;
	private GameObject directionsGameObject;
	private GameObject navigationArrow;
	private Transform currentOrigin;
	private Transform currentDestination;

	private GameObject[] prints;
	private bool isPlayerToTargetMode;
	private int previousGeometryCount;

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
			var tmp = Vector3.Distance(marker.Value.transform.position, player.transform.position);
			if (tmp > dist) continue;
			dist = tmp;
			closestStone = marker.Value;
		}
		CalculateDirectionsToSelectedStone(closestStone);
	}

	public void CalculateDirectionsToSelectedStone(Vector2d coordinates)
	{
		var stone = spawner.SpawnedMarkers.FirstOrDefault(x => x.Key.Equals(coordinates)).Value;
		if (stone == null)
		{
			return;
		}
		
		CalculateDirectionsToSelectedStone(stone);
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
		if(directionsGameObject != null)
		{
			directionsGameObject.Destroy();
		}

		if(navigationArrow != null)
		{
			navigationArrow.Destroy();
		}
		
		foreach (var print in prints)
		{
			print.Destroy();
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
		
		CreateARNavigationArrow(response.Routes.First());
		
		CreateARNavigationFootSteps(response.Routes.First());
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

	private void CreateARNavigationArrow(Route route)
	{
		if (route == null)
		{
			return;
		}

		if (route.Geometry.Count < 3)
		{
			navigationArrow.Destroy();
			return;
		}

		var arrowGeoPos = route.Geometry[1]; // First is the users position, second is where the arrow should be
		var targetGeoPos = route.Geometry[2]; // Third is the next point in the route, the arrow should face towards this point
		var arrowWorldPos = map.GeoToWorldPosition(arrowGeoPos, true) + Vector3.up;
		var targetWorldPos = map.GeoToWorldPosition(targetGeoPos, true);
		
		if (navigationArrow == null)
		{
			navigationArrow = Instantiate(navigationArrowPrefab);
		}

		var camPos = arCamera.transform.position;
		var distFromCamera = Vector3.Distance(camPos, arrowWorldPos);
		if (distFromCamera >= arCamera.farClipPlane)
		{
			var midpoint = (arrowWorldPos + camPos) / 2.0f;
			var direction = (midpoint - camPos).normalized;
			navigationArrow.transform.position = camPos + direction * arCamera.farClipPlane;
			navigationArrow.transform.LookAt(arrowWorldPos);
		}
		else
		{
			navigationArrow.transform.position = arrowWorldPos;
			navigationArrow.transform.LookAt(targetWorldPos);
		}
	}

	private void CreateARNavigationFootSteps(Route route)
	{
		// if (previousGeometryCount == route.Geometry.Count)
		// {
		// 	return;
		// }

		previousGeometryCount = route.Geometry.Count;
		
		var targetPos = route.Geometry[1];
		var targetWorldPos = map.GeoToWorldPosition(targetPos);
		var playerPos = player.transform.position;
		
		var directionVectorNormalized = (targetWorldPos - playerPos).normalized;
		var dist = Vector3.Distance(playerPos, targetWorldPos);
		
		var printCount = (int) (dist / 1.5);
		var distBetweenPrints = dist / printCount;
		
		ClearPrints();
		prints = new GameObject[printCount];
		for (int i = 0; i < printCount; i++)
		{
			var distFromPlayer = distBetweenPrints * i;
			//var position = playerPos + directionVectorNormalized * distFromPlayer;
			
			if (i % 2 == 0)
			{
				prints[i] = Instantiate(footPrintPrefab);
				prints[i].transform.LookAt(targetWorldPos);
				
				var pos = playerPos + directionVectorNormalized * distFromPlayer;
				pos.y -= 2f;
				prints[i].transform.position = pos;
			}
			else
			{
				prints[i] = Instantiate(footPrintPrefab);
				prints[i].transform.LookAt(targetWorldPos);
				
				var pos = playerPos + directionVectorNormalized * distFromPlayer;
				pos.y -= 2f;
				prints[i].transform.position = pos;
			}
		}
	}

	private void ClearPrints()
	{
		if (prints == null)
		{
			return;
		}
		
		foreach (var print in prints.Where(p => p != null))
		{
			Destroy(print);
		}
	}
}

