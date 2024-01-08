using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    public class Tour : ScriptableObject
    {
        public int id;
        public string name;
        public string description;
        public List<TourLocation> locationsInOrder;
    }
}