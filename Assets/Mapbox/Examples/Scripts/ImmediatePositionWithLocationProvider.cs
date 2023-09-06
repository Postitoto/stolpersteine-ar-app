namespace Mapbox.Examples
{
	using Mapbox.Unity.Location;
	using Mapbox.Unity.Map;
	using UnityEngine;

	public class ImmediatePositionWithLocationProvider : MonoBehaviour
	{
		[Range(0.00001f, 1.0f)] public float interest;
		private Vector3 targetPos;

		public bool isMoving;
		bool _isInitialized;

		ILocationProvider _locationProvider;
		ILocationProvider LocationProvider
		{
			get
			{
				if (_locationProvider == null)
				{
					_locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
				}

				return _locationProvider;
			}
		}

		Vector3 _targetPosition;

		void Start()
		{
			LocationProviderFactory.Instance.mapManager.OnInitialized += () => _isInitialized = true;
		}

		void LateUpdate()
		{
			if (!_isInitialized) return;
			if (!isMoving) return;
			//MyWay();
			TheirWay();
			
			
		}

		private void MyWay()
		{
			if (transform.localPosition != targetPos)
			{
				transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos, interest);
			}
			else
			{
				var map = LocationProviderFactory.Instance.mapManager;
				targetPos = map.GeoToWorldPosition(LocationProvider.CurrentLocation.LatitudeLongitude);
			}
		}

		private void TheirWay()
		{
			var map = LocationProviderFactory.Instance.mapManager;
			transform.localPosition = map.GeoToWorldPosition(LocationProvider.CurrentLocation.LatitudeLongitude);
		}

		public void SetPosition()
		{
			transform.position = new Vector3(-39996.80f, 0, -61587.33f);
		}
	}
}