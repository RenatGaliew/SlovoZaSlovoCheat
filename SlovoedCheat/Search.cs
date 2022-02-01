using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlovoedCheat
{
    public class SearchTask
    {
        public event EventHandler<List<Word>> MethodOK;
        public Character[][] Matrix;
        public List<Word> _words { get; private set; }

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
            if (currentWord.Length > 9) return;
            var current = Matrix[currentX][currentY];
            if (current.IsUsed) return;

            current.IsUsed = true;
            current.Index = currentWord.Length + 1;
            currentWord.Stoimost = currentWord.Stoimost + current.XKoef * current.Index;
            currentWord += current;
            if (current.CKoef != 1)
            {
                currentWord.CCoef *= current.CKoef;
            }

            if (currentWord.Length > 2)
            {
                _words.Add(new Word(currentWord.Name)
                {
                    Stoimost = currentWord.Stoimost * currentWord.CCoef
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
            MethodOK?.Invoke(this, _words);
        }

        public void Clear()
        {
            _words.Clear();
            GC.Collect();
        }
    }
}
