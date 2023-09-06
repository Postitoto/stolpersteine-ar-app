using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TopMenuButtonBehaviour : MonoBehaviour, IPointerDownHandler
{
    public Image menu;
    public List<GameObject> offMenus;

    private GameObject currentlyActive;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        menu.gameObject.SetActive(!menu.gameObject.activeSelf);
        if (menu.gameObject.activeSelf)
        {
            offMenus.ForEach(x => x.SetActive(false));
        }
    }
}
