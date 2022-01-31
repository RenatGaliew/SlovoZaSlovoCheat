using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Catel.Collections;
using Catel.Data;
using Catel.Logging;
using Catel.MVVM;

namespace SlovoedCheat
{
    public class MainViewModel : ViewModelBase
    {
        public static readonly PropertyData ValueOfProgressProperty = RegisterProperty<MainViewModel, int>(x => x.ValueOfProgress);
        public static readonly PropertyData MaxValuesProperty = RegisterProperty<MainViewModel, int>(x => x.MaxValues);
        public static readonly PropertyData MatrixViewProperty = RegisterProperty<MainViewModel, ObservableCollection<Character>>(x => x.MatrixView);

        public ObservableCollection<Character> MatrixView
        {
            get => GetValue<ObservableCollection<Character>>(MatrixViewProperty);
            set => SetValue(MatrixViewProperty, value);
        }

        public int ValueOfProgress
        {
            get => GetValue<int>(ValueOfProgressProperty);
            set => SetValue(ValueOfProgressProperty, value);
        }

        public int MaxValues
        {
            get => GetValue<int>(MaxValuesProperty);
            set => SetValue(MaxValuesProperty, value);
        }

        public event EventHandler MethodOK;
        public ObservableCollection<Word> Words { get; private set; }
        public List<string> _words { get; private set; }
        public Character[][] Matrix;
        public Command SearchCommand { get; set; }
        public Command StopCommand { get; set; }
        private CancellationTokenSource ct;
        private List<string> dict;
        public MainViewModel()
        {
            Words = new ObservableCollection<Word>();
            _words = new List<string>();
            SearchCommand = new Command(Search);
            StopCommand = new Command(() =>
            {
                ct.Cancel();
            });
            Matrix = new[]
            {
                new[] {new Character("р"),new Character("и"),new Character("т"),new Character("р"),new Character("ы")},
                new[] {new Character("у"),new Character("х"),new Character("л"),new Character("т"),new Character("у")},
                new[] {new Character("е"),new Character("б"),new Character("е"),new Character("л"),new Character("и")},
                new[] {new Character("й"),new Character("д"),new Character("у"),new Character("к"),new Character("т")},
                new[] {new Character("г"),new Character("о"),new Character("р"),new Character("ы"),new Character("ь")},
            };
            dict = new List<string>();
            dict.AddRange(File.ReadAllLines("russian.txt"));
            MethodOK += OnMethodOK;
        }

        private void OnMethodOK(object? sender, EventArgs e)
        { 
            foreach (var word in _words)
            {
                if (dict.Contains(word))
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Words.Add(new Word()
                        {
                            Name = word
                        });
                    });
                }
            }
        }

        private void Search()
        {
            ct = new CancellationTokenSource();
            
            Task.Run(() =>
            {
                NewMethodNoRecurse(ct.Token);
            }, ct.Token);
        }

        private void NewMethodNoRecurse(CancellationToken ct)
        {
            NewMethod("", 4, 2, ct);
            MethodOK?.Invoke(this, EventArgs.Empty);
        }

        private void NewMethod(string currentWord, int currentX, int currentY, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;
            if (currentX >= 5 || currentY >= 5 || currentX < 0|| currentY < 0) return;
            if (currentWord.Length > 7) return;
            var current = Matrix[currentX][currentY];
            if (current.IsUsed) return;

            current.IsUsed = true;
            currentWord += current.Name;
            if(currentWord.Length > 5) _words.Add(currentWord);
            
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
            current.IsUsed = false;
        }
    }
}
