using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppBehaviour : MonoBehaviour
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
                QuitApp();
            }
        }
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }
    
    /// <summary>
    /// Quits the application
    /// </summary>
    public void QuitApp()
    {
        Application.Quit();
    }
}
