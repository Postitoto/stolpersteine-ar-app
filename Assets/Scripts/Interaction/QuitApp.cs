using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitApp : MonoBehaviour
{
    void Update()
    {
        // Make sure user is on Android platform
        if (Application.platform == RuntimePlatform.Android)
        {
            // Check if Back was pressed this frame
            if (Input.GetKeyDown(KeyCode.Escape))
            {

                // Quit the application
                quitApp();
            }
        }
    }

    /// <summary>
    /// Quits the application
    /// </summary>
    public void quitApp()
    {
        Application.Quit();
    }
}
