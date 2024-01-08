using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialSystem : MonoBehaviour
{
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private List<GameObject> toolTipTargets;
    [SerializeField] private List<ToolTip> toolTips;

    private void Start()
    {
        Settings.SettingChanged += ChangedTooltipActive;
    }
    
    private void ChangedTooltipActive(Settings.Setting setting)
    {
        if (setting != Settings.Setting.Tooltip) return;
        toolTips.ForEach(x => x.gameObject.SetActive(Settings.TooltipsActive));   
    }
}
