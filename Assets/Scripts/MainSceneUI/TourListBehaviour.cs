using System;
using System.Collections;
using System.Collections.Generic;
using Scriptables;
using TMPro;
using UnityEngine;

public class TourListBehaviour : MonoBehaviour
{
    public delegate void NotifyClose();
    public static event NotifyClose OnClose;
    public delegate void NotifyOpen();
    public static event NotifyOpen OnOpen;

    public GameObject tourListEntry;
    public Transform entryParent;
    public RectTransform listBackgroundRect;
    public RectTransform descriptionBackgroundRect;
    public List<StoneListEntryBehaviour> entries;
    public TextMeshProUGUI tourDetailTitle;
    public TextMeshProUGUI tourDetailDescription;
    
    [SerializeField] private BackendInterface backendInterface;
    [SerializeField] private TourManager tourManager;

    private Tour selectedTour;
    private Vector2 touchStartPosition;
    private bool isTouching;

    private void Awake()
    {
        if (backendInterface == null)
        {
            backendInterface = FindObjectOfType<BackendInterface>();
        }
        if (tourManager == null)
        {
            tourManager = FindObjectOfType<TourManager>();
        }
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            CheckTouchLocation(Input.GetTouch(0));
        }
    }

    private void CheckTouchLocation(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                // Saves the position where the touch was started
                RectTransformUtility.ScreenPointToLocalPointInRectangle(listBackgroundRect, touch.position, null,
                    out touchStartPosition);
                isTouching = true;
                break;
            case TouchPhase.Ended:
                if (!isTouching) return;
                if (!listBackgroundRect.rect.Contains(touchStartPosition))
                    Close();
                isTouching = false;
                break;
            case TouchPhase.Canceled:
                isTouching = false;
                break;
        }
    }

    public void LoadDescription(Tour tour)
    {
        selectedTour = tour;
        tourDetailTitle.text = tour.name;
        tourDetailDescription.text = tour.description;
        listBackgroundRect.gameObject.SetActive(false);
        descriptionBackgroundRect.gameObject.SetActive(true);
    }

    public void StartTour()
    {
        tourManager.StartTour(selectedTour);
        Close();
    }

    public void ReturnToList()
    {
        selectedTour = null;
        descriptionBackgroundRect.gameObject.SetActive(false);
        listBackgroundRect.gameObject.SetActive(true);
    }

    public void PopulateTourList(List<Tour> tours)
    {
        foreach (var tour in tours)
        {
            var entry = Instantiate(tourListEntry, entryParent).GetComponent<TourListEntryBehaviour>();
            entry.Init(tour);
        }
    }
    
    private void OnEnable()
    {
        OnOpen?.Invoke();
    }

    public void Close()
    {
        if (descriptionBackgroundRect.gameObject.activeSelf)
        {
            ReturnToList();
        }
       
        OnClose?.Invoke();
    }
}
