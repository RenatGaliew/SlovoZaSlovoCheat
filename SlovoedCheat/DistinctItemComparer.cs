using System.Collections.Generic;

namespace SlovoedCheat
{
    public class DistinctItemComparer : IEqualityComparer<Word>
    {
        public bool Equals(Word x, Word y)
        {
            return x.Name == y.Name;
        }

        public int GetHashCode(Word obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}