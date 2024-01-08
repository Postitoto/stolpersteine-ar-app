using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Allows usage of Events with GameObjects and Lists of GameObjects as Arguments

[System.Serializable]
public class GameObjectEvent : UnityEvent<GameObject> { }

[System.Serializable]
public class GameObjectListEvent : UnityEvent<List<GameObject>> { }