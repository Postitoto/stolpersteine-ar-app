using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using SimpleJSON;
using TMPro;


// Handles localization, instantiation and destruction of the Stolperstein scene
// Main part of the Stolperstein Area Handler

public class StolpersteinLocationHandler : MonoBehaviour
{
    private enum LocationMethod { Touch, ObjectDetection }

    // Associated Components
    [SerializeField]
    private ARSessionOrigin _aRSessionOrigin;
    [SerializeField]
    private BackendInterface _backendInterface;
    [SerializeField]
    private StolpersteinSceneConstructor _sceneConstructor;
    [SerializeField]
    private ARCameraInputDetection _detector;

    // Prefabs/Game Objects
    [SerializeField]
    private GameObject _localizingIndicatorPrefab;
    [SerializeField]
    private Button _startLocalizingButton;
    [SerializeField]
    private GameObject _anchorPrefab;
    [SerializeField]
    private Button _backToSelectionButton;
    [SerializeField]
    private Button _toggleDetectionMethodButton;
    [SerializeField]
    private GameObject _locationNameObj;
    [SerializeField]
    private GameObject _detectingLabel;

    [SerializeField] private TextMeshProUGUI debugText;
    
    // Events
    [SerializeField]
    private GameObjectEvent _sceneCreated;
    [SerializeField]
    private UnityEvent _sceneDeleted;

    // Settings
    [SerializeField]
    private LocationMethod _locationMethod = LocationMethod.Touch;
    [SerializeField]
    private float _detectionInterval = 0.3f;

    // Private variables
    private enum State
    {
        Idle,
        Ready,
        Localizing,
        Selection,
        Created,
        None // TODO: remove, debug state that will never be set
    };
    private State _state;
    private State _lastState = State.None; // TODO: remove
    private List<GameObject> _localizationVisualizers;
    private GameObject _currentVisualizer; 
    private List<JSONNode> _stolpersteineContents; 
    private List<ARAnchor> _stolpersteinAnchors;
    private List<GameObject> _anchorVisualizers;
    private GameObject _instantiatedScene;
    private DateTime _lastDetection;
    private List<(ARPlane, Pose)> _touchLocatedStones;
    private Transform _cam;

    private ARPlaneManager _planeManager;
    private ARRaycastManager _raycastManager;
    private ARAnchorManager _anchorManager;
    private static List<ARRaycastHit> _raycastHits = new List<ARRaycastHit>();


    void Awake()
    {
        _planeManager = _aRSessionOrigin.GetComponent<ARPlaneManager>();
        _raycastManager = _aRSessionOrigin.GetComponent<ARRaycastManager>();
        _anchorManager = _aRSessionOrigin.GetComponent<ARAnchorManager>();
        _backToSelectionButton.gameObject.SetActive(false);
        _toggleDetectionMethodButton.gameObject.SetActive(true);
        SetLocationMethodText(_toggleDetectionMethodButton, _locationMethod);
        _cam = Camera.main.transform;
    }

    void OnEnable()
    {
        _backToSelectionButton.onClick.AddListener(BackToSelection);
        _startLocalizingButton.onClick.AddListener(TriggerLocalizationState);
        _toggleDetectionMethodButton.onClick.AddListener(ToggleDetectionMethod);
    }

    // State machine, handling the current active state
    void Update()
    {
        if (_state != _lastState)
        {
            Debug.Log("Current State: " + _state);
        }
        _lastState = _state;
        switch (_state)
        {
            case State.Idle:
                // activate location info
                _locationNameObj.SetActive(true);
                break;
            case State.Ready:
                // Activate detection method toggle button
                _toggleDetectionMethodButton.gameObject.SetActive(true);
                // Wait for user to click the _startLocalizingButton to trigger next state
                break;

            case State.Localizing:
                _toggleDetectionMethodButton.gameObject.SetActive(true);
                // Wait until the content has been loaded
                if (_stolpersteineContents == null)
                {
                    return;
                }

                // Localize the Stolperstein(e) by the chosen method
                List<(ARPlane, Pose)> locatedStolpersteine;
                switch (_locationMethod)
                {
                    case LocationMethod.Touch:
                        _detectingLabel.SetActive(false);
                        locatedStolpersteine = LocateByTouch();
                        break;
                    case LocationMethod.ObjectDetection:
                        _detectingLabel.SetActive(true);
                        locatedStolpersteine = LocateByObjectDetection();
                        break;
                    default:
                        _detectingLabel.SetActive(false);
                        throw new System.Exception("unsupported location method: " + _locationMethod);
                }

                // Continue, if detection was successfull
                if (locatedStolpersteine != null)
                {
                    _detectingLabel.SetActive(false);

                    // Sort the located stolpersteine to assign correct scenes to each stone
                    SortByPosition(locatedStolpersteine);
                    int i = 0;
                    foreach ((ARPlane hitPlane, Pose hitPose) in locatedStolpersteine)
                    {
                        // Attach an anchor at the found Stolperstein position,
                        // The rotation should match the camera rotation projected down on the plane
                        var anchorDirection = Vector3.ProjectOnPlane(Camera.main.transform.forward, hitPlane.normal);
                        Pose anchorPose = new Pose(hitPose.position, Quaternion.LookRotation(anchorDirection));
                        var currentAnchor = _anchorManager.AttachAnchor(hitPlane, anchorPose);

                        // Attach a selection GO to found stolperstein anchor
                        var anchorObject = Instantiate(_anchorPrefab);
                        var infoBehavior = anchorObject.GetComponent<InfoBehavior>();
                        if(infoBehavior != null)
                        {
                            infoBehavior.SetInfoText(_stolpersteineContents[i]["name"]);
                        }
                        anchorObject.transform.SetParent(currentAnchor.transform, false);
                        _anchorVisualizers.Add(anchorObject);

                        _stolpersteinAnchors.Add(currentAnchor);

                        if (currentAnchor == null)
                        {
                            Debug.Log("Error creating anchor.");
                        }
                        i++;
                    }
                    Debug.Assert(_stolpersteinAnchors.Count == _stolpersteineContents.Count);

                    // Turn off plane detection to save resources & don't show plane prefab
                    SetPlaneManagerActive(false);
                    _state = State.Selection;
                }
                break;

            case State.Selection:
                _locationNameObj.SetActive(false);
                _toggleDetectionMethodButton.gameObject.SetActive(false);
                // Assure that no AR content remains
                TearDownScene();

                // Show the GOs for the Stolperstein selection, they are the first child since first added
                foreach (ARAnchor stolperstein in _stolpersteinAnchors)
                {
                    stolperstein.transform.GetChild(0).gameObject.SetActive(true);
                }

                // If there is only one Stolperstein, directly choose it
                ARAnchor anchor;
                if (_stolpersteineContents.Count == 1)
                {
                    anchor = _stolpersteinAnchors[0];
                }
                else
                {
                    anchor = ChooseSceneByTouch();
                }

                // If a stone was chosen
                if (anchor != null)
                {
                    // Offer the possibility to select another stone
                    if (_stolpersteineContents.Count != 1)
                    {
                        _backToSelectionButton.gameObject.SetActive(true);
                    }

                    // Deactivate selection objects
                    foreach (ARAnchor stolperstein in _stolpersteinAnchors)
                    {
                        stolperstein.transform.GetChild(0).gameObject.SetActive(false);
                    }

                    // Create the selected scene and fire event
                    int idx = _stolpersteinAnchors.IndexOf(anchor);
                    Debug.Log("Contructing Scene with" +_stolpersteineContents[idx]);
                    _instantiatedScene = _sceneConstructor.ConstructScene(
                        _stolpersteineContents[idx], _backendInterface.Images, _backendInterface.Audios);
                    _instantiatedScene.transform.SetParent(anchor.transform, false);
                    _sceneCreated.Invoke(_instantiatedScene);
                    Debug.Log("Finished Scene Construction!");
                    _state = State.Created;
                }
                break;

            default:
                // Do nothing
                break;
        }
    }

    private void TriggerLocalizationState()
    {
        _state = State.Localizing;
        _startLocalizingButton.gameObject.SetActive(false);
    }

    /// <summary>
    ///     Choose a stolperstein by touching on the representing gameobject.
    /// </summary>
    private ARAnchor ChooseSceneByTouch()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hitObject;
                if (Physics.Raycast(ray, out hitObject))
                {
                    // The gameobject is parented to the anchor
                    var anchor = hitObject.collider.transform.parent.GetComponent<ARAnchor>();
                    if (anchor != null)
                    {
                        return anchor;
                    }
                    Debug.Log("No anchor hit by raycast");
                }
            }
        }
        return null;
    }

    /// <summary>
    ///     Plays or pauses an audio if a gameobject with audio source was touched
    /// </summary>
    private void PlayByTouch()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hitObject;
                if (Physics.Raycast(ray, out hitObject))
                {
                        
                    var source = hitObject.collider.transform.parent.GetComponent<AudioSource>();
                    if (source != null)
                    {
                        if (source.isPlaying)
                        {
                            Debug.Log("Touchdetection: Audio Stopped");
                            source.Stop();
                        }
                        else
                        {
                            Debug.Log("Touchdetection: Audio Played");
                            source.Play();
                        }
                    }  
                    else
                    {
                        Debug.Log("Touchdetection: Audio Source not found");
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Initializes values to be ready to Create a Stolperstein scene for the given coordinates
    /// </summary>
    /// <param name="coordinates"> Coordinates of the stolperstein location</param>
    public void StartSceneCreation(string coordinates)
    {
        StartCoroutine(_backendInterface.GetStolpersteineAt(coordinates, (List<JSONNode> stolpersteineJson) =>
            {
                _stolpersteineContents = stolpersteineJson;
            }));
        _state = State.Ready;
        _touchLocatedStones = new List<(ARPlane, Pose)>();
        _localizationVisualizers = new List<GameObject>();
        _stolpersteinAnchors = new List<ARAnchor>();
        _anchorVisualizers = new List<GameObject>();
        SetTrackableManagersActive(true);
        _startLocalizingButton.gameObject.SetActive(true);
    }

    /// <summary>
    ///     Destructs the current active Stolperstein Scene and frees resources
    /// </summary>
    public void TearDownScene()
    {
        if (GameObject.FindGameObjectWithTag("MainCamera").gameObject.activeInHierarchy)
        {
            GameObject.FindGameObjectWithTag("MainCamera").SetActive(true);
        }
        _backToSelectionButton.gameObject.SetActive(false);
        Destroy(_instantiatedScene);
        _sceneDeleted.Invoke();
    }


    /// <summary>
    ///     Frees all resources and returns to Idle state
    /// </summary>
    public void StopSceneCreation()
    {
        // Return to idle state
        _state = State.Idle;

        // Free AR Manager resources
        foreach (ARAnchor anchor in _anchorManager.trackables)
        {
            _anchorManager.RemoveAnchor(anchor);
        }
        _stolpersteinAnchors = null;
        SetTrackableManagersActive(false);

        // Dereference stolperstein contents and destroy the instantiated scene
        _stolpersteineContents = null;
        _touchLocatedStones = null;
        if (_anchorVisualizers != null)
        {
            foreach (GameObject visualizer in _anchorVisualizers)
            {
                Destroy(visualizer);
            }
            _anchorVisualizers = null;
        }
        if (_localizationVisualizers != null)
        {
            foreach (GameObject locationVisualizer in _localizationVisualizers)
            {
                Destroy(locationVisualizer);
            }
        }
        _localizationVisualizers = null;
        _currentVisualizer = null;
        _startLocalizingButton.gameObject.SetActive(false);

        // Clean up any remaining AR content
        TearDownScene();
    }

    /// <summary>
    ///     Returns to Selection state
    /// </summary>
    void BackToSelection()
    {
        if (_state != State.Created)
        {
            Debug.Log("Return to selection button should not be clickable in state " + _state);
            return;
        }
        TearDownScene();
        _state = State.Selection;
    }

    /// <summary>
    ///     Switches between stolperstein detection methods
    /// </summary>
    void ToggleDetectionMethod()
    {
        switch (_locationMethod)
        {
            case LocationMethod.Touch:
                _locationMethod = LocationMethod.ObjectDetection;
                break;
            case LocationMethod.ObjectDetection:
                _locationMethod = LocationMethod.Touch;
                break;
            default:
                break;
                
        }
        SetLocationMethodText(_toggleDetectionMethodButton, _locationMethod);
    }
    /// <summary>
    ///     Locates the Stolperstein in World space using user touch input
    ///     Can be used e.g. for debugging
    /// </summary>
    /// <returns> Returns the ARPlane and Pose at which the stolperstein was located </returns>
    private List<(ARPlane, Pose)> LocateByTouch()
    {
        if (Input.touchCount == 0)
        {
            Log("No touch");
            return null;
        }

        Touch touch = Input.GetTouch(index: 0);
        var locationOnScreen = touch.position;
        Log("Touch at: " + locationOnScreen);

        if (_raycastManager.Raycast(locationOnScreen, _raycastHits, TrackableType.PlaneWithinPolygon))
        {
            Log("Found a target!");
            
            // Raycast hits are sorted by distance, so the first one will be the closest hit.
            var hitPose = _raycastHits[0].pose;
            var hitTrackableId = _raycastHits[0].trackableId;
            var hitPlane = _planeManager.GetPlane(hitTrackableId);

            // Check if localization is finished (not touching screen anymore when localizing with touch)
            if (Input.touchCount > 0 && Input.GetTouch(index: 0).phase == TouchPhase.Ended)
            {
                _currentVisualizer = null;
                _touchLocatedStones.Add((hitPlane, hitPose));
                if (_touchLocatedStones.Count == _stolpersteineContents.Count)
                {
                    foreach (GameObject visualizer in _localizationVisualizers)
                    {
                        Destroy(visualizer);
                    }
                    return _touchLocatedStones;
                }
            }
            else
            {
                // Show the currently localized position in AR space
                if (_currentVisualizer == null && _localizingIndicatorPrefab != null)
                {
                    _currentVisualizer = Instantiate(_localizingIndicatorPrefab, hitPose.position, hitPose.rotation);
                    _localizationVisualizers.Add(_currentVisualizer);
                }
                else
                {
                    _currentVisualizer.transform.position = hitPose.position;
                    _currentVisualizer.transform.rotation = hitPose.rotation;
                }
            }
        }
        return null;
    }

    /// <summary>
    ///     Locates the Stolperstein in World space using object detection
    /// </summary>
    /// <returns> Returns the ARPlane and Pose at which the stolperstein was located </returns>
    private List<(ARPlane, Pose)> LocateByObjectDetection()
    {
        if (_detector == null)
        {
            throw new System.Exception("no object detector provided");
        }

        if ((DateTime.Now - _lastDetection).TotalSeconds < _detectionInterval)
        {
            return null;
        }
        _lastDetection = DateTime.Now;

        // Execute detection
        IList<BoundingBox> bboxes = _detector.DetectOnLatestFrame();
        if (bboxes == null)
        {
            return null;
        }
        
        Log("boxes count = " + bboxes.Count + ", Stolperstein count: " + _stolpersteineContents.Count);
        if (bboxes.Count != _stolpersteineContents.Count)
        {
            return null;
        }
        
        // Map the found bounding boxes to 3D positions
        var locatedStolpersteine = new List<(ARPlane, Pose)>();
        foreach (BoundingBox box in bboxes)
        {
            var position = new Vector2(box.Dimensions.X, Screen.height - box.Dimensions.Y);
            if (_raycastManager.Raycast(position, _raycastHits, TrackableType.PlaneWithinPolygon))
            {
                // Map the Stolperstein found in the 2D image position into the 3D by raycasting from the screen position
                // The stolperstein is located at the position, where the raycast hits the detected ground plane
                // Raycast hits are sorted by distance, so the first one will be the closest hit.
                var hitPose = _raycastHits[0].pose;
                var hitTrackableId = _raycastHits[0].trackableId;
                var hitPlane = _planeManager.GetPlane(hitTrackableId);

                // Rotate hitpose, such that it faces in the forward direction
                Vector3 forward = new Vector3(_cam.forward.x, 0, _cam.forward.z);
                hitPose.rotation = Quaternion.LookRotation(forward, hitPlane.normal);

                locatedStolpersteine.Add((hitPlane, hitPose));
            }
            else
            {
                // If a predicted stolperstein doesnt fall on a ground plane, reject whole prediction
                return null;
            }
        }

        // Only accept detection if all sanity checks are passed
        if (PassesSanityChecks(locatedStolpersteine))
        {
            return locatedStolpersteine;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    ///     Perform sanity checks for the predicted stolpersteine
    /// </summary>
    /// <param name="detectedStolpersteine"> List of proposed Stolperstein detections</param>
    /// <returns> true, if all sanity checks are passed, false otherwise </returns>
    private bool PassesSanityChecks(List<(ARPlane, Pose)> detectedStolpersteine)
    {
        Debug.Assert(detectedStolpersteine != null && detectedStolpersteine.Count >= 1);
        if (detectedStolpersteine.Count > 1)
        {
            // A nested loop is reasonable since the number of stolpersteine usually is low one digit number
            for (int i = 0; i < detectedStolpersteine.Count; i++)
            {
                float minDist = float.PositiveInfinity;
                for (int j = 0; j < detectedStolpersteine.Count; j++)
                {
                    (ARPlane plane1, Pose pose1) = detectedStolpersteine[i];
                    (ARPlane plane2, Pose pose2) = detectedStolpersteine[j];

                    // Check if all stones lie on the same ground plane
                    if (plane1 != plane2)
                    {
                        Debug.Log("Located Stolpersteine lie on different ground planes");
                        return false;
                    }

                    float distance = Vector3.Distance(pose1.position, pose2.position);
                    minDist = i != j && distance < minDist ? distance : minDist;
                }

                // Check if the minimum distance between the stolpersteine is reasonable
                if (minDist > 0.3)
                {
                    Debug.Log("Located Stolpersteine are at least : " + minDist + "m away from each other");
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    ///     Sorts the List of found Stolpersteine from top to button and left to right relative to camera
    /// </summary>
    /// <param name="stolpersteine"> List of detected stolpersteine to sort</param>
    private void SortByPosition(List<(ARPlane, Pose)> stolpersteine)
    {
        // Planes are all the same for each stolperstein detection, so choose from any entry
        (ARPlane plane, Pose pose) = stolpersteine[0];

        // In order to sort the stolperstein by their position relative to the user, 
        // view their arrangement on the 2D ground plane
        Transform coordSys = new GameObject().transform;
        // Origin is point on ground plane on which normal that goes through cam lies
        coordSys.position = new Vector3(_cam.position.x, pose.position.y, _cam.position.z);
        // Forward (z) direction of is x and z component of the camera facing direction
        Vector3 forward = new Vector3(_cam.forward.x, 0, _cam.transform.forward.z);
        coordSys.rotation = Quaternion.LookRotation(forward, plane.normal);

        // Comparison method, that determines the order, based on which stone is further back and further left
        // Including some tolerance, if user is positioned slightely angeled towards them
        int CompareStolpersteine((ARPlane, Pose) a, (ARPlane, Pose) b)
        {
            (ARPlane _, Pose p1) = a;
            (ARPlane _, Pose p2) = b;
            // View the Stolperstein coordinates from the new coordinate system
            float x1 = coordSys.InverseTransformPoint(p1.position).x;
            float x2 = coordSys.InverseTransformPoint(p2.position).x;
            float z1 = coordSys.InverseTransformPoint(p1.position).z;
            float z2 = coordSys.InverseTransformPoint(p2.position).z;

            if (x1 == x2 && z1 == z2)
            {
                Debug.Log("Comparsion of Stolperstein positions returned 0. Probably stone compared with itself");
                return 0;
            }

            // Check if the stones lie in the same row, by comparing z-difference
            if (Mathf.Abs(z1 - z2) > 0.08)
            {
                // For different rows, stones further to the back come first
                return z1 > z2 ? -1 : 1;
            }
            else
            {
                // For the same row, stones further left come first
                return x1 < x2 ? -1 : 1;
            }
        }

        // Finally sort the Stolpersteine List based on the comparison method
        stolpersteine.Sort(CompareStolpersteine);
    }

    /// <summary>
    ///     Enable or disable trackable managers (to save resources and not display plane prefab when not needed)
    /// </summary>
    /// <param name="value"> true for activation, false for deactivation</param>
    private void SetTrackableManagersActive(bool value)
    {
        SetPlaneManagerActive(value);
        SetRayCastManagerActive(value);
        SetAnchorManagerActive(value);
    }

    private void SetPlaneManagerActive(bool value)
    {
        _planeManager.enabled = value;
        foreach (ARPlane plane in _planeManager.trackables)
        {
            plane.gameObject.SetActive(value);
        }
    }

    private void SetRayCastManagerActive(bool value)
    {
        _raycastManager.enabled = value;
        foreach (ARRaycast raycast in _raycastManager.trackables)
        {
            raycast.gameObject.SetActive(value);
        }
    }

    private void SetAnchorManagerActive(bool value)
    {
        _anchorManager.enabled = value;
        foreach (ARAnchor anchor in _anchorManager.trackables)
        {
            anchor.gameObject.SetActive(value);
        }
    }

    private void SetLocationMethodText(Button methodButton, LocationMethod locationMethod)
    {
        switch (locationMethod)
        {
            case LocationMethod.Touch:
                methodButton.gameObject.GetComponentsInChildren<TextMeshProUGUI>(true)[0].text = "Touch";
                break;
            case LocationMethod.ObjectDetection:
                methodButton.gameObject.GetComponentsInChildren<TextMeshProUGUI>(true)[0].text = "Camera Detection";
                break;
            default:
                methodButton.gameObject.GetComponentsInChildren<TextMeshProUGUI>(true)[0].text = "Unknown Method";
                break;
        }
    }

    private void Log(string message)
    {
        debugText.text = message;
    }
}