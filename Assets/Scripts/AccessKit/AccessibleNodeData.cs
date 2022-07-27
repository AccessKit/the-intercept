using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using UnityEngine.UI;

namespace AccessKit
{
    public class AccessibleNodeData
    {
        public ulong id;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public AccessibleRole role;
        public string name;
        public List<ulong> children;
        public bool focusable;
        [DefaultValue(true)]
        public bool visible = true;

        public AccessibleNodeData(ulong id, AccessibleRole role)
        {
            this.id = id;
            this.role = role;
        }
        
        public AccessibleNodeData(AccessibleNode node, List<ulong> children)
        {
            id = node.id;
            role = node.role;
            this.children = children;
            focusable = node.focusable;
            name = getName(node);
        }
        
        string getName(AccessibleNode node)
        {
            var text = node.gameObject.GetComponentInChildren(typeof(Text)) as Text;
            return text != null ? text.text : node.name;
        }
    }
}