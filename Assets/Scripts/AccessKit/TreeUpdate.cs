using System;
using System.Collections.Generic;

namespace AccessKit
{
    public class TreeUpdate
    {
        public List<AccessibleNodeData> nodes;
        public ulong? focus;
        public AccessibleTree tree;
        
        public TreeUpdate()
        {
            nodes = new List<AccessibleNodeData>();
        }
    }
}
