using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Catel.Collections;
using Catel.Data;
using Catel.Logging;
using Catel.MVVM;
using Catel.Runtime;

namespace SlovoedCheat
{
    public class MainViewModel : ViewModelBase
    {
        public static readonly PropertyData MatrixViewProperty = RegisterProperty<MainViewModel, ObservableCollection<Character>>(x => x.MatrixView);
        public static readonly PropertyData SelectedWordProperty = RegisterProperty<MainViewModel, Word>(x => x.SelectedWord);

        public ObservableCollection<Character> MatrixView
        {
            get => GetValue<ObservableCollection<Character>>(MatrixViewProperty);
            set => SetValue(MatrixViewProperty, value);
        }

        public ObservableCollection<Word> Words { get; private set; }
        public Character[][] Matrix;
        public Command SearchCommand { get; set; }
        public Command StopCommand { get; set; }
        private CancellationTokenSource ct;
        private List<string> dict;
        private SearchTask S { get; set; }

        public Word SelectedWord
        {
            get => GetValue<Word>(SelectedWordProperty);
            set => SetValue(SelectedWordProperty, value);
        }

        public MainViewModel()
        {
            Words = new ObservableCollection<Word>();
            SearchCommand = new Command(Search);
            StopCommand = new Command(() =>
            {
                ct.Cancel();
            });
            var str24= "у1ритьимыр3ынвнйродые4брбол2е"; // уриновый
            var str = "глуим1э4схтииаео2ащнв3втняалс";
            //str = str.ToUpper();
            Matrix = new[]
            {
                new Character[5],
                new Character[5],
                new Character[5],
                new Character[5],
                new Character[5],
            };
            int index = 0;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    var name = str[index].ToString();
                    var X = 0;
                    var t = int.TryParse(name, out X);
                    if (t)
                    {
                        index++; 
                        name = str[index].ToString();
                        if (X is 4)
                        {
                            Matrix[i][j] = new Character(name)
                            {
                                CKoef = 1,
                                XKoef = 3
                            };
                        }
                        else if(X is 3)
                        {
                            Matrix[i][j] = new Character(name)
                            {
                                CKoef = 1,
                                XKoef = 2
                            };
                        }
                        else if (X is 1)
                        {
                            Matrix[i][j] = new Character(name)
                            {
                                CKoef = 2,
                                XKoef = 1
                            };
                        }
                        else if (X is 2)
                        {
                            Matrix[i][j] = new Character(name)
                            {
                                CKoef = 3,
                                XKoef = 1
                            };
                        }
                    }
                    else
                    {
                        Matrix[i][j] = new Character(name)
                        {
                            CKoef = 1,
                            XKoef = 1
                        };
                    }
                    index++;
                }
            }

            /*Matrix = new[]
            {
                new[] {new Character("") {CKoef=1,XKoef=1}, new Character("") {CKoef=1,XKoef=3},new Character("") {CKoef=1,XKoef=1},new Character("") {CKoef=3,XKoef=1},new Character("") {CKoef=1,XKoef=1}},
                new[] {new Character("") {CKoef=1,XKoef=1}, new Character("") {CKoef=1,XKoef=2},new Character("") {CKoef=1,XKoef=1},new Character("") {CKoef=2,XKoef=1},new Character("") {CKoef=1,XKoef=1}},
                new[] {new Character("") {CKoef=1,XKoef=1}, new Character("") {CKoef=1,XKoef=1},new Character("") {CKoef=1,XKoef=1},new Character("") {CKoef=1,XKoef=1},new Character("") {CKoef=1,XKoef=1}},
                new[] {new Character("") {CKoef=1,XKoef=1}, new Character("") {CKoef=1,XKoef=1},new Character("") {CKoef=1,XKoef=1},new Character("") {CKoef=1,XKoef=1},new Character("") {CKoef=1,XKoef=1}},
                new[] {new Character("") {CKoef=1,XKoef=1}, new Character("") {CKoef=1,XKoef=1},new Character("") {CKoef=1,XKoef=1},new Character("") {CKoef=1,XKoef=1},new Character("") {CKoef=1,XKoef=1}},
            };*/
            
            MatrixView = new ObservableCollection<Character>();
            foreach (var value in Matrix)
            {
                foreach (var character in value)
                {
                    MatrixView.Add(character);
                }
            }

            dict = new List<string>();
            dict.AddRange(File.ReadAllLines("russian.txt"));
        }

        protected override void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
        }

        private void OnMethodOK(object? sender, List<Word> words)
        {
            var d = dict;
            words.Sort(Comparer);
            var noDupl = words.Distinct(new DistinctItemComparer());
            var dictStoim = noDupl.ToDictionary(word => word.Name, word => word.Stoimost);
            //var wordsString = words.Select(x => x.Name);
            var ttt = dictStoim.Keys.Intersect(d).Select(x => new Word(x)
            {
                Stoimost = dictStoim[x]
            });
            var t = ttt.ToList();

            //var wordsList = dictStoim.Keys.Intersect(d).Select(x => new Word(x));
            App.Current.Dispatcher.Invoke(() =>
            {
                Words.AddRange(ttt);
            });

            S.Clear();
        }


        private int Comparer(Word arg1, Word arg2)
        {
            if (arg1.Stoimost == arg2.Stoimost) return 0;
            return arg1.Stoimost < arg2.Stoimost ? 1 : -1;
        }

        private void Search()
        {
            ct = new CancellationTokenSource();
            
            Task.Run(() =>
            {
                S = new SearchTask(Matrix);
                S.MethodOK += OnMethodOK;
                S.Search(ct);
            }, ct.Token);
        }
    }

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
