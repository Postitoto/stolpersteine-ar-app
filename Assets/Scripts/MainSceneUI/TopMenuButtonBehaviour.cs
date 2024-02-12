using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TopMenuButtonBehaviour : MonoBehaviour, IPointerDownHandler
{
    public Image menu;
    public List<GameObject> offMenus;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        menu.gameObject.SetActive(!menu.gameObject.activeSelf);
        if (menu.gameObject.activeSelf)
        {
            offMenus.ForEach(x => x.SetActive(false));
        }
        else
        {
            offMenus.Last().SetActive(true);
        }
    }
}
