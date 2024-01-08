using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Settings : MonoBehaviour
{
    public delegate void Notify(Setting setting);
    public static event Notify SettingChanged;
    
    // Tooltip Setting
    public TextMeshProUGUI tooltipText;
    public static bool TooltipsActive = true;
    
    public enum Setting
    {
        Tooltip = 1
    }
    
    public void ExecuteSettingChange(int setting)
    {
        if (!Enum.IsDefined(typeof(Setting), setting)) return;

        switch ((Setting) setting)
        {
            case Setting.Tooltip:
                TooltipsActive = !TooltipsActive;
                tooltipText.text = TooltipsActive ? "On" : "Off";
                break;
        }
        
        SettingChanged?.Invoke((Setting) setting);
    }
}
