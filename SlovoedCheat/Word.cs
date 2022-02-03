using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Documents;
using System.Windows.Media;
using Catel.Data;
using Catel.MVVM;
using Brush = System.Drawing.Brush;

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
        public List<Point> Points { get; set; }

        public Word()
        {
            Points = new List<Point>();
            Name = "";
        }
        public Word(string s)
        {
            Points = new List<Point>();
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

    public class Character : ViewModelBase
    {
        public static readonly PropertyData BrushProperty = RegisterProperty<Character, SolidColorBrush>(x => x.Brush);
        public string Name { get; set; }
        public SolidColorBrush Brush
        {
            get => GetValue<SolidColorBrush>(BrushProperty);
            set => SetValue(BrushProperty, value);
        }
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