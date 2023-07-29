using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SimpleJSON;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using Mapbox.CheapRulerCs;

public enum SceneType
{
    Info,
    LifeStations,
    Family,
    RelatedStolpersteine,
    AudioPlayer,
    AdditionalInfo
}

public class StolpersteinSceneConstructor : MonoBehaviour
{
    // Object Prefabs
    [SerializeField]
    private GameObject _infoTextHolder;
    [SerializeField]
    private GameObject _infoScenePrefab;
    [SerializeField]
    private GameObject _infoAdditionalPrefab;
    [SerializeField]
    private GameObject _familyScenePrefab;
    [SerializeField]
    private GameObject _lifeStationPrefab;
    [SerializeField]
    private GameObject _relatedStolpersteineScenePrefab;
    [SerializeField]
    private GameObject _lifeStationMarkerPrefab;
    [SerializeField]
    private GameObject _relatedStolpersteinMarkerPrefab;
    [SerializeField]
    private AbstractMap _mapCityPrefab;
    [SerializeField]
    private AbstractMap _mapWorldPrefab;
    [SerializeField]
    private GameObject _person3DInfoPrefab;
    [SerializeField]
    private GameObject _currentPositionMarker;
    [SerializeField]
    private GameObject _audioPlayerPrefab;
    [SerializeField]
    private GameObject _simpleTextButton;
    [SerializeField]
    private GameObject _audioPlayerControlsPrefab;
    [SerializeField]
    private Button _backToSelectionButton;

    private LocationHandler _locationHandler;
    private const string _dateFormat = "dd.MM.yyyy";

    void Awake()
    {
        GameObject locHandler = GameObject.Find("Location Handler");
        _locationHandler = locHandler != null ? locHandler.GetComponent<LocationHandler>() : null;
    }

    /// <summary>
    ///     Creates a Stolperstein scene from the given content.
    ///     The Scene is made up of several subscenes, that can be interactively switched.
    /// </summary>
    /// <param name="contentJson"> json containing the content mapping</param>
    /// <param name="images"> Dictionary holding images for the keys defined in contentJson</param>
    public GameObject ConstructScene(JSONNode content, Dictionary<string, Texture> images, Dictionary<string, AudioClip> audios)
    {
        
        Dictionary<SceneType, GameObject> scenes = new Dictionary<SceneType, GameObject>();

        GameObject root = new GameObject(content["name"] + "Scene");

        // Info Scene
        string infoText = content["info_text"];
        if (infoText != null)
        {
            GameObject infoScene = ConstructTextScene(infoText, "Info");
            scenes.Add(SceneType.Info, infoScene);
        }

        // Life Stations Scene
        if (content["life_stations"].Count > 0)
        {
            List<(string, GameObject)> coordinates = new List<(string, GameObject)>();
            foreach (JSONNode life_station in content["life_stations"])
            {
                var coords = life_station["coordinates"];
                var lsObject = Instantiate(_lifeStationMarkerPrefab);
                lsObject.transform.SetParent(root.transform);
                lsObject.SetActive(false);
                // Add clickable component to lsObject to make it interactable
                var lsTextScene = ConstructTextScene(life_station["text"], life_station["name"]);
                lsTextScene.transform.SetParent(root.transform);
                var clickable = lsObject.AddComponent<Clickable>();
                lsTextScene.SetActive(false);
                clickable.sceneOnClick = lsTextScene;
                coordinates.Add((coords, lsObject));
            }

            GameObject lifestationsScene = ConstructMapScene(coordinates);
            scenes.Add(SceneType.LifeStations, lifestationsScene);
        }

        // Family Scene
        string familyText = content["family_text"];
        if (familyText != null)
        {
            GameObject familyScene = ConstructTextScene(familyText, "Family");
            scenes.Add(SceneType.Family, familyScene);
        }

        // Related Stolpersteine Scene
        if (content["stolperstein_relations"].Count > 0)
        {
            List<(string, GameObject)> relatedStolpersteine = new List<(string, GameObject)>();
            foreach (JSONNode relation in content["stolperstein_relations"])
            {
                var stolperstein = relation["related_stolperstein"];
                var location = stolperstein["location"];
                var coords = location["coordinates"];
                var relationObject = Instantiate(_relatedStolpersteinMarkerPrefab);
                var infoBehavior = relationObject.GetComponent<InfoBehavior>();
                if (infoBehavior != null)
                {
                    infoBehavior.SetInfoText(stolperstein["name"]);
                }
                relationObject.transform.SetParent(root.transform);
                relationObject.SetActive(false);
                // Add clickable component to relationObject to make it interactable
                var location_name = location["name"];
                var heading = stolperstein["name"] + (location_name != "" ? "\n(" + location_name + ")" : "");
                var relationTextScene = ConstructTextScene(relation["text"], heading);
                relationTextScene.transform.SetParent(root.transform);
                var clickable = relationObject.AddComponent<Clickable>();
                relationTextScene.SetActive(false);
                clickable.sceneOnClick = relationTextScene;
                relatedStolpersteine.Add((coords, relationObject));
            }
            // Add indicator for current position
            if (_locationHandler != null && _locationHandler.ActiveLocation != null)
            {
                GameObject positionMarker = Instantiate(_currentPositionMarker);
                positionMarker.transform.SetParent(root.transform);
                positionMarker.SetActive(false);
                relatedStolpersteine.Add((_locationHandler.ActiveLocation, positionMarker));
            }

            GameObject relatedStolpersteineScene = ConstructMapScene(relatedStolpersteine);
            scenes.Add(SceneType.RelatedStolpersteine, relatedStolpersteineScene);
        }
        // Audio Player Scene
        var audioKey = content["files"]["audio"];    
        if (audioKey != null)
        {
            if (!audios.ContainsKey(audioKey))
            {
                Debug.Log("Expected audio " + audioKey + ". But nothing was found in the audio store");  
            }
            else
            {
                // load audio clip
                AudioClip audio = audios[audioKey];
                Debug.Log("Found audio clip " + audio);
                GameObject audioScene = CreateAudioPlayerScene(audio);
                scenes.Add(SceneType.AudioPlayer, audioScene);
                Debug.Log("Created Audio Player Scene; TODO");
            }
        }
        // TODO: Additional Information (Plus) Scene
        if (content["info_textboxes"].Count > 0)
        {
            var additionalInfoScene = CreateTextInfoSelectionScene(content["info_textboxes"], root.transform);
            scenes.Add(SceneType.AdditionalInfo, additionalInfoScene);
        }

        // Base Scene
        var photoKey = content["files"]["photo"];
        Texture photo = null;
        // load photo
        if (photoKey != null)
        {
            if (images.ContainsKey(photoKey))
            {
                photo = images[photoKey];
            }
            else
            {
                Debug.Log("photokey " + photoKey + " not found in image store");
            }
        }

        string dates = "";
        string birthdate = content["birthdate"] != null ? DateTime.Parse(content["birthdate"], CultureInfo.InvariantCulture).ToString(_dateFormat) : null;
        string deathdate = content["deathdate"] != null ? DateTime.Parse(content["deathdate"], CultureInfo.InvariantCulture).ToString(_dateFormat) : null;
        if (birthdate != null && deathdate != null)
        {
            dates = birthdate + " - " + deathdate;
        }
        else if (birthdate != null)
        {
            dates = "born " + birthdate;
        }
        else if (deathdate != null)
        {
            dates = "died " + deathdate;
        }
        GameObject baseScene = ConstructBaseScene(photo, content["name"], dates);
        baseScene.transform.SetParent(root.transform);

        // Selection scene
        GameObject selectionScene = ConstructSelectionScene(scenes);
        selectionScene.transform.SetParent(root.transform);
        selectionScene.transform.SetAsFirstSibling();

        // Parent the created scenes to the root scene game object and set inactive
        foreach (GameObject scene in scenes.Values)
        {
            scene.transform.SetParent(root.transform);
            scene.SetActive(false);
        }

        return root;
    }

    private GameObject CreateTextInfoSelectionScene(JSONNode infoTexts, Transform rootTransform)
    {
        GameObject additionalInfosTextScene = new GameObject("Infos Selection Scene");
        // UserInteraction for handling button clicks
        var userInteraction = FindObjectOfType<UserInteraction>();
        // Preparing Canvas
        GameObject canvas = new GameObject("canvasInfoSelection");
        var canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        canvas.transform.SetParent(additionalInfosTextScene.transform);
        // Instantiate Selection Buttons
        int numberOfButtons = 0;
        foreach (JSONNode text in infoTexts)
        {
            GameObject button;
            if (numberOfButtons > 0)
            {
                button = Instantiate(_simpleTextButton, canvas.transform, false);
                button.transform.Translate(new Vector3(0, -200, 0) * numberOfButtons);
                numberOfButtons++;
            }
            else
            {
                button = Instantiate(_simpleTextButton, canvas.transform, false);
                numberOfButtons++;
            }
            button.GetComponentInChildren<Text>(true).text = text["title"];
            button.AddComponent<Button>();
            GameObject textScene = ConstructTextScene(text["content"], text["title"]);
            textScene.transform.SetParent(rootTransform);
            textScene.SetActive(false);
            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (userInteraction != null)
                {
                    userInteraction.LoadSceneOnButtonClick(textScene);
                }
            });
        }
        
        return additionalInfosTextScene;
    }

    private GameObject CreateAudioPlayerScene(AudioClip audio)
    {
        GameObject audioPlayerScene = new GameObject("2D Audio Player " + audio.name + " Scene");
        GameObject canvas = new GameObject("canvas");
        var canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        var canvasScaler = canvas.AddComponent<CanvasScaler>();
        canvasScaler.referenceResolution = new Vector2(1280, 800);
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvas.AddComponent<GraphicRaycaster>();
        canvas.transform.SetParent(audioPlayerScene.transform);

        Instantiate(_audioPlayerControlsPrefab, canvas.transform, false);
        var blAudioPlayer = FindObjectOfType<bl_AudioPlayer>();
        blAudioPlayer.m_Clip.Clear();
        blAudioPlayer.NewClip(audio, true);
        blAudioPlayer.m_Clip.Add(audio);
        return audioPlayerScene;
    }

    /// <summary>
    ///     Constructs a stolperstein info scene (Displaying the info Text on Screen)
    /// </summary>
    /// <param name="text"> The text to be displayed</param>
    private GameObject ConstructTextScene(string text, string heading)
    {
        GameObject textScene = new GameObject("2D " + heading + " Scene");

        // Preparing Canvas
        GameObject canvas = new GameObject("canvas");
        var canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        canvas.transform.SetParent(textScene.transform);

        // Initializing Text
        GameObject infoTextGO = Instantiate(_infoTextHolder, canvas.transform, false);
        Text textComponent = infoTextGO.GetComponentsInChildren<Text>(true)[0];
        Text headingComponent = infoTextGO.GetComponentsInChildren<Text>(true)[1];
        textComponent.text = text;
        headingComponent.text = heading;

        return textScene;
    }

    /// <summary>
    ///     Constructs a stolperstein map scene.
    ///     This is a 3D Map on the ground with (possibly interactive) GameObjects placed at coordinates.
    /// </summary>
    /// <param name="location"> Key Value Pair of Coordinates and GameObject to show at the coordinates</param>
    private GameObject ConstructMapScene(List<(string, GameObject)> locations)
    {
        GameObject mapScene = new GameObject("MapScene");
        AbstractMap map;

        // Compute the outermost locations in order to determine center and zoom level of the map
        IEnumerable<float> lat = from loc in locations select float.Parse(loc.Item1.Split(',')[0], CultureInfo.InvariantCulture);
        IEnumerable<float> lon = from loc in locations select float.Parse(loc.Item1.Split(',')[1], CultureInfo.InvariantCulture);
        List<(float, float)> coordinates = lat.Zip(lon, (a, b) => (a, b)).ToList();
        (float latMid, float lonMid) = CalculateCenterCoordinates(coordinates);

        // Set the number of tiles in width an height
        int tilesWidth = 3;
        int tilesHeight = 3;

        // The zoom level is calculated, such that all locations are inside the visible area
        int zoom = (int) CalculateZoomLevel(latMid, lat, lon, tilesWidth, tilesHeight);
        _mapCityPrefab.InitializeOnStart = false;

        // Display map differently depending on the level of zoom
        Debug.Log("zoom: " + zoom);
        if (zoom >= 13)
        {
            // "City View"
            map = Instantiate(_mapCityPrefab);
        }
        else
        {
            // "World View"
            map = Instantiate(_mapWorldPrefab);
            // Dynamic exaggeration factor for terrain
            var terrain = map.Terrain;
            float exaggerationFactor = (13 - zoom) * 2;
            terrain.SetExaggerationFactor(exaggerationFactor);
        }

        // Make sure map extent is based on range around center
        MapOptions options = map.Options;
        options.extentOptions.extentType = MapExtentType.RangeAroundCenter;
        var rangeExtentOptions = options.extentOptions.defaultExtents.rangeAroundCenterOptions;
        Debug.Assert(tilesHeight % 2 == 1 && tilesWidth % 2 == 1);
        int extentNorthSouth = tilesHeight / 2;
        int extentEastWest = tilesWidth / 2;
        rangeExtentOptions.SetOptions(extentNorthSouth, extentNorthSouth, extentEastWest, extentEastWest);

        // Add script for creation of the GOs at the given coordinates
        var spawnScript = map.gameObject.AddComponent<SpawnOnMapAsChild>();
        spawnScript.ObjectsToSpawn = locations;
        spawnScript.Map = map;
        spawnScript.SpawnScale = 0.2f;

        // Position the map relative to Stolperstein Position and Initialize
        map.transform.position = new Vector3(0f, 0f, 1f);
        map.transform.SetParent(mapScene.transform);
        map.Initialize(new Vector2d(latMid, lonMid), zoom);

        return mapScene;
    }

    /// <summary>
    ///     Calculates the Center of a List of geocoordinates.
    ///     Where Center is the Center of the minimal "rectangle" that includes all points
    /// </summary>
    /// <param name="coordinates"> A List of the coordinates</param>
    private (float, float) CalculateCenterCoordinates(
    // Adapted from https://stackoverflow.com/questions/6671183/calculate-the-center-point-of-multiple-latitude-longitude-coordinate-pairs

        List<(float, float)> coordinates)
    {
        if (coordinates.Count == 1)
        {
            return coordinates[0];
        }

        float x = 0;
        float y = 0;
        float z = 0;

        foreach (var coordinate in coordinates)
        {
            var latitude = coordinate.Item1 * Mathf.PI / 180;
            var longitude = coordinate.Item2 * Mathf.PI / 180;

            x += Mathf.Cos(latitude) * Mathf.Cos(longitude);
            y += Mathf.Cos(latitude) * Mathf.Sin(longitude);
            z += Mathf.Sin(latitude);
        }

        var total = coordinates.Count;

        x = x / total;
        y = y / total;
        z = z / total;

        var centralLongitude = Mathf.Atan2(y, x);
        var centralSquareRoot = Mathf.Sqrt(x * x + y * y);
        var centralLatitude = Mathf.Atan2(z, centralSquareRoot);

        return (centralLatitude * 180 / Mathf.PI, centralLongitude * 180 / Mathf.PI);
    }

    /// <summary>
    ///     Calculates the Zoom Level for the map, such that all coordinates are included.
    /// </summary>
    /// <param name="latCenter"> Latitude of the center coordinate</param>
    /// <param name="lat"> Latitudes of the coordinates that have to be included</param>
    /// <param name="lon"> Longitudes of the coordinates that have to be included</param>
    /// <param name="tilesWidth"> Number of tiles that the map has in widht direction</param>
    /// <param name="tilesHeight"> Number of tiles that the map has in height direction</param>
    private float CalculateZoomLevel(float latCenter, IEnumerable<float> lat, IEnumerable<float> lon, int tilesWidth, int tilesHeight)
    {
        Debug.Assert(lat.Count() == lon.Count());

        // If only one Element has to be in the map, choose close zoom level
        if (lat.Count() == 1)
        {
            return 17f;
        }

        // MapBox zoom levels: https://docs.mapbox.com/help/glossary/zoom-level/#zoom-levels-and-geographical-distance

        double[] locLatMin = new double[] { lat.Min(), lon.ToList()[lat.ToList().IndexOf(lat.Min())] };
        double[] locLatMax = new double[] { lat.Max(), lon.ToList()[lat.ToList().IndexOf(lat.Max())] };
        double[] locLonMin = new double[] { lon.Min(), lat.ToList()[lon.ToList().IndexOf(lon.Min())] };
        double[] locLonMax = new double[] { lon.Max(), lat.ToList()[lon.ToList().IndexOf(lon.Max())] };

        // Calculate the maximum distances that have to be displayed on the map using CheapRuler, which approximates well for distances < 500km
        var rulerLat = new CheapRuler(lat.Min(), CheapRulerUnits.Meters);
        var rulerLon = new CheapRuler(lon.Min(), CheapRulerUnits.Meters);
        double distHeight = rulerLat.Distance(locLatMin, locLatMax);
        double distWidth = rulerLon.Distance(locLonMin, locLonMax);

        // Obtaining meters per pixel by fitting quadratic function through values for zoom levels given at the above link
        double metersPerPixel = 78674.1 - 120.601 * latCenter - 8.72843 * Mathf.Pow(latCenter, 2);
        const int pixelsPerTile = 256;

        // Get how many meters are covered by the map in each direction
        double metersWidth = tilesWidth * pixelsPerTile * metersPerPixel;
        double metersHeight = tilesHeight * pixelsPerTile * metersPerPixel;

        // Reverse computations of scale, to compute target zoom that covers the required distance
        double targetScale = Math.Min(metersWidth / distWidth, metersHeight / distHeight);
        float targetZoom = (float)Math.Log(targetScale, 2.0f) + 1; // Offset of 1, bc 256 instead of 512 pixels per tile

        // Hard Cap the target zoom at 18
        targetZoom = targetZoom > 18 ? 18 : targetZoom;

        return targetZoom;
    }

    /// <summary>
    ///     Constructs scene for selection of the information obejects
    /// </summary>
    private GameObject ConstructSelectionScene(Dictionary<SceneType, GameObject> scenes)
    {
        GameObject selectionScene = new GameObject("SelectionScene");

        // Create Selection objects for the different scenes and links
        List<GameObject> sceneSelectionObjects = new List<GameObject>();
        foreach (var scene in scenes)
        {
            // Instantiate a selection object for the corresponding scene and make it interactable
            var selectionObject = Instantiate(prefabForScene(scene.Key));
            var interactionComponent = selectionObject.AddComponent<Clickable>();
            //var clickInteraction = selectionObject.AddComponent<ClickInteraction>();
            //clickInteraction.OnPointerClick();
            selectionObject.transform.SetParent(selectionScene.transform);
            interactionComponent.sceneOnClick = scene.Value;
            var infoBehavior = selectionObject.GetComponent<InfoBehavior>();
            if (infoBehavior != null)
            {
                infoBehavior.SetInfoText(infoForScene(scene.Key));
            }

            sceneSelectionObjects.Add(selectionObject);
        }

        //Arrange selection objects
        arrangeInCircle(sceneSelectionObjects, Vector3.zero, 0.25f);

        return selectionScene;
    }

    /// <summary>
    ///     Constructs scene displaying info over person
    /// </summary>
    /// <param name="photo"> Photo of the person (can be null)</param>
    /// <param name="name"> The persons name</param>
    /// <param name="dates"> information about life and death dates if available</param>
    private GameObject ConstructBaseScene(Texture photo, string name, string dates)
    {
        GameObject baseScene = new GameObject("BaseScene");

        // Create Person Info on head height
        GameObject personInfo = new GameObject();
        personInfo.transform.position = new Vector3(0f, 1.25f, 3f);
        personInfo.transform.SetParent(baseScene.transform);
        float textOffset = 0f;

        // Photo:
        if (photo != null)
        {
            float aspect = photo.height / (float)photo.width;
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.localScale = new Vector3(1f, 1f * aspect, 1);
            quad.GetComponent<Renderer>().material.SetTexture("_MainTex", photo);
            quad.transform.SetParent(personInfo.transform, false);
            textOffset = -quad.transform.localScale.y / 2;
        }

        // Name and birth/death date of the person
        GameObject personText = Instantiate(_person3DInfoPrefab);
        personText.transform.SetParent(personInfo.transform, false);
        personText.transform.localPosition = new Vector3(0f, textOffset, 0f);
        var texts = personText.GetComponentsInChildren<TextMesh>(true);
        texts[0].text = name;
        texts[1].text = dates;

        return baseScene;
    }

    /// <summary>
    ///     Arrange the objects in a circle around the center
    /// </summary>
    /// <param name="objects"> Objects to arrange</param>
    /// <param name="center"> center of the circle</param>
    /// <param name="radius"> radius of the circle</param>
    public void arrangeInCircle(List<GameObject> objects, Vector3 center, float radius)
    {
        for (var i = 0; i < objects.Count; i++)
        {
            var angle = i * Mathf.PI * 2 / objects.Count;
            var pos = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;
            objects[i].transform.position = center + pos;
            objects[i].transform.rotation = Quaternion.LookRotation(-pos, Vector3.up);
        }
    }

    private GameObject prefabForScene(SceneType sceneType)
    {
        switch (sceneType)
        {
            case SceneType.Info:
                return _infoScenePrefab;
            case SceneType.LifeStations:
                return _lifeStationPrefab;
            case SceneType.Family:
                return _familyScenePrefab;
            case SceneType.RelatedStolpersteine:
                return _relatedStolpersteineScenePrefab;
            case SceneType.AudioPlayer:
                return _audioPlayerPrefab;
            case SceneType.AdditionalInfo:
                return _infoAdditionalPrefab;
            default:
                throw new ArgumentException("unhandled scene type: " + sceneType);
        }
    }

    private string infoForScene(SceneType sceneType)
    {
        switch (sceneType)
        {
            case SceneType.Info:
                return "Info";
            case SceneType.LifeStations:
                return "Life Stations";
            case SceneType.Family:
                return "Family";
            case SceneType.RelatedStolpersteine:
                return "Related Stolpersteine";
            case SceneType.AudioPlayer:
                return "Audio Player";
            case SceneType.AdditionalInfo:
                return "Additional Info";
            default:
                throw new ArgumentException("unhandled scene type: " + sceneType);
        }
    }

}

