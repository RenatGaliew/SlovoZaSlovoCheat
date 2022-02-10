using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using Catel.Collections;
using Catel.Data;
using Catel.MVVM;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using Timer = System.Timers.Timer;

namespace SlovoedCheat
{
    public class MainViewModel : ViewModelBase
    {
        public static readonly PropertyData MatrixViewProperty = RegisterProperty<MainViewModel, ObservableCollection<Character>>(x => x.MatrixView);
        public static readonly PropertyData SelectedWordProperty = RegisterProperty<MainViewModel, Word>(x => x.SelectedWord);
        public static readonly PropertyData SourceTextProperty = RegisterProperty<MainViewModel, string>(x => x.SourceText);
        public static readonly PropertyData CurrentTimeProperty = RegisterProperty<MainViewModel, string>(x => x.CurrentTime);
        public static readonly PropertyData NewWordProperty = RegisterProperty<MainViewModel, string>(x => x.NewWord);

        public ObservableCollection<Character> MatrixView
        {
            get => GetValue<ObservableCollection<Character>>(MatrixViewProperty);
            set => SetValue(MatrixViewProperty, value);
        }
        public string CurrentTime
        {
            get => GetValue<string>(CurrentTimeProperty);
            set => SetValue(CurrentTimeProperty, value);
        }

        public ObservableCollection<Word> Words { get; private set; }
        private Character[][] _matrix;
        public Command SearchCommand { get; }
        public Command StartTimeCommand { get; }
        public Command StopCommand { get; }
        public Command StartMouseMoveCommand { get; }
        public Command GetMatrixMoveCommand { get; }
        public Command AddWordCommand { get; }
        public Command<Word> RemoveWordCommand { get; }
        public Command DeleteWordsCommand { get; }
        private CancellationTokenSource _cancellationTokenSourceSearch;
        private CancellationTokenSource _cancellationTokenSourceMove;
        private SearchTask S { get; set; }
        private Timer T { get; set; }
        private WondowInputMouse WindowInput { get; set; }

        public Word SelectedWord
        {
            get => GetValue<Word>(SelectedWordProperty);
            set => SetValue(SelectedWordProperty, value);
        }
        
        public string SourceText
        {
            get => GetValue<string>(SourceTextProperty);
            set => SetValue(SourceTextProperty, value);
        }
        
        public string NewWord
        {
            get => GetValue<string>(NewWordProperty);
            set => SetValue(NewWordProperty, value);
        }
        
        public DateTime EndTime { get; set; }

        static void DoClickMouse(int mouseButton)
        {
            var input = new INPUT()
            {
                dwType = 0, // Mouse input
                mi = new MOUSEINPUT() { dwFlags = mouseButton }
            };

            if (SendInput(1, input, Marshal.SizeOf(input)) == 0)
            {
                throw new Exception();
            }
        }

        public MainViewModel()
        {
            Words = new ObservableCollection<Word>();
            SearchCommand = new Command(Search);
            StopCommand = new Command(() =>
            {
                _cancellationTokenSourceSearch.Cancel();
            });
            StartTimeCommand = new Command(StartTimer);
            
            StartMouseMoveCommand = new Command(() =>
            {
                _cancellationTokenSourceMove = new CancellationTokenSource();
                StartMove(_cancellationTokenSourceMove.Token, Words.TakeLast(Words.Count - Words.IndexOf(SelectedWord)));
            });
            AddWordCommand = new Command(AddNewWord);
            RemoveWordCommand = new Command<Word>(RemoveWord);
            DeleteWordsCommand = new Command(DeleteWords);
            GetMatrixMoveCommand = new Command(GetImagesWords);
            WindowInput = new WondowInputMouse();
            WindowInput.Show();
            CurrentTime = "00:00";
            SourceText = "";
            S = new SearchTask(_matrix);
        }

        private void DeleteWords()
        {
            if (File.Exists("../../../ToDelete.txt"))
            {
                var toDelete = File.ReadAllLines("../../../ToDelete.txt");
                var words = File.ReadAllLines("russian.txt").ToList();
                foreach (var s in toDelete)
                {
                    words.Remove(s);
                }

                File.WriteAllLines("../../../russian.txt", words);
                File.Delete("../../../ToDelete.txt");
            }
        }

        private void RemoveWord(Word word)
        {
            if (!string.IsNullOrEmpty(word.Name))
            {
                Console.WriteLine();
                File.AppendAllLines("../../../ToDelete.txt", new List<string> { word.Name});
                DictionaryOfWords.Remove(word.Name);
                Words.Remove(word);
            }
        }

        private void AddNewWord()
        {
            if (!string.IsNullOrEmpty(NewWord))
            {
                //saveWord
                File.AppendAllLines("../../../russian.txt", new List<string> { NewWord });
                DictionaryOfWords.Add(NewWord);
                NewWord = "";
            }
        }

        private void GetImagesWords()
        {
            _matrix = new[]
            {
                new Character[5],
                new Character[5],
                new Character[5],
                new Character[5],
                new Character[5],
            };
            MatrixView = new ObservableCollection<Character>();

            GetWidthAndHeight(out var startPositionX, out var startPositionY, out var h, out var w);
            var size = new Size(w, h);
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    int xStart = startPositionX + w * j;
                    int yStart = startPositionY + h * i;
                    using (Bitmap bitmap = new Bitmap(w, h))
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.CopyFromScreen(new Point(xStart, yStart), new Point(0, 0), size);
                        }

                        for (int k = 0; k < w; k++)
                        {
                            for (int l = 0; l < h; l++)
                            {
                                var pixel = bitmap.GetPixel(k, l); 
                                var gray = (pixel.R + pixel.G + pixel.B)/3;
                                bitmap.SetPixel(k, l, Color.FromArgb(255, gray, gray, gray));
                            }
                        }
                        bitmap.Save($"{Guid.NewGuid():N}.jpg", ImageFormat.Jpeg);
                    }
                }
            }
        }

        private void GetWidthAndHeight(out int startPositionX, out int startPositionY, out int h, out int w)
        {
            startPositionX = (int) WindowInput.Left + (int) WindowInput.PolygonWords.Points[0].X;
            startPositionY = (int) WindowInput.Top + (int) WindowInput.PolygonWords.Points[0].Y;
            var height = (int) WindowInput.PolygonWords.Points[3].Y - (int) WindowInput.PolygonWords.Points[0].Y;
            var width = (int) WindowInput.PolygonWords.Points[1].X - (int) WindowInput.PolygonWords.Points[0].X;
            h = height / 5;
            w = width / 5;
        }

        private void StartTimer()
        {
            T?.Stop();
            T = new Timer(1000);
            T.Start();
            T.Elapsed += TOnElapsed;
            EndTime = DateTime.Now + TimeSpan.FromMinutes(2);
        }

        private void TOnElapsed(object sender, ElapsedEventArgs e)
        {
            var timeSpan = EndTime - DateTime.Now;
            CurrentTime = timeSpan.ToString(@"m\:ss");
            if (e.SignalTime >= EndTime)
            {
                _cancellationTokenSourceMove?.Cancel(); 
                T.Stop();
            }
        }

        private void StartMove(CancellationToken token, IEnumerable<Word> words)
        {
            GetWidthAndHeight(out var startPositionX, out var startPositionY, out var h, out var w);
            GetWidthAndHeightButton(out var xClickOk, out var yClickOk);

            WindowInput.Close();
            try
            {
                var position = System.Windows.Forms.Cursor.Position;
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(500, token);
                        foreach (var word in words.ToList())
                        {
                            if (Math.Abs(position.X - System.Windows.Forms.Cursor.Position.X) > 10 
                                || Math.Abs(position.Y - System.Windows.Forms.Cursor.Position.Y) > 10)
                                break;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                SelectedWord = word;
                            });
                            if (token.IsCancellationRequested) return;

                            int i = 0;
                            foreach (var wordPoint in word.Points)
                            {
                                if (Math.Abs(position.X - System.Windows.Forms.Cursor.Position.X) > 10
                                    || Math.Abs(position.Y - System.Windows.Forms.Cursor.Position.Y) > 10)
                                    break;

                                if (token.IsCancellationRequested) return;
                                int x = startPositionX + w * wordPoint.Y + (w / 2);
                                int y = startPositionY + h * wordPoint.X + (h / 2);
                                
                                System.Windows.Forms.Cursor.Position = new Point(x, y);
                                position = System.Windows.Forms.Cursor.Position;
                                await Task.Delay(100, token);
                                if (i == 0)
                                    DoClickMouse(0x2);
                                i++;
                            }
                            DoClickMouse(0x4);
                            if (i < 3)
                            {
                                System.Windows.Forms.Cursor.Position = new Point(xClickOk, yClickOk); 
                                position = System.Windows.Forms.Cursor.Position;
                                await Task.Delay(100, token);
                                DoClickMouse(0x2);
                                DoClickMouse(0x4);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }, token);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void GetWidthAndHeightButton(out int xClickOk, out int yClickOk)
        {
            var startPositionOkBtnX = (int)WindowInput.Left + (int)WindowInput.PolygonButton.Points[0].X;
            var startPositionOkBtnY = (int)WindowInput.Top + (int)WindowInput.PolygonButton.Points[0].Y;
            var heightOkBtn = (int)WindowInput.PolygonButton.Points[3].Y - (int)WindowInput.PolygonButton.Points[0].Y;
            var widthOkBtn = (int)WindowInput.PolygonButton.Points[1].X - (int)WindowInput.PolygonButton.Points[0].X;
            xClickOk = startPositionOkBtnX + widthOkBtn / 2;
            yClickOk = startPositionOkBtnY + heightOkBtn / 2;
        }

        protected override void OnPropertyChanged(AdvancedPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if(e.PropertyName == nameof(SelectedWord))
            {
                if (SelectedWord != null)
                {
                    foreach (var character in MatrixView)
                    {
                        character.Brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
                    }

                    int t = 255;
                    int tt = t / (SelectedWord.Points.Count + 1);
                    foreach (var selectedWordPoint in SelectedWord.Points)
                    {
                        MatrixView[selectedWordPoint.X * 5 + selectedWordPoint.Y].Brush =
                            new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte) t, 128, 255, 0));
                        t -= tt;
                    }
                }
            }
        }

        private void OnMethodOK(object sender, IEnumerable<Word> words)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Words.AddRange(words);
                SelectedWord = Words.First();
            });

            S.Clear();
        }

        private void Search()
        {
            _cancellationTokenSourceSearch = new CancellationTokenSource();
            var str = SourceText.Length == 0 ? "ларчйосьфетоажлсрьияидздя" : SourceText;

            _matrix = new[]
            {
                new Character[5],
                new Character[5],
                new Character[5],
                new Character[5],
                new Character[5],
            };
            SetMatrix(str);

            MatrixView = new ObservableCollection<Character>();

            foreach (var value in _matrix)
            {
                foreach (var character in value)
                {
                    character.Brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
                    MatrixView.Add(character);
                }
            }

            Words.Clear();

            Task.Run(() =>
            {
                S = new SearchTask(_matrix);
                S.MethodOK += OnMethodOK;
                S.Search(_cancellationTokenSourceSearch);
            }, _cancellationTokenSourceSearch.Token);
        }

        private void SetMatrix(string str)
        {
            int index = 0;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    var name = str[index].ToString();
                    var t = int.TryParse(name, out var x);
                    if (t)
                    {
                        index++;
                        name = str[index].ToString();
                        if (x is 4)
                        {
                            _matrix[i][j] = new Character(name)
                            {
                                CKoef = 1,
                                XKoef = 3
                            };
                        }
                        else if (x is 3)
                        {
                            _matrix[i][j] = new Character(name)
                            {
                                CKoef = 1,
                                XKoef = 2
                            };
                        }
                        else if (x is 1)
                        {
                            _matrix[i][j] = new Character(name)
                            {
                                CKoef = 2,
                                XKoef = 1
                            };
                        }
                        else if (x is 2)
                        {
                            _matrix[i][j] = new Character(name)
                            {
                                CKoef = 3,
                                XKoef = 1
                            };
                        }
                    }
                    else
                    {
                        _matrix[i][j] = new Character(name)
                        {
                            CKoef = 1,
                            XKoef = 1
                        };
                    }

                    index++;
                }
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint cInputs, INPUT input, int size);
    }
}
