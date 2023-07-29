using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

// Performs communication with the backend and offers data to other components

public class BackendInterface : MonoBehaviour
{
    // Variables for mock behaviour
    // Can be used, if it is not possible to connect to the database. Stores content locally instead
    public bool _mock;
    public string _mockLocationsJSONString;
    public string _mockStolpersteineJSONString;
    public List<Texture> _mockImageStore;

    public List<string> StolpersteinLocations { get; private set; }
    public Dictionary<string, string> StolpersteinLocationNames { get; private set; }
    // Images
    public Dictionary<string, Texture> Images { get { return _images; } }
    private Dictionary<string, Texture> _images;

    // Audio
    public Dictionary<string, AudioClip> Audios { get { return _audios; } }
    private Dictionary<string, AudioClip> _audios;

    [SerializeField]
    private string URL = "https://cryptic-depths-19636.herokuapp.com/";

    const string COORDINATES_SUFFIX = "get_coordinates/";
    const string STOLPERSTEINE_SUFFIX = "get_stolpersteine/";
    const string AUTH_TOKEN = "Token 04c696a540743ac5e7242e57368a707cb7585c1e";
    private bool _isDownloading;
    private bool _isAudioDownload = false;
    private bool _isImageDownload = false;
    private int _downloadCount;

    void Start()
    {
        // Initially retrieve all Stolperstein locations
        _images = new Dictionary<string, Texture>();
        _audios = new Dictionary<string, AudioClip>();
        StartCoroutine(GetAllLocations((Tuple<List<string>, Dictionary<string, string>> values) =>
            {
                Debug.Log(values.Item1);
                StolpersteinLocations = values.Item1;
                StolpersteinLocationNames = values.Item2;
            }));
    }

    // Queries for all stolperstein locations (without duplicates)
    public IEnumerator GetAllLocations(Action<Tuple<List<string>, Dictionary<string, string>>> callback)
    {
        Tuple<List<string>, Dictionary<string, string>> callbackValues;
        if (_mock)
        {
            List<string> locations = ParseLocations(_mockLocationsJSONString);
            Dictionary<string, string> locationNames = ParseLocationNames(_mockLocationsJSONString);
            callbackValues = new Tuple<List<string>, Dictionary<string, string>>(locations, locationNames);
            callback(callbackValues);
        }
        else
        {
            using (UnityWebRequest request = UnityWebRequest.Get(URL + COORDINATES_SUFFIX))
            {
                request.SetRequestHeader("Authorization", AUTH_TOKEN);
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
                        List<string> locations = ParseLocations(bodyText);
                        Dictionary<string, string> locationNames = ParseLocationNames(bodyText);
                        Debug.Log("GetAllLocations() request from backend returned coordinates: " + string.Join(" | ", locations));
                        Debug.Log("GetAllLocations() request from backend returned names: " + string.Join(" | ", locationNames.Values));
                        callbackValues = new Tuple<List<string>, Dictionary<string, string>>(locations, locationNames);
                        callback(callbackValues);
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
        if (_mock)
        {
            string locUrl = "";
            JSONNode locationsJSON = JSON.Parse(_mockLocationsJSONString);
            foreach (var location in locationsJSON.Values)
            {
                if (location["coordinates"] == coordinates)
                {
                    locUrl = location["url"];
                }
            }
            Debug.Assert(locUrl != "");

            List<JSONNode> allStolpersteineJson = ParseStolpersteine(_mockStolpersteineJSONString);
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
            using (UnityWebRequest request = UnityWebRequest.Get(URL + STOLPERSTEINE_SUFFIX + coordinates))
            {
                request.SetRequestHeader("Authorization", AUTH_TOKEN);
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

    private List<string> ParseLocations(string json_string)
    {
        List<string> coordinates = new List<string>();
        JSONNode data = JSON.Parse(json_string);
        foreach (var location in data.Values)
        {
            coordinates.Add(location["coordinates"]);
        }
        return coordinates;
    }

    private Dictionary<string, string> ParseLocationNames(string json_string)
    {
        Dictionary<string, string> locationToName = new Dictionary<string, string>();
        JSONNode data = JSON.Parse(json_string);
        foreach (var location in data.Values)
        {
            locationToName.Add(location["coordinates"], location["name"]);
        }
        return locationToName;
    }

    private List<JSONNode> ParseStolpersteine(string json_string)
    {
        List<JSONNode> stolpersteine = new List<JSONNode>();
        JSONNode data = JSON.Parse(json_string);
        SaveImages(data);
        SaveAudiofiles(data);
        foreach (JSONNode stolperstein in data)
        {
            stolpersteine.Add(stolperstein);
        }

        return stolpersteine;

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
                        if (_mock)
                        {
                            Texture img = null;
                            foreach (Texture tex in _mockImageStore)
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
                                _images.Add(node.Value, img);
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
                                _images.Add(photo, img);

                                _downloadCount--;
                                Debug.Log("Downloaded photo: " + photo);
                            }
                        }));
                    }
                }      
                
            }
        }
        _isImageDownload = false;
        this.SetIsDownloading();
    }

    /// <summary>
    ///     Traverses initial json data and looks for audio files delivered with the stolpersteine.
    ///     Found audio files are then downloaded and are available to be played in the stolperstein scene.
    /// </summary>
    /// <param name="json"> The json node for which this should be performed</param>
    private void SaveAudiofiles(JSONNode json)
    {
        _isDownloading = true;
        _isAudioDownload = true;
        foreach (JSONNode node in json)
        {
            if (!node.HasKey("files") || node.IsArray)
            {
                SaveAudiofiles(node);
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
                        _audios.Add(audio, audioClip);
                        _downloadCount--;
                    }));
                }
            }
        }
        _isAudioDownload = false;
        this.SetIsDownloading();
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

    IEnumerator DownloadAudio(string url, Action<AudioClip> callback)
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
}
