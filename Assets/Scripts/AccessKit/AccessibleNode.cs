using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AccessKit
{
    public class AccessibleNode : MonoBehaviour
    {
        static ulong nextId = 2;
        public ulong id;
        public AccessibleRole role;
        public ulong parent;
        public uint indexInParent;
        public bool focusable;
        public bool visible;
        
        void Start()
        {
            id = nextId++;
        }
    }
}
