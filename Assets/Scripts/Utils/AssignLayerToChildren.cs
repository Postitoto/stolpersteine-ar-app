using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Helper function that assigns all children the same layer as the object itself

public class AssignLayerToChildren : MonoBehaviour
{
    void Update()
    {
        foreach (Transform trans in GetComponentsInChildren<Transform>(true)) {
            trans.gameObject.layer = this.gameObject.layer;
        }
    }
}
