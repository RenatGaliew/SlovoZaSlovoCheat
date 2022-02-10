using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace SlovoedCheat
{
    public static class DictionaryOfWords
    {
        public static List<string> Items { get; set; }

        public static void Remove(string wordName)
        {
            Items?.Remove(wordName);
        }

        public static void Add(string newWord)
        {
            Items?.Add(newWord);
        }
    }

    public class SearchTask
    {
        public event EventHandler<IEnumerable<Word>> MethodOK;
        private Character[][] Matrix;
        private readonly List<Word> _words;

        public SearchTask(Character[][] matrix)
        {
            Matrix = matrix;
            _words = new List<Word>();
        }

        private void NewMethodNoRecurse(int i, int j, CancellationToken ct)
        {
            NewMethod(new Word(),i , j, ct);
        } 
        
        private void NewMethod(Word currentWord, int currentX, int currentY, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;
            if (currentX >= 5 || currentY >= 5 || currentX < 0 || currentY < 0) return;
            if (currentWord.Length > 8) return;
            var current = Matrix[currentX][currentY];
            if (current.IsUsed) return;

            current.IsUsed = true;
            current.Index = currentWord.Length + 1;
            currentWord.Stoimost = currentWord.Stoimost + current.XKoef * current.Index;
            currentWord.Stoimost2 = currentWord.Stoimost2 + current.Index;
            currentWord.Points.Add(new Point(currentX, currentY));
            currentWord += current;
            if (current.CKoef != 1)
            {
                currentWord.CCoef *= current.CKoef;
            }

            if (currentWord.Length > 1)
            {
                _words.Add(new Word(currentWord.Name)
                {
                    Stoimost = currentWord.Stoimost * currentWord.CCoef,
                    Stoimost2 = currentWord.Stoimost2,
                    Points = new List<Point>(currentWord.Points)
                });
            }

            var y1 = currentY + 1;
            var ym1 = currentY - 1;
            var x1 = currentX + 1;
            var xm1 = currentX - 1;

            NewMethod(currentWord, currentX, y1, ct);
            NewMethod(currentWord, currentX, ym1, ct);
            NewMethod(currentWord, x1, currentY, ct);
            NewMethod(currentWord, xm1, currentY, ct);

            NewMethod(currentWord, x1, y1, ct);
            NewMethod(currentWord, xm1, y1, ct);
            NewMethod(currentWord, x1, ym1, ct);
            NewMethod(currentWord, xm1, ym1, ct);
            currentWord = currentWord - 1;
            currentWord.Stoimost = currentWord.Stoimost - current.XKoef * current.Index;
            currentWord.Stoimost2 = currentWord.Stoimost2 - current.Index;
            currentWord.Points.RemoveAt(currentWord.Length);
            if (current.CKoef != 1)
                currentWord.CCoef /= current.CKoef;
            current.IsUsed = false;
        }
        
        public void Search(CancellationTokenSource cancellationTokenSource)
        {
            for (int k = 0; k < 5; k++)
            {
                for (int l = 0; l < 5; l++)
                {
                    if(Matrix[k][l].Name is "ы" or "ъ" or "ь") continue;
                    NewMethodNoRecurse(k, l, cancellationTokenSource.Token);
                }
            }

            var d = DictionaryOfWords.Items;
            _words.Sort(Comparer);
            var noDupl = _words.Distinct(new DistinctItemComparer());
            var dictStoim = noDupl.ToDictionary(word => word.Name, word => word);

            var ttt = dictStoim.Keys.Intersect(d).Select(x => new Word(x)
            {
                Stoimost = dictStoim[x].Stoimost,
                Stoimost2 = dictStoim[x].Stoimost2,
                Points = dictStoim[x].Points
            });
            MethodOK?.Invoke(this, ttt);
        }

        private int Comparer(Word arg1, Word arg2)
        {
            if (arg1.Stoimost == arg2.Stoimost) return 0;
            return arg1.Stoimost < arg2.Stoimost ? 1 : -1;
        }

        public void Clear()
        {
            _words.Clear();
            GC.Collect();
        }
    }
}
