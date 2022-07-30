using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AccessKit
{
    public class AccessibleNode : MonoBehaviour
    {
        public static SortedList<HierarchyPosition, AccessibleNode> allAccessibles = new SortedList<HierarchyPosition, AccessibleNode>();
        HierarchyPosition currentHierarchyPosition;
        static ulong nextId = 2;
        public ulong id;
        public AccessibleRole role;
        public string accessibleName;
        public AccessibleNode parent;
        public bool focusable;
        public bool invisible;
        public DefaultActionVerb defaultActionVerb;
        public bool canBeFocused
        {
            get { return focusable && !invisible && (role != AccessibleRole.staticText); }
        }
        public AriaLive live;
        
        void Awake()
        {
            id = nextId++;
        }
        
        void OnEnable()
        {
            currentHierarchyPosition = computePosition();
            allAccessibles.Add(currentHierarchyPosition, this);
        }

        HierarchyPosition computePosition()
        {
            var transforms = GetComponentsInParent<Transform>();
            var indices = new List<ulong>(transforms.Length);
            if (transforms.Length > 0)
            {
                var topLevelTransform = transforms[transforms.Length - 1];
                var topLevelAccessible = topLevelTransform.GetComponent<AccessibleNode>();
                if (topLevelAccessible != null)
                    indices.Add(topLevelAccessible.id);
            }
            for (int i = transforms.Length - 2; i >= 0; i--)
                indices.Add((ulong)transforms[i].GetSiblingIndex());
            return new HierarchyPosition(indices);
        }

        void Update()
        {
            if (transform.hasChanged)
            {
                var newHierarchyPosition = computePosition();
                if (currentHierarchyPosition != newHierarchyPosition)
                {
                    var oldIndex = allAccessibles.Values.IndexOf(this);
                    if (oldIndex >= 0)
                        allAccessibles.RemoveAt(oldIndex);
                    allAccessibles.Add(newHierarchyPosition, this);
                }
            }
        }
        
        void OnDisable()
        {
            int index = allAccessibles.IndexOfValue(this);
            allAccessibles.RemoveAt(index);
        }
    }
}
