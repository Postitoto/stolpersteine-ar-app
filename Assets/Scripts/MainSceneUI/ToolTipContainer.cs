using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolTipContainer : MonoBehaviour
{
    public List<ToolTipBehaviour> toolTipContainer;

    private void Start()
    {
        Settings.SettingChanged += ChangedTooltipActive;
    }
    
    private void ChangedTooltipActive(Settings.Setting setting)
    {
        if (setting != Settings.Setting.Tooltip) return;
        toolTipContainer.ForEach(x => x.gameObject.SetActive(Settings.TooltipsActive));   
    }
}
