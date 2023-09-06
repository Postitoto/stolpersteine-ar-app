using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Mapbox.Examples
{
	using UnityEngine;
	using UnityEngine.EventSystems;
	using Mapbox.Unity.Map;

	public class CameraMovement : MonoBehaviour
	{
		[SerializeField]
		AbstractMap _map;

		[SerializeField]
		float _panSpeed = 20f;

		[SerializeField]
		float _zoomSpeed = 50f;

		[SerializeField]
		Camera _referenceCamera;

		Quaternion _originalRotation;
		Vector3 _origin;
		Vector3 _delta;
		bool _shouldDrag;

		void HandleTouch()
		{
			float zoomFactor = 0.0f;
			//pinch to zoom. 
			switch (Input.touchCount)
			{
				case 1:
					{
						HandleMouseAndKeyBoard();
					}
					break;
				case 2:
					{
						// Store both touches.
						Touch touchZero = Input.GetTouch(0);
						Touch touchOne = Input.GetTouch(1);

						// Find the position in the previous frame of each touch.
						Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
						Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

						// Find the magnitude of the vector (the distance) between the touches in each frame.
						float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
						float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

						// Find the difference in the distances between each frame.
						zoomFactor = 0.05f * (touchDeltaMag - prevTouchDeltaMag);
					}
					ZoomMapUsingTouchOrMouse(zoomFactor);
					break;
				default:
					break;
			}
		}

		void ZoomMapUsingTouchOrMouse(float zoomFactor)
		{
			var y = zoomFactor * _zoomSpeed;
			transform.localPosition += (transform.forward * y);
		}

		void HandleMouseAndKeyBoard()
		{
			//if (Input.GetMouseButton(0)/* && !EventSystem.current.IsPointerOverGameObject()*/)
			if (Input.GetMouseButton(0) && GetTopMostUIElement().layer != 7)
			{
				var mousePosition = Input.mousePosition;
				mousePosition.z = _referenceCamera.transform.localPosition.y;
				_delta = _referenceCamera.ScreenToWorldPoint(mousePosition) - _referenceCamera.transform.localPosition;
				_delta.y = 0f;
				if (_shouldDrag == false)
				{
					_shouldDrag = true;
					_origin = _referenceCamera.ScreenToWorldPoint(mousePosition);
				}
			}
			else
			{
				_shouldDrag = false;
			}

			if (_shouldDrag == true)
			{
				var offset = _origin - _delta;
				offset.y = transform.localPosition.y;
				transform.localPosition = offset;
			}
			else
			{
				if (EventSystem.current.IsPointerOverGameObject())
				{
					var uiElement = GetTopMostUIElement();
					if(uiElement != null && uiElement.layer == 7)
						return;
				}
				
				var x = Input.GetAxis("Horizontal");
				var z = Input.GetAxis("Vertical");
				var y = Input.mouseScrollDelta.y * _zoomSpeed;
				//var y = Input.GetAxis("Mouse ScrollWheel") * _zoomSpeed;
				if (!(Mathf.Approximately(x, 0) && Mathf.Approximately(y, 0) && Mathf.Approximately(z, 0)))
				{
					transform.localPosition += transform.forward * y + (_originalRotation * new Vector3(x * _panSpeed, 0, z * _panSpeed));
					_map.UpdateMap();
				}
			}


		}

		GameObject GetTopMostUIElement()
		{
			// Get the current pointer event data
			PointerEventData pointerData = new PointerEventData(EventSystem.current)
			{
				// Set the pointer's position to the mouse or touch position
				position = Input.mousePosition // Use Input.touchPosition for touch input
			};

			// Raycast to find all GameObjects under the pointer
			var results = new List<RaycastResult>(); // You can adjust the size as needed
			EventSystem.current.RaycastAll(pointerData, results);

			// Check the topmost GameObject (if any)
			if (results.Count > 0)
				return results[0].gameObject;
			return null;
		}
		
		void Awake()
		{
			_originalRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

			if (_referenceCamera == null)
			{
				_referenceCamera = GetComponent<Camera>();
				if (_referenceCamera == null)
				{
					throw new System.Exception("You must have a reference camera assigned!");
				}
			}

			if (_map == null)
			{
				_map = FindObjectOfType<AbstractMap>();
				if (_map == null)
				{
					throw new System.Exception("You must have a reference map assigned!");
				}
			}
		}

		void LateUpdate()
		{
			//Debug.Log("Late update: " + Input.GetAxis("Mouse ScrollWheel"));

			if (Input.touchSupported && Input.touchCount > 0)
			{
				HandleTouch();
			}
			else
			{
				HandleMouseAndKeyBoard();
			}
		}
	}
}