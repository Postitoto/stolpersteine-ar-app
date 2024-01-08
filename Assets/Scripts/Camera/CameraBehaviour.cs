using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraBehaviour : MonoBehaviour
{
    public Transform player;

    public float zoomBuffer;
    public bool followPlayer = false;
    public bool rotateWithPlayer = false;

    [Range(0.1f, 1.0f)] 
    [SerializeField]
    private float animationTime = 0.1f;
    
    [Range(1, 100)]
    [SerializeField]
    private int animationFrameCount = 10;
    
    private Camera camera;
    private Vector3 playerPosition;

    private float deltaTime;
    private float dxCameraZoomChange;
    private float posXShift;
    private float posZShift;
    
    private float TOLERANCE = 0.1f;
    
    private void Awake()
    {
        camera = gameObject.GetComponent<Camera>();
        deltaTime = animationTime / animationFrameCount;
    }
    
    

    // Update is called once per frame
    void LateUpdate()
    {
        playerPosition = player.transform.position;
        
        // Following position
        if (followPlayer)
        {
            transform.position = new Vector3(playerPosition.x, transform.position.y, playerPosition.z);
        }
        
        // Rotate camera with player
        if (rotateWithPlayer)
        {
            transform.rotation = Quaternion.Euler(player.rotation.x, player.rotation.y, 0);
        }
    }

    public void FocusCameraOnPlayer()
    {
        var targetPosition = new Vector3(playerPosition.x, transform.position.y, playerPosition.z);
        StartCoroutine(MovingToTargetPosition(targetPosition));
    }
    
    public void FocusOnTargetPoints(Transform p1, Transform p2)
    {
        followPlayer = false;
        rotateWithPlayer = false;
        
        // Changing the size changes the level of zoom
        // The higher the size the more zoomed out it looks
        // To understand why the calculations are done this way:
        // Camera FOV height = 2f * camera.orthographicSize;
        // Camera FOV width = height * camera.aspect;
        float targetSize = 0f;
        var xDiff = Math.Abs(p1.position.x - p2.position.x);
        var zDiff = Math.Abs(p1.position.z - p2.position.z);
        if (xDiff > zDiff)
        {
            var w = xDiff + zoomBuffer;
            var h = w / camera.aspect;
            targetSize = h / 2;
        }
        else
        {
            var h = zDiff + zoomBuffer;
            targetSize = h / 2;
        }
        
        // Moves the camera to the exact position between p1 and p2
        var x = (p1.position.x + p2.position.x) / 2;
        var z = (p1.position.z + p2.position.z) / 2;
        var targetPosition = new Vector3(x, transform.position.y, z);
        
        StartCoroutine(Zooming(targetSize));
        StartCoroutine(MovingToTargetPosition(targetPosition));
    }
    
    private IEnumerator MovingToTargetPosition(Vector3 targetPosition)
    {
        SetPositionDeltaValues(transform.position, targetPosition);
        
        while (Vector3.Distance(transform.position, targetPosition) > TOLERANCE)
        {
            // Position
            var newPos = transform.position;
            newPos = new Vector3(newPos.x + posXShift, newPos.y, newPos.z + posZShift);
            transform.position = newPos;
            
            // transform.position = Vector3.MoveTowards(transform.position, targetPosition,dist / 10);
            // targetPosition = new Vector3(playerPosition.x, transform.position.y, playerPosition.z);
            // dist = Vector3.Distance(transform.position, targetPosition);
            yield return new WaitForSeconds(deltaTime);
        }
    }
    
    private IEnumerator Zooming(float targetSize)
    {
        SetZoomDeltaValues(targetSize);
        
        while (Math.Abs(camera.orthographicSize - targetSize) > TOLERANCE)
        {
            camera.orthographicSize += dxCameraZoomChange;
            yield return new WaitForSeconds(deltaTime);
        }
    }
    
    private void SetZoomDeltaValues(float targetSize)
    {
        deltaTime = animationTime / animationFrameCount;
        dxCameraZoomChange = (targetSize - camera.orthographicSize) / animationFrameCount;
    }

    private void SetPositionDeltaValues(Vector3 startPosition, Vector3 endPosition)
    {
        deltaTime = animationTime / animationFrameCount;
        posXShift = (endPosition.x - startPosition.x) / animationFrameCount;
        posZShift = (endPosition.z - startPosition.z) / animationFrameCount;
    }
}