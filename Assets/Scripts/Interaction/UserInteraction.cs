using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Coordinates interaction of Clickable Objects

public class UserInteraction : MonoBehaviour
{
    [SerializeField]
    private Button previousSceneButton;

    private GameObject currentScene;
    private Camera arCamera;
    private Vector2 touchPosition;
    private Stack<GameObject> previousScenes;
    private int cullingMaskBackup;
    void Start()
    {
        Reset();
        arCamera = Camera.main;
        cullingMaskBackup = arCamera.GetComponent<Camera>().cullingMask;
        if (previousSceneButton != null)
        {
            previousSceneButton.onClick.AddListener(PreviousScene);
        }
    }

    // Start with base scene of the new stolperstein and empty previous scene stack
    public void NewStolperstein(GameObject stolpersteinScene)
    {
        Reset();
        // The first child of the stolpersteinScene root GameObject is taken as start scene
        currentScene = stolpersteinScene.transform.GetChild(0).gameObject;
        currentScene.SetActive(true);
    }

    public void Reset()
    {
        currentScene = null;
        previousScenes = new Stack<GameObject>();
        previousSceneButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if (currentScene == null)
        {
            return;
        }

        // Touching the back key (on the phone) also brings the user back
        if (Input.GetKey(KeyCode.Escape))
        {
            PreviousScene();
            return;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended)
            {
                Ray ray = arCamera.ScreenPointToRay(touch.position);
                RaycastHit hitObject;
                if (Physics.Raycast(ray, out hitObject))
                {
                    // Check if an interactive element was touched
                    Clickable clickable = hitObject.transform.GetComponent<Clickable>();
                    if (clickable != null)
                    {
                        if (clickable.sceneOnClick == null)
                        {
                            Debug.Log("clickable object with no sceneOnClick specified was clicked");
                        }
                        currentScene.SetActive(false);

                        clickable.sceneOnClick.SetActive(true);
                        previousScenes.Push(currentScene);
                        currentScene = clickable.sceneOnClick;
                        previousSceneButton.gameObject.SetActive(true);
                        // Text Scenes disable the main camera (must come after scene switch)
                        if (clickable.sceneOnClick.name.StartsWith("2D"))
                        {
                            
                            arCamera.GetComponent<Camera>().enabled = false;
                        }
                    }
                }

            }
        }
    }

    public void LoadSceneOnButtonClick(GameObject scene, GameObject fromScene = null)
    {
        if (scene == null)
        {
            return;
        }
        if (fromScene == null)
        {
            currentScene.SetActive(false);
            previousScenes.Push(currentScene);
        }
        else
        {
            fromScene.SetActive(false);
            previousScenes.Push(fromScene);
        }
        scene.SetActive(true);
        currentScene = scene;
        previousSceneButton.gameObject.SetActive(true);
        // Text Scenes disable the main camera
        if (scene.name.StartsWith("2D"))
        {
            arCamera.GetComponent<Camera>().enabled = false;
        }
    }

    public void PreviousScene()
    {

        if (previousScenes.Count > 0)
        {
            currentScene.SetActive(false);
            currentScene = previousScenes.Pop();
            currentScene.SetActive(true);
            if (previousScenes.Count == 0)
            {
                previousSceneButton.gameObject.SetActive(false);
            }
            if (!arCamera.GetComponent<Camera>().isActiveAndEnabled && !currentScene.name.StartsWith("2D"))
            {
                arCamera.GetComponent<Camera>().enabled = true;
            }
        }
    }
}

