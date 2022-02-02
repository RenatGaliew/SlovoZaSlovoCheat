using System.Reflection;
using Catel.MVVM;

namespace SlovoedCheat
{
    public class Word
    {
        public string Name { get; set; }
        public int _stoimost { get; set; }

        public int Stoimost
        {
            get => _stoimost;
            set
            {
                _stoimost = value;
                if (_stoimost < 0)
                {

                }
            }
        }

        public int Length => Name?.Length ?? 0;
        public int CCoef { get; set; } = 1;
        public int UniqueKey { get; set; }

        public Word()
        {
            Name = "";
        }
        public Word(string s)
        {
            Name = s;
        }
        public static Word operator +(Word s1, Word s2)
        {
            s1.Name += s2.Name;
            return s1;
        }

        public static Word operator +(Word s1, Character s2)
        {
            s1.Name += s2.Name;
            return s1;
        }

        public static Word operator -(Word s1, int i)
        {
            s1.Name = s1.Name.Remove(s1.Length - 1);
            return s1;
        }
    }

    public class Character
    {
        public string Name { get; set; }
        public bool IsUsed { get; set; }
        public int XKoef { get; set; } = 1;
        public int CKoef { get; set; } = 1;
        public int Index { get; set; } = 1;
        public Character(string name)
        {
            Name = name;
            IsUsed = false;
            
        }

        public static Character operator +(Character s1, Character s2)
        {
            s1.Name += s2.Name;
            return s1;
        }
    }
}