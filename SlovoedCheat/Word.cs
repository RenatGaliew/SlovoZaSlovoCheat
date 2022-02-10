using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media;
using Catel.Data;
using Catel.MVVM;

namespace SlovoedCheat
{
    public class Word
    {
        public string Name { get; set; }
        public int Stoimost { get; set; }
        public int Stoimost2 { get; set; }

        public int Length => Name?.Length ?? 0;
        public int CCoef { get; set; } = 1;
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