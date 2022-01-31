using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlovoedCheat
{
    public class Word
    {
        public string Name { get; set; }

        public Word()
        {
            
        }
    }

    public class Character
    {
        public string Name { get; set; }
        public bool IsUsed { get; set; }

        public Character(string name)
        {
            Name = name;
            IsUsed = false;
        }
    }
}
