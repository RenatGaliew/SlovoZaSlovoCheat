using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;
using Catel.Collections;
using Catel.Data;
using Catel.IoC;
using Catel.MVVM;
using Color = System.Drawing.Color;
using Form = System.Windows.Forms.Form;
using Point = System.Drawing.Point;
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
        public Character[][] Matrix;
        public Command SearchCommand { get; set; }
        public Command StartTimeCommand { get; set; }
        public Command StopCommand { get; set; }
        public Command StartMouseMoveCommand { get; set; }
        public Command GetMatrixMoveCommand { get; set; }
        public Command AddWordCommand { get; set; }
        public Command<Word> RemoveWordCommand { get; set; }
        public Command DeleteWordsCommand { get; }
        private CancellationTokenSource ct;
        private CancellationTokenSource ctInput;
        private List<string> dict;
        private SearchTask S { get; set; }
        private System.Timers.Timer t { get; set; }
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


        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint cInputs, INPUT input, int size);

        public static void ClickSomePoint()
        {
            // Set the cursor position
            System.Windows.Forms.Cursor.Position = new Point(20, 35);

            DoClickMouse(0x2); // Left mouse button down
            DoClickMouse(0x4); // Left mouse button up
        }

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
                ct.Cancel();
            });
            StartTimeCommand = new Command(() =>
            {
                StartTimer();
            });
            
            StartMouseMoveCommand = new Command(() =>
            {
                ctInput = new CancellationTokenSource();
                StartMove(ctInput.Token, Words);
            });
            AddWordCommand = new Command(AddNewWord);
            RemoveWordCommand = new Command<Word>(RemoveWord);
            DeleteWordsCommand = new Command(DeleteWords);
            var str24= "ларчйосьфетоажлсрьияидздя"; // уриновый
            GetMatrixMoveCommand = new Command(Execute);
            dict = new List<string>();
            dict.AddRange(File.ReadAllLines("russian.txt"));
            WindowInput = new WondowInputMouse();
            WindowInput.Show();
            CurrentTime = "00:00";
            SourceText = "";
            
        }

        private void DeleteWords()
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

        private void RemoveWord(Word word)
        {
            if (!string.IsNullOrEmpty(word.Name))
            {
                Console.WriteLine();
                File.AppendAllLines("../../../ToDelete.txt", new List<string> { word.Name});
                dict.Remove(word.Name);
                Words.Remove(word);
            }
        }

        private void AddNewWord()
        {
            if (!string.IsNullOrEmpty(NewWord))
            {
                //saveWord
                File.AppendAllLines("../../../russian.txt", new List<string> { NewWord });
                dict.Add(NewWord);
                NewWord = "";
            }
        }

        private void Execute()
        {
            Matrix = new[]
            {
                new Character[5],
                new Character[5],
                new Character[5],
                new Character[5],
                new Character[5],
            };
            MatrixView = new ObservableCollection<Character>();

            var startPositionX = (int)WindowInput.Left + (int)WindowInput.PolygonWords.Points[0].X;
            var startPositionY = (int)WindowInput.Top + (int)WindowInput.PolygonWords.Points[0].Y;
            var height = (int)WindowInput.PolygonWords.Points[3].Y - (int)WindowInput.PolygonWords.Points[0].Y;
            var width = (int)WindowInput.PolygonWords.Points[1].X - (int)WindowInput.PolygonWords.Points[0].X;
            int h = (int)height / 5;
            int w = (int)width / 5;
            var size = new Size(w, h);
            Dictionary<int, int> dictColorChar = new Dictionary<int, int>();
            var t = 0;
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

                        var countGray = 0;
                        for (int k = 0; k < w; k++)
                        {
                            for (int l = 0; l < h; l++)
                            {
                                var pixel = bitmap.GetPixel(k, l);
                                //if (pixel.R == pixel.G && pixel.R == pixel.B && pixel.R != 255) 
                                //{ 
                                 //   countGray++;
                                   // bitmap.SetPixel(k, l, Color.Black);
                                //}
                               // else
                                //{
                                    var gray = (pixel.R + pixel.G + pixel.B)/3;
                                    bitmap.SetPixel(k, l, Color.FromArgb(255, gray, gray, gray));
                                //}
                            }
                        }/*

                        string s = "";
                        if (countGray == 344 || countGray == 338) s = "к";
                        if (countGray == 362) s = "о"; 
                        if (countGray == 412) s = "в";
                        if (countGray == 346 || countGray == 311) s = "е"; 
                        if (countGray == 291 || countGray == 295) s = "р";
                        if (countGray == 312 || countGray == 312 || countGray == 322 || countGray == 310) s = "а";
                        if (countGray == 486) s = "м";
                        if (countGray == 316) s = "л";
                        if (countGray == 202) s = "т";
                        if (countGray == 320) s = "ь";
                        if (countGray == 332) s = "н";
                        if (countGray == 385) s = "и";
                        if (countGray == 345) s = "п";
                        if (countGray == 321) s = "с";
                        */
                        //Matrix[i][j] = new Character(s);

                        //dictColorChar.Add(t, countGray);
                        //t++;
                        bitmap.Save($"{Guid.NewGuid():N}.jpg", ImageFormat.Jpeg);
                    }
                }
            }

            /*foreach (var value in Matrix)
            {
                foreach (var character in value)
                {
                    character.Brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
                    MatrixView.Add(character);
                }
            }*/


            int y = 0;
        }

        private void StartTimer()
        {
            t?.Stop();
            t = new Timer(1000);
            t.Start();
            t.Elapsed += TOnElapsed;
            EndTime = DateTime.Now + TimeSpan.FromMinutes(2);
        }

        private void TOnElapsed(object sender, ElapsedEventArgs e)
        {
            var timeSpan = EndTime - DateTime.Now;
            CurrentTime = timeSpan.ToString(@"m\:ss");
            if (e.SignalTime >= EndTime)
            {
                ctInput?.Cancel(); 
                t.Stop();
            }
        }

        private void Callback(object? state)
        {
            
        }

        private void StartMove(CancellationToken token, IReadOnlyList<Word> words)
        {
            var startPositionX = (int)WindowInput.Left + (int)WindowInput.PolygonWords.Points[0].X;
            var startPositionY = (int)WindowInput.Top + (int)WindowInput.PolygonWords.Points[0].Y;
            var height = (int)WindowInput.PolygonWords.Points[3].Y - (int)WindowInput.PolygonWords.Points[0].Y;
            var width = (int)WindowInput.PolygonWords.Points[1].X - (int)WindowInput.PolygonWords.Points[0].X;
            int h = (int)height / 5;
            int w = (int)width / 5;

            var startPositionOKBTNX = (int)WindowInput.Left + (int)WindowInput.PolygonButton.Points[0].X;
            var startPositionOKBTNY = (int)WindowInput.Top + (int)WindowInput.PolygonButton.Points[0].Y;
            var heightOKBTN = (int)WindowInput.PolygonButton.Points[3].Y - (int)WindowInput.PolygonButton.Points[0].Y;
            var widthOKBTN = (int)WindowInput.PolygonButton.Points[1].X - (int)WindowInput.PolygonButton.Points[0].X;
            int xClickOK = startPositionOKBTNX+ widthOKBTN / 2;
            int yClickOK = startPositionOKBTNY + heightOKBTN / 2 ;

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
                            App.Current.Dispatcher.Invoke(() =>
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
                                System.Windows.Forms.Cursor.Position = new Point(xClickOK, yClickOK); 
                                position = System.Windows.Forms.Cursor.Position;
                                await Task.Delay(100, token);
                                DoClickMouse(0x2);
                                DoClickMouse(0x4);
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }, token);
            }
            catch (Exception)
            {
                
            }
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

        private void OnMethodOK(object? sender, List<Word> words)
        {
            var d = dict;
            words.Sort(Comparer);
            var noDupl = words.Distinct(new DistinctItemComparer());
            var dictStoim = noDupl.ToDictionary(word => word.Name, word => word);

            var ttt = dictStoim.Keys.Intersect(d).Select(x => new Word(x)
            {
                Stoimost = dictStoim[x].Stoimost,
                Points = dictStoim[x].Points
            });

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
            var str = SourceText.Length == 0 ? "ларчйосьфетоажлсрьияидздя" : SourceText;

            Matrix = new[]
            {
                new Character[5],
                new Character[5],
                new Character[5],
                new Character[5],
                new Character[5],
            };
            SetMatrix(str);

            MatrixView = new ObservableCollection<Character>();

            foreach (var value in Matrix)
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
                S = new SearchTask(Matrix);
                S.MethodOK += OnMethodOK;
                S.Search(ct);
            }, ct.Token);
        }

        private void SetMatrix(string str)
        {
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
                        else if (X is 3)
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
        }
    }
}
