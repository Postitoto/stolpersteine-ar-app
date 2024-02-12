using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RuntimeInspectorNamespace;
using Scriptables;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using TMPro;
using UnityEngine.Video;

// Performs communication with the backend and offers data to other components

public class BackendInterface : MonoBehaviour
{
    public delegate void NotifyLocationsLoaded();
    public static event NotifyLocationsLoaded OnLocationsLoaded;

    public delegate void NotifyToursLoaded();
    public static event NotifyToursLoaded OnToursLoaded;
    
    // Variables for mock behaviour
    // Can be used, if it is not possible to connect to the database. Stores content locally instead
    public bool mock;
    public string mockLocationsJsonString;
    public string mockStolpersteineJsonString;
    public List<Texture> mockImageStore;

    public List<string> StolpersteinLocations { get; private set; }
    public Dictionary<string, string> StolpersteinLocationNames { get; private set; }
    
    public List<Location> Locations { get; private set; }
    public List<Stolperstein> Stolpersteine { get; private set; }
    public List<Tour> Tours { get; private set; }
    
    // Images
    public Dictionary<string, Texture> Images { get; private set; }

    // Audio
    public Dictionary<string, AudioClip> Audios { get; private set; }

    [SerializeField]
    private string URL = "https://cryptic-depths-19636.onrender.com/";

    [SerializeField] private TextMeshProUGUI locationText;
    
    private const string LOCATIONS_SUFFIX = "api/get-locations/";
    private const string STOLPERSTEINE_SUFFIX = "api/stolpersteine/";
    private const string STOLPERSTEINE_AT_SUFFIX = "api/get-stolpersteine/";
    private const string TOURS = "api/get-tours/";
    private const string TOURLOC = "api/get-tour-locations/";
    
    private const string AUTH_HEADER_STRING = "fb52f9b647cf14b92ffa25a52b0c668d3859b8cc";
    private bool _isDownloading;
    private bool _isAudioDownload = false;
    private bool _isImageDownload = false;
    private int _downloadCount;

    void Start()
    {
        // Initially retrieve all Stolperstein locations
        Images = new Dictionary<string, Texture>();
        Audios = new Dictionary<string, AudioClip>();
        StartCoroutine(GetAllLocations((List<Location> values) =>
        {
            Locations = values;
            OnLocationsLoaded?.Invoke();
        }));
        
        StartCoroutine(GetTours((List<Tour> tours) =>
        {
            Tours = tours;
        }));
        
        StartCoroutine(GetTourLocations());
    }
    
    // Queries for all stolperstein locations (without duplicates)
    private IEnumerator GetAllLocations(Action<List<Location>> callback)
    {
        locationText.text = "Loading locations ...";
        
        Tuple<List<string>, Dictionary<string, string>> callbackValues;
        if (mock)
        {
            List<Location> locations = ParseLocations(mockLocationsJsonString);
            callback(locations);
        }
        else
        {
            using (UnityWebRequest request = UnityWebRequest.Get(URL + LOCATIONS_SUFFIX))
            {
                request.SetRequestHeader("Authorization", "Token " + AUTH_HEADER_STRING);
                yield return request.SendWebRequest();

                switch (request.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.Log(request.error);
                        callback(null);
                        break;

                    case UnityWebRequest.Result.Success:
                        string bodyText = request.downloadHandler.text;
                        List<Location> locations = ParseLocations(bodyText);
                        callback(locations);
                        break;

                    default:
                        Debug.Log("unhandled result type: " + request.result);
                        callback(null);
                        break;
                }
            }
        }
    }
    
    /// <summary>
    ///     Returns all stolperstein scene json strings for a location from the backend
    ///     Should be called from within a coroutine
    /// </summary>
    /// <param name="coordinates"> Location for which the Stolpersteine should be retrieced</param>
    /// <param name="callback"> Callback function to process the returned results </param>
    // 
    public IEnumerator GetStolpersteineAt(string coordinates, Action<List<JSONNode>> callback)
    {
        // In mock mode, no actual request is made, but locally stored data is read out
        if (mock)
        {
            string locUrl = "";
            JSONNode locationsJSON = JSON.Parse(mockLocationsJsonString);
            foreach (var location in locationsJSON.Values)
            {
                if (location["coordinates"] == coordinates)
                {
                    locUrl = location["url"];
                }
            }
            Debug.Assert(locUrl != "");

            List<JSONNode> allStolpersteineJson = ParseStolpersteine(mockStolpersteineJsonString);
            List<JSONNode> stolpersteineJson = new List<JSONNode>();
            foreach (JSONNode stolpersteinJSON in allStolpersteineJson)
            {
                if (stolpersteinJSON["location"] == locUrl)
                {
                    stolpersteineJson.Add(stolpersteinJSON);
                }
            }

            callback(stolpersteineJson);
        }

        // In normal mode, the data is acquired by performing HTTP request to the Database
        else
        {
            using (UnityWebRequest request = UnityWebRequest.Get(URL + STOLPERSTEINE_AT_SUFFIX + coordinates))
            {
                request.SetRequestHeader("Authorization", "Token " + AUTH_HEADER_STRING);
                yield return request.SendWebRequest();

                switch (request.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.Log(request.error);
                        callback(null);
                        break;

                    case UnityWebRequest.Result.Success:
                        List<JSONNode> stolpersteineJson = ParseStolpersteine(request.downloadHandler.text);
                        Debug.Log("GetStolpersteineAt() returned\n" + string.Join(",", stolpersteineJson));
                        // Only return, when all needed resources are downloaded
                        yield return new WaitUntil(() => !_isDownloading && _downloadCount == 0);
                        callback(stolpersteineJson);
                        break;

                    default:
                        Debug.Log("unhandled result type: " + request.result);
                        callback(null);
                        break;
                }
            }
        }
    }

    private IEnumerator GetTours(Action<List<Tour>> callback)
    {
        if (mock)
        {
            callback(null);
            yield return null;
        }
        
        
        using (UnityWebRequest request = UnityWebRequest.Get(URL + TOURS))
        {
            request.SetRequestHeader("Authorization", "Token " + AUTH_HEADER_STRING);
            yield return request.SendWebRequest();

            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.Log(request.error);
                    callback(null);
                    break;

                case UnityWebRequest.Result.Success:
                    string bodyText = request.downloadHandler.text;
                    var tours = ParseTours(bodyText);
                    callback(tours);
                    break;

                default:
                    Debug.Log("unhandled result type: " + request.result);
                    callback(null);
                    break;
            }
        }
    }

    private IEnumerator GetTourLocations()
    {
        if (mock)
        {
            yield return null;
        }
        
        using (UnityWebRequest request = UnityWebRequest.Get(URL + TOURLOC))
        {
            request.SetRequestHeader("Authorization", "Token " + AUTH_HEADER_STRING);
            yield return request.SendWebRequest();

            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.Log(request.error);
                    break;

                case UnityWebRequest.Result.Success:
                    yield return new WaitUntil(() => Tours != null && Locations != null);
                    AddLocationsToTour(request.downloadHandler.text);
                    break;

                default:
                    Debug.Log("unhandled result type: " + request.result);
                    break;
            }
        }
    }

    private void AddLocationsToTour(string jsonString)
    {
        var data = JSON.Parse(jsonString);
        
        foreach (var tour in Tours)
        {
            tour.locationsInOrder = new List<TourLocation>();
            foreach (var node in data.Values)
            {
                if (node["tour"] != tour.id)
                {
                    continue;
                }
                
                var loc = Locations.Find(loc => loc.id == node["location"]);
                var tourLock = ScriptableObject.CreateInstance<TourLocation>();
                tourLock.id = loc.id;
                tourLock.address = loc.address;
                tourLock.coordinates = loc.coordinates;
                tourLock.stones = loc.stones;
                tourLock.order = node["order"];
                tourLock.audioName = node["audioName"];
                tourLock.audio = node["audio"];
                tour.locationsInOrder.Add(tourLock);
            }

            tour.locationsInOrder.Sort((x, y) => x.order.CompareTo(y.order));
        }
        
        OnToursLoaded?.Invoke();
    }
    
    private List<Location> ParseLocations(string jsonString)
    {
        List<Location> locations = new List<Location>();
        JSONNode data = JSON.Parse(jsonString);
        foreach (var locString in data.Values)
        {
            var location = ScriptableObject.CreateInstance<Location>();
            location.id = locString["id"];
            location.address = locString["name"];
            location.coordinates = locString["coordinates"];
            locations.Add(location);
        }
        return locations;
    }

    private Dictionary<string, string> ParseLocationNames(string jsonString)
    {
        Dictionary<string, string> locationToName = new Dictionary<string, string>();
        JSONNode data = JSON.Parse(jsonString);
        foreach (var location in data.Values)
        {
            locationToName.Add(location["coordinates"], location["name"]);
        }
        return locationToName;
    }

    private List<JSONNode> ParseStolpersteine(string jsonString)
    {
        List<JSONNode> stolpersteine = new List<JSONNode>();
        JSONNode data = JSON.Parse(jsonString);
        SaveImages(data);
        SaveAudioFiles(data);
        foreach (JSONNode stolperstein in data)
        {
            stolpersteine.Add(stolperstein);
        }

        return stolpersteine;

    }

    private JSONNode ParseSingleStone(string jsonString)
    {
        var data = JSON.Parse(jsonString);
        SaveImage(data);
        SaveAudioFile(data);
        return data;
    }

    private List<Tour> ParseTours(string jsonString)
    {
        List<Tour> tours = new List<Tour>();
        var data = JSON.Parse(jsonString);
        foreach (var tourString in data.Values)
        {
            var tour = ScriptableObject.CreateInstance<Tour>();
            tour.id = tourString["id"];
            tour.name = tourString["name"];
            tour.description = tourString["description"];
            tours.Add(tour);
        }
        
        return tours;
    }
    
    /// <summary>
    ///     Recursively traverse JSON and look for image links.
    ///     If (image) link is found, try downloading image and if successfull, 
    ///     save as variable and change the json entry accordingly
    /// </summary>
    /// <param name="json"> The json node for which this should be performed</param>
    private void SaveImages(JSONNode json)
    {
        _isDownloading = true;
        _isImageDownload = true;
        foreach (JSONNode node in json)
        {
           SaveImage(node);
        }
        _isImageDownload = false;
        SetIsDownloading();
    }

    private void SaveImage(JSONNode node)
    {
         if (!node.HasKey("files") || node.IsArray)
         {
             SaveImages(node);
         }
         // Check if the value is a link
         else 
         {
             JSONNode filesList = node["files"];
             string photoName = filesList["photoName"];
             string photo = filesList["photo"];
             Debug.Log("Found photo: " + photo);
             if (Uri.IsWellFormedUriString(photo, UriKind.Absolute))
             {
                 Debug.Log("photo is well formed uri");
                 // Check if its an image (png or jpg) and download it, if not cached
                 // string fileExt = System.IO.Path.GetExtension(node.Value);
                 if (!Images.ContainsKey(photo))
                 {
                     // In mock mode, instead of downloading, access local image store
                     if (mock)
                     {
                         Texture img = null;
                         foreach (Texture tex in mockImageStore)
                         {
                             var photoPath = node.Value.Split('/');
                             var photoNameExt = photoPath[photoPath.Length - 1].Split('.');
                             var photoNameLegacy = photoNameExt[0];
                             if (tex.name == photoNameLegacy)
                             {
                                 img = tex;
                             }
                         }
                         if (img != null && !Images.ContainsKey(node.Value))
                         {
                             Images.Add(node.Value, img);
                         }
                     }

                     // Download the link and interpret as image
                     Debug.Log("about to download photo " + photo);
                     _downloadCount++;
                     StartCoroutine(DownloadImage(photo, (Texture img) =>
                     {
                         if (img != null && !Images.ContainsKey(photo))
                         {
                             // Cache the downloaded image
                             Images.Add(photo, img);

                             _downloadCount--;
                             Debug.Log("Downloaded photo: " + photo);
                         }
                     }));
                 }
             }      
                
         }
    }

    /// <summary>
    ///     Traverses initial json data and looks for audio files delivered with the stolpersteine.
    ///     Found audio files are then downloaded and are available to be played in the stolperstein scene.
    /// </summary>
    /// <param name="json"> The json node for which this should be performed</param>
    private void SaveAudioFiles(JSONNode json)
    {
        _isDownloading = true;
        _isAudioDownload = true;
        foreach (JSONNode node in json)
        {
          SaveAudioFile(node);
        }
        _isAudioDownload = false;
        this.SetIsDownloading();
    }

    private void SaveAudioFile(JSONNode node)
    {
        if (!node.HasKey("files") || node.IsArray)
        {
            SaveAudioFiles(node);
        }
        // Check if the value is a link
        else
        {
            JSONNode filesList = node["files"];
            string audioName = filesList["audioName"];
            string audio = filesList["audio"];
            Debug.Log("Found audio file: " + audio);
            if (Uri.IsWellFormedUriString(audio, UriKind.Absolute) && !Audios.ContainsKey(audio))
            {
                _downloadCount++;
                StartCoroutine(DownloadAudio(audio, (AudioClip audioClip) =>
                {
                    // Save the downloaded audio clip
                    Audios.Add(audio, audioClip);
                    _downloadCount--;
                }));
            }
        }
    }

    /// <summary>
    ///     Downloads a resource and converts it to texture. 
    ///     Should be called from within a coroutine
    /// </summary>
    /// <param name="url"> The url of the resource</param>
    /// <param name="callback"> Callback function</param>
    IEnumerator DownloadImage(string url, Action<Texture> callback)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        switch (request.result)
        {
            case UnityWebRequest.Result.ConnectionError:
            case UnityWebRequest.Result.DataProcessingError:
            case UnityWebRequest.Result.ProtocolError:
                Debug.Log(request.error + "from request: " + url);
                callback(null);
                break;

            case UnityWebRequest.Result.Success:
                DownloadHandlerTexture dhTexture = (DownloadHandlerTexture)request.downloadHandler;
                Texture texture = dhTexture.texture;
                Debug.Log("Downloaded texture " + texture.name);
                callback(texture);
                break;

            default:
                Debug.Log("unhandled result type: " + request.result);
                callback(null);
                break;
        }
    }

    public IEnumerator DownloadAudio(string url, Action<AudioClip> callback)
    {
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
        yield return request.SendWebRequest();
        switch (request.result)
        {
            case UnityWebRequest.Result.ConnectionError:
            case UnityWebRequest.Result.DataProcessingError:
            case UnityWebRequest.Result.ProtocolError:
                Debug.Log(request.error + "from request: " + url);
                callback(null);
                break;

            case UnityWebRequest.Result.Success:
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
                Debug.Log("Downloaded audio " + audioClip);
                callback(audioClip);
                break;

            default:
                Debug.Log("unhandled result type: " + request.result);
                callback(null);
                break;
        }
    }
    
    private void SetIsDownloading()
    {
        if (!_isImageDownload && !_isAudioDownload)
        {
            this._isDownloading = false;
        }
    }

    
    #region Scene Construction Test

    [SerializeField]
    private GameObjectEvent _sceneCreated;
    
    public StolpersteinSceneConstructor SceneConstructor;
    
    public void ConstructTestScene(string stoneId)
    {
        int id = -1;
        GameObject constructedScene = null;
        if (Int32.TryParse(stoneId, out id))
        {
            StartCoroutine(GetStolpersteinById(id, callback =>
            {
                constructedScene = SceneConstructor.ConstructScene(callback, Images, Audios);
                
                if (constructedScene == null)
                    return;
        
                _sceneCreated.Invoke(constructedScene);
            }));
        }
        else
        {
            StartCoroutine(GetStolpersteineAt(stoneId, (List<JSONNode> stolpersteineJson) =>
            {
                var stoneJson = stolpersteineJson;
                
                constructedScene = SceneConstructor.ConstructScene(stoneJson.First(), Images, Audios);
                
                if (constructedScene == null)
                    return;
        
                _sceneCreated.Invoke(constructedScene);
            }));
        }
    }

    private IEnumerator GetStolpersteinById(int id, Action<JSONNode> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(URL + STOLPERSTEINE_SUFFIX + id))
        {
            request.SetRequestHeader("Authorization", "Token " + AUTH_HEADER_STRING);
            yield return request.SendWebRequest();

            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.Log(request.error);
                    callback(null);
                    break;

                case UnityWebRequest.Result.Success:
                    var stoneJsonList = ParseSingleStone(request.downloadHandler.text);
                    // Only return, when all needed resources are downloaded
                    yield return new WaitUntil(() => !_isDownloading && _downloadCount == 0);
                    callback(stoneJsonList);
                    break;

                default:
                    Debug.Log("unhandled result type: " + request.result);
                    callback(null);
                    break;
            }
        }
    }
    
    #endregion 
   
}
