using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Inspired by https://www.youtube.com/watch?v=OE66gtiF8QQ
// When the camera is focused on the object, the associated information gets displayed

public class InfoOnGaze : MonoBehaviour
{
    private Transform _cam;
    private List<InfoBehavior> _infos = new List<InfoBehavior>();

    void Start()
    {
        _cam = Camera.main.transform;    
    }

    void Update()
    {
        if (Physics.Raycast(_cam.position, _cam.forward, out RaycastHit hitObject))
        {
            InfoBehavior info = hitObject.transform.GetComponent<InfoBehavior>();
            if (info != null)
            {
                DisplayInfo(info);
            }
            else
            {
                HideAll();
            }
        }
    }

    void DisplayInfo(InfoBehavior desiredInfo)
    {
        // We can not do this in Start(), since Objects change
        _infos = FindObjectsOfType<InfoBehavior>().ToList();
        foreach (InfoBehavior info in _infos)
        {
            if (info == desiredInfo)
            {
                info.DisplayInfo();
            }
            else
            {
                info.HideInfo();
            }
        }
    }

    void HideAll()
    {
        foreach (InfoBehavior info in _infos)
        {
            info.HideInfo();
        }
    }
}