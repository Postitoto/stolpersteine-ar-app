using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Marks the GameObject that the script is attached to as interactable via clicking
// Can be used by raycast manager to check if raycasts hit an object that is interactable

[RequireComponent(typeof(Collider))]
public class Clickable : MonoBehaviour
{
    public bool IsActive { get; set; }
    public GameObject sceneOnClick;
}
