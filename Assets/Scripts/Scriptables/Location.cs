using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    public class Location : ScriptableObject
    {
        public int id;
        public string address;
        public string coordinates;
        public List<Stolperstein> stones;
    }
}