using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuButtonBehaviour : MonoBehaviour
{
    public RectTransform buttonPanel;
    public RectTransform creditPanel;

    public MenuImageBehaviour parentImage;
    public TextMeshProUGUI textField;

    private float currentDim;
    
    private void Start()
    {
        UpdateFontSize();
    }

    private void Update()
    {
        if(currentDim != parentImage.GetDimension())
            UpdateFontSize();
    }

    private void UpdateFontSize()
    {
        // I'll calculate the font size in relation to the size of the parent image
        currentDim = parentImage.GetDimension();
        textField.fontSize = currentDim * 0.1f;
    }

    #region ButtonMethods
    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }

    public void ShowCredits()
    {
        creditPanel.gameObject.SetActive(true);
        buttonPanel.gameObject.SetActive(false);
    }

    public void QuitApplication()
    {
        Application.Quit();
    }
    #endregion
}
