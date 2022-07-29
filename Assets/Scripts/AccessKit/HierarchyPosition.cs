using System;
using System.Collections.Generic;

namespace AccessKit
{
    public class HierarchyPosition : IComparable
    {
        public List<ulong> siblingIndices;
        
        public HierarchyPosition(List<ulong> siblingIndices)
        {
            this.siblingIndices = siblingIndices;
        }

        public int CompareTo(object other)
        {
            if (other == null)
                return 1;
            var otherPosition = other as HierarchyPosition;
            var otherIndices = otherPosition.siblingIndices;
            var count = siblingIndices.Count;
            var otherCount = otherIndices.Count;
            int i = 0;
            while (true)
            {
                if (i == count && i == otherCount)
                    break;
                if (i == count)
                    return -1;
                if (i == otherCount)
                    return 1;
                if (siblingIndices[i] < otherIndices[i])
                    return -1;
                if (siblingIndices[i] > otherIndices[i])
                    return 1;
                i++;
            }
            return 0;
        }
        
        public override bool Equals(object other)
        {
            return CompareTo(other) == 0;
        }
        
        public override int GetHashCode()
        {
            return siblingIndices.GetHashCode();
        }
        
        public static bool operator ==(HierarchyPosition a, HierarchyPosition b)
        {
            return Object.Equals(a, b);
        }
        
        public static bool operator !=(HierarchyPosition a, HierarchyPosition b)
        {
            return !Object.Equals(a, b);
        }
    }
}