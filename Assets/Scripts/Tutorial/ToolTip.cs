using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTip : MonoBehaviour, IPointerClickHandler
{
    public bool isMoving = true;
    public List<Vector3> positions;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(isMoving)
            MoveTooltip();
    }

    private void MoveTooltip()
    {
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Deactivate();
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
