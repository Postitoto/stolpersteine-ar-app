using UnityEngine;
using UnityEngine.UI;

//adapted from Mapbox, extended to also log Debug.Log messages
public class DebugConsole : MonoBehaviour
{
    [SerializeField]
    Button _toggleButton;

    [SerializeField]
    GameObject _menu;

    [SerializeField]
    Text _logText;

    [SerializeField]
    ScrollRect _logScroll;

    [SerializeField]
    bool _displayStacktrace;

    static DebugConsole _instance;
    public static DebugConsole Instance { get { return _instance; } }

    string _log;


    protected virtual void Awake()
    {
        if (_instance != null)
        {
            Debug.LogError("Duplicate singleton!", gameObject);
        }
        _instance = this;
        ClearLog();
    }

    void ClearLog()
    {
        _log = "";
        _logText.text = _log;
        _logScroll.verticalNormalizedPosition = 0f;
    }

    public void Log(string log, string color)
    {
        if (!string.IsNullOrEmpty(_log) && _log.Length > 15000)
        {
            _log = "";
        }
        _log += string.Format("<color={0}>{1}</color>\n", color, log);
        _logText.text = _log;
        _logScroll.verticalNormalizedPosition = 0f;
    }


    public void ToggleMenu()
    {
        _menu.SetActive(!_menu.activeSelf);
    }

    //listen to debug log messages and display them
    
    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }
    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type) {
        Log(logString, "white");
        if(_displayStacktrace)
        {
            Log(stackTrace, "red");
        }
    }
    
}