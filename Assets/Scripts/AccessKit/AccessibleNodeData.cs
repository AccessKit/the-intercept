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
        public bool invisible;
        [DefaultValue(DefaultActionVerb.none)]
        public DefaultActionVerb defaultActionVerb;

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
            invisible = node.invisible;
            defaultActionVerb = node.defaultActionVerb;
        }
        
        string getName(AccessibleNode node)
        {
            if (node.role == AccessibleRole.presentation || node.role == AccessibleRole.pane)
                return null;
            var text = node.gameObject.GetComponentInChildren(typeof(Text)) as Text;
            return text != null ? text.text : node.name;
        }
    }
}
