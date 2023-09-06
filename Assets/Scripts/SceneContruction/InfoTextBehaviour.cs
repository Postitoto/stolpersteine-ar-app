using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InfoTextBehaviour : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI infoText;

    public void SetTitle(string title)
    {
        titleText.text = title;
    }

    public void SetInfoText(string text)
    {
        infoText.text = text;
    }
}
