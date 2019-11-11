using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Net.Http;
using Microsoft.Win32;

namespace Sonic_3_A.I.R_Animation_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string RefrenceFolderPath { get; set; } = "";

        public bool ShowFullFrame = false;
        public bool ShowFrameBorder = false;
        public bool ShowAlignmentLines = false;
        private int Zoom = 1;

        public Sonic3AIRAnim CurrentAnimation;
        public Sonic3AIRAnim.Sonic3AIRFrame CurrentFrame;

        public Sonic3AIRAnim CurrentRefrenceAnimation;
        public Sonic3AIRAnim.Sonic3AIRFrame CurrentRefrenceFrame;

        public SkiaSharp.SKBitmap CurrentSpriteSheet;
        public SkiaSharp.SKBitmap CurrentSpriteSheetFrame;
        public string CurrentSpriteSheetName;

        public SkiaSharp.SKBitmap CurrentRefrenceSpriteSheet;
        public SkiaSharp.SKBitmap CurrentRefrenceSpriteSheetFrame;
        public string CurrentRefrenceSpriteSheetName;

        public bool AllowUpdate = true;

        public static string nL = Environment.NewLine;

        private double _RefrenceOpacity = 100;

        public double RefrenceOpacity
        {
            get
            {
                return _RefrenceOpacity;
            }

            set
            {

                _RefrenceOpacity = value * 0.01;
            }
        }
        public int SelectedFrameIndex { get; set; } = -1;

        public MainWindow()
        {
            InitializeComponent();
            UpdateUI();

            //ShowFullImageItem.IsChecked = true;
            //ShowFrameBorderItem.IsChecked = true;

        }

        private void UpdateUI()
        {
            bool isAnimationLoaded = CurrentAnimation != null;
            OpenMenuItem.IsEnabled = true;
            SaveAsMenuItem.IsEnabled = isAnimationLoaded;
            SaveMenuItem.IsEnabled = isAnimationLoaded;
            NewMenuItem.IsEnabled = false;
            ExitMenuItem.IsEnabled = true;
            UnloadMenuItem.IsEnabled = isAnimationLoaded;

            //CanvasImageOpacity = (isAnimationLoaded ? 1.0 : 0);
            //CanvasRefrenceImageOpacity = (isAnimationLoaded ? 1.0 : 0);

            if (isAnimationLoaded)
            {
                EntriesList.ItemsSource = null;
                EntriesList.ItemsSource = CurrentAnimation.FrameList;
                FrameViewer.ItemsSource = null;
                FrameViewer.ItemsSource = CurrentAnimation.FrameList;
                UpdateFrameViewAndValues();
            }

        }


        #region File Tab Methods

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog()
            {
                Filter = "JSON Files (*.json) | *.json"
            };
            if (fileDialog.ShowDialog() == true)
            {
                CurrentAnimation = new Sonic3AIRAnim(new FileInfo(fileDialog.FileName));
                UpdateRecentsDropDown(fileDialog.FileName);
                UpdateUI();
            }
        }

        private void SaveFileAsEvent(object sender, RoutedEventArgs e)
        {
            FileSaveAs();
        }

        private void FileSaveAs()
        {
            SaveFileDialog fileDialog = new SaveFileDialog()
            {
                Filter = "JSON Files (*.json) | *.json"
            };
            if (fileDialog.ShowDialog() == true)
            {
                CurrentAnimation.Save(fileDialog.FileName);
            }
        }

        private void FileSave()
        {
            CurrentAnimation.Save();
        }

        private void SaveFileEvent(object sender, RoutedEventArgs e)
        {
            if (CurrentAnimation.FileLocation == "") FileSaveAs();
            else FileSave();
        }

        private void ExitEditor(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        #endregion

        #region Frame List Button Methods

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentAnimation != null)
            {
                if (CurrentFrame != null && EntriesList.SelectedItem != null)
                {
                    int index = EntriesList.SelectedIndex;
                    if (index != 0)
                    {
                        CurrentAnimation.FrameList.Move(index, index - 1);
                        UpdateUI();
                        EntriesList.SelectedIndex = index - 1;
                        FrameViewer.SelectedIndex = index - 1;
                    }
                }
            }
        }


        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentAnimation != null)
            {
                if (CurrentFrame != null && EntriesList.SelectedItem != null)
                {
                    int index = EntriesList.SelectedIndex;
                    if (index != EntriesList.Items.Count - 1)
                    {
                        CurrentAnimation.FrameList.Move(index, index + 1);
                        UpdateUI();
                        EntriesList.SelectedIndex = index + 1;
                        FrameViewer.SelectedIndex = index + 1;
                    }
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentAnimation != null)
            {
                if (CurrentFrame != null && EntriesList.SelectedItem != null)
                {
                    int index = EntriesList.SelectedIndex;
                    CurrentAnimation.FrameList.Insert(index, new Sonic3AIRAnim.Sonic3AIRFrame(CurrentAnimation.Directory));
                    UpdateUI();
                }
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentAnimation != null)
            {
                if (CurrentFrame != null && EntriesList.SelectedItem != null)
                {
                    int index = EntriesList.SelectedIndex;
                    if (MessageBox.Show($"Are you sure you want to remove \"{CurrentAnimation.FrameList[index].Name}\"?", "Remove Frame", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        CurrentAnimation.FrameList.RemoveAt(index);
                        UpdateUI();
                    }
                }
            }
        }


        #endregion

        #region Frame Updates

        #region Values

        public FrameValues UpdateRefrenceValues(bool NewFrame = false)
        {
            int img_width = (CurrentRefrenceSpriteSheet != null ? CurrentRefrenceSpriteSheet.Width : int.MaxValue);
            int img_height = (CurrentRefrenceSpriteSheet != null ? CurrentRefrenceSpriteSheet.Height : int.MaxValue);

            Rect area = GetCOBRAF(CurrentRefrenceFrame.X, CurrentRefrenceFrame.Y, CurrentRefrenceFrame.Width, CurrentRefrenceFrame.Height, img_width, img_height);
            return new FrameValues(CurrentRefrenceFrame.X, CurrentRefrenceFrame.Y, CurrentRefrenceFrame.Width, CurrentRefrenceFrame.Height, CurrentRefrenceFrame.CenterX, CurrentRefrenceFrame.CenterY, area);
        }

        public FrameValues UpdateValues(bool NewFrame = false)
        {
            int img_width = (CurrentSpriteSheet != null ? CurrentSpriteSheet.Width : int.MaxValue);
            int img_height = (CurrentSpriteSheet != null ? CurrentSpriteSheet.Height : int.MaxValue);
            Rect area;
            if (!NewFrame) area = GetCOBRAF((int)XNUD.Value, (int)YNUD.Value, (int)WidthNUD.Value, (int)HeightNUD.Value, img_width, img_height);
            else area = GetCOBRAF(CurrentFrame.X, CurrentFrame.Y, CurrentFrame.Width, CurrentFrame.Height, img_width, img_height);

            int x = (int)area.X;
            int y = (int)area.Y;
            int width = (int)area.Width;
            int height = (int)area.Height;
            int centerX;
            int centerY;



            if (NewFrame)
            {
                CurrentFrame.X = x;
                CurrentFrame.Y = y;
                CurrentFrame.Width = width;
                CurrentFrame.Height = height;

                centerX = CurrentFrame.CenterX;
                centerY = CurrentFrame.CenterY;

                AllowUpdate = false;

                YNUD.Value = y;
                XNUD.Value = x;
                WidthNUD.Value = width;
                HeightNUD.Value = height;
                CenterXNUD.Value = centerX;
                CenterYNUD.Value = centerY;

                FileTextBox.Text = CurrentFrame.File;
                NameTextBox.Text = CurrentFrame.Name;

                AllowUpdate = true;

            }
            else
            {
                centerX = (int)CenterXNUD.Value;
                centerY = (int)CenterYNUD.Value;

                CurrentFrame.X = x;
                CurrentFrame.Y = y;
                CurrentFrame.Width = width;
                CurrentFrame.Height = height;

                CurrentFrame.CenterX = centerX;
                CurrentFrame.CenterY = centerY;

                CurrentFrame.File = FileTextBox.Text;
                CurrentFrame.Name = NameTextBox.Text;

            }

            //Max Values
            YNUD.MaxValue = (int)(img_height - area.Height);
            XNUD.MaxValue = (int)(img_width - area.Width);
            WidthNUD.MaxValue = (int)(img_width - area.X);
            HeightNUD.MaxValue = (int)(img_height - area.Y);

            return new FrameValues(x, y, width, height, centerX, centerY, area);
        }

        public void UpdateValues()
        {
            AllowUpdate = false;
            NameTextBox.Text = CurrentFrame.Name;
            FileTextBox.Text = CurrentFrame.File;

            YNUD.MaxValue = CurrentFrame.Y;
            XNUD.MaxValue = CurrentFrame.X;
            WidthNUD.MaxValue = CurrentFrame.Width;
            HeightNUD.MaxValue = CurrentFrame.Height;

            YNUD.Value = CurrentFrame.Y;
            XNUD.Value = CurrentFrame.X;
            WidthNUD.Value = CurrentFrame.Width;
            HeightNUD.Value = CurrentFrame.Height;

            CenterXNUD.Value = CurrentFrame.CenterX;
            CenterYNUD.Value = CurrentFrame.CenterY;
            AllowUpdate = true;
        }

        public void VoidValues()
        {
            AllowUpdate = false;
            NameTextBox.Text = "";
            FileTextBox.Text = "";

            YNUD.MaxValue = 0;
            XNUD.MaxValue = 0;
            WidthNUD.MaxValue = 0;
            HeightNUD.MaxValue = 0;

            YNUD.Value = 0;
            XNUD.Value = 0;
            WidthNUD.Value = 0;
            HeightNUD.Value = 0;

            CenterXNUD.Value = 0;
            CenterYNUD.Value = 0;
            AllowUpdate = true;
        }

        private Rect GetCOBRAF(int x, int y, int width, int height, int imageWidth, int imageHeight)
        {
            if (x + width > imageWidth)
            {
                int tempX = imageWidth - x - width;
                if (tempX < 0)
                {
                    if (-tempX > imageWidth) width = imageWidth;
                    else
                    {
                        width = -tempX;
                    }
                    x = 0;
                }
                else x = 0;
            }
            if (y + height > imageHeight)
            {
                int tempY = imageHeight - y - height;
                if (tempY < 0)
                {
                    if (-tempY > imageHeight) height = imageHeight;
                    else
                    {
                        height = -tempY;
                    }
                    y = 0;
                }
                else y = 0;
            }
            return new Rect(x, y, width, height);
        }

        public class FrameValues
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int CenterX { get; set; }
            public int CenterY { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public Rect Area { get; set; }

            public FrameValues(int x, int y, int width, int height, int centerX, int centerY, Rect area)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
                CenterX = centerX;
                CenterY = centerY;
                Area = area;

            }
        }

        #endregion

        private void UpdateFrameViewAndValues(bool NewFrame = false, bool ZoomChanged = false)
        {
            if (CurrentAnimation != null && CurrentFrame != null)
            {
                AllowListViewUpdate = false;
                EntriesList.SelectedIndex = CurrentAnimation.FrameList.IndexOf(CurrentFrame);
                FrameViewer.SelectedIndex = CurrentAnimation.FrameList.IndexOf(CurrentFrame);
                AllowListViewUpdate = true;

                UpdateOpacitySlider();

                UpdateFrameImage();

                if (CurrentSpriteSheet != null)
                {
                    UpdateCanvasView(NewFrame);
                }
                else UpdateValues();
            }
            else VoidValues();

        }

        private void UpdateOpacitySlider()
        {
            if (CurrentRefrenceAnimation != null) CurrentRefrenceFrame = CurrentRefrenceAnimation.FrameList.Where(x => x.Name == CurrentFrame.Name).FirstOrDefault();

            if (CurrentRefrenceAnimation != null && CurrentRefrenceFrame != null)
            {
                if (OpacitySlider.IsEnabled == false) OpacitySlider.IsEnabled = true;
                if (RefrenceOpacityLabel.IsEnabled == false) RefrenceOpacityLabel.IsEnabled = true;

            }
            else
            {
                OpacitySlider.IsEnabled = false;
                RefrenceOpacityLabel.IsEnabled = false;
            }
        }

        public void UpdateCanvasView(bool NewFrame = false)
        {
            if (CurrentAnimation != null && CurrentFrame != null) Draw();
            if (CurrentRefrenceAnimation != null && CurrentRefrenceFrame != null && CurrentRefrenceSpriteSheet != null) DrawRefrence();



            CanvasView.InvalidateVisual();


            void Draw()
            {
                FrameValues values = UpdateValues(NewFrame);
                if (values.Width != 0 && values.Height != 0)
                {
                    System.Drawing.Bitmap sourceImage = SkiaSharp.Views.Desktop.Extensions.ToBitmap(CurrentSpriteSheet);
                    System.Drawing.Bitmap croppedImg = (System.Drawing.Bitmap)BitmapExtensions.CropImage(sourceImage, new System.Drawing.Rectangle(values.X, values.Y, values.Width, values.Height));
                    BitmapImage croppedBitmapImage = (BitmapImage)BitmapExtensions.ToWpfBitmap(croppedImg);
                    CurrentSpriteSheetFrame = SkiaSharp.Views.WPF.WPFExtensions.ToSKBitmap(croppedBitmapImage);

                    sourceImage.Dispose();
                    sourceImage = null;

                    croppedImg.Dispose();
                    croppedImg = null;
                }



            }

            void DrawRefrence()
            {
                FrameValues refValues = UpdateRefrenceValues(NewFrame);
                if (refValues.Width != 0 && refValues.Height != 0 && CurrentRefrenceSpriteSheet != null)
                {
                    System.Drawing.Bitmap sourceImage = SkiaSharp.Views.Desktop.Extensions.ToBitmap(CurrentRefrenceSpriteSheet);
                    System.Drawing.Bitmap croppedImg = (System.Drawing.Bitmap)BitmapExtensions.CropImage(sourceImage, new System.Drawing.Rectangle(refValues.X, refValues.Y, refValues.Width, refValues.Height));
                    BitmapImage croppedBitmapImage = (BitmapImage)BitmapExtensions.ToWpfBitmap(croppedImg);
                    CurrentRefrenceSpriteSheetFrame = SkiaSharp.Views.WPF.WPFExtensions.ToSKBitmap(croppedBitmapImage);

                    sourceImage.Dispose();
                    sourceImage = null;

                    croppedImg.Dispose();
                    croppedImg = null;
                }
            }


        }

        private void UpdateFrameImage()
        {
            MainImage();
            if (CurrentRefrenceAnimation != null && CurrentRefrenceFrame != null) RefrenceImage();

            void MainImage()
            {
                if (CurrentSpriteSheetName != CurrentFrame.File)
                {
                    Dispose();
                    CurrentSpriteSheetName = "";
                    try
                    {
                        if (File.Exists($"{CurrentAnimation.Directory}\\{CurrentFrame.File}"))
                        {
                            string fileName = $"{CurrentAnimation.Directory}\\{CurrentFrame.File}";
                            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                            System.Drawing.Bitmap img = new System.Drawing.Bitmap(fileStream);
                            fileStream.Close();
                            if (img.Palette.Entries.Length > 0)
                            {
                                var color = img.Palette.Entries[0];
                                string hex = HexConverter(color);
                                img.MakeTransparent(color);
                            }

                            BitmapImage bitmapImage = (BitmapImage)BitmapExtensions.ToWpfBitmap(img);
                            CurrentSpriteSheet = SkiaSharp.Views.WPF.WPFExtensions.ToSKBitmap(bitmapImage);

                            CurrentSpriteSheetName = CurrentFrame.File;

                            img.Dispose();
                            img = null;

                        }
                        else
                        {
                            Dispose();
                        }

                    }
                    catch
                    {
                        Dispose();
                    }
                }

                void Dispose()
                {
                    if (CurrentSpriteSheet != null) CurrentSpriteSheet.Dispose();
                    CurrentSpriteSheet = null;
                }
            }
            void RefrenceImage()
            {
                if (CurrentRefrenceSpriteSheetName != CurrentRefrenceFrame.File)
                {
                    Dispose();
                    CurrentRefrenceSpriteSheetName = "";
                    try
                    {
                        if (File.Exists($"{CurrentRefrenceAnimation.Directory}\\{CurrentRefrenceFrame.File}"))
                        {
                            string fileName = $"{CurrentRefrenceAnimation.Directory}\\{CurrentRefrenceFrame.File}";
                            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                            System.Drawing.Bitmap img = new System.Drawing.Bitmap(fileStream);
                            fileStream.Close();
                            if (img.Palette.Entries.Length > 0)
                            {
                                var color = img.Palette.Entries[0];
                                string hex = HexConverter(color);
                                img.MakeTransparent(color);
                            }

                            //img = (System.Drawing.Bitmap)BitmapExtensions.SetImageOpacity(img, (float)OpacitySlider.Value);

                            BitmapImage bitmapImage = (BitmapImage)BitmapExtensions.ToWpfBitmap(img);
                            CurrentRefrenceSpriteSheet = SkiaSharp.Views.WPF.WPFExtensions.ToSKBitmap(bitmapImage);

                            CurrentRefrenceSpriteSheetName = CurrentRefrenceFrame.File;

                            img.Dispose();
                            img = null;

                        }
                        else
                        {
                            Dispose();
                        }

                    }
                    catch
                    {
                        Dispose();
                    }
                }

                void Dispose()
                {
                    if (CurrentRefrenceSpriteSheet != null) CurrentRefrenceSpriteSheet.Dispose();
                    CurrentRefrenceSpriteSheet = null;
                }
            }



        }

        private static String HexConverter(System.Drawing.Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        #endregion

        #region Events

        private bool AllowListViewUpdate = true;
        private void EntriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EntriesList.SelectedItem != null && AllowListViewUpdate)
            {
                AllowListViewUpdate = false;
                FrameViewer.SelectedItem = EntriesList.SelectedItem;
                EntriesList.ScrollIntoView(EntriesList.SelectedItem);
                FrameViewer.ScrollIntoView(EntriesList.SelectedItem);
                CurrentFrame = EntriesList.SelectedItem as Sonic3AIRAnim.Sonic3AIRFrame;
                if (CurrentRefrenceAnimation != null) CurrentRefrenceFrame = CurrentRefrenceAnimation.FrameList.Where(x => x.Name == CurrentFrame.Name).FirstOrDefault();
                UpdateFrameViewAndValues(true);
                AllowListViewUpdate = true;
            }
        }
        private void FrameViewer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FrameViewer.SelectedItem != null && AllowListViewUpdate)
            {
                AllowListViewUpdate = false;
                EntriesList.SelectedItem = FrameViewer.SelectedItem;
                EntriesList.ScrollIntoView(EntriesList.SelectedItem);
                FrameViewer.ScrollIntoView(EntriesList.SelectedItem);
                CurrentFrame = EntriesList.SelectedItem as Sonic3AIRAnim.Sonic3AIRFrame;
                if (CurrentRefrenceAnimation != null) CurrentRefrenceFrame = CurrentRefrenceAnimation.FrameList.Where(x => x.Name == CurrentFrame.Name).FirstOrDefault();
                UpdateFrameViewAndValues(true);
                AllowListViewUpdate = true;
            }
        }
        private void NUD_ValueChanged(object sender, ControlLib.ValueChangedEventArgs e)
        {
            if (AllowUpdate) UpdateUI();
        }

        private void FrameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AllowUpdate)
            {
                CurrentFrame.File = FileTextBox.Text;
                CurrentFrame.Name = NameTextBox.Text;
                UpdateUI();
            }
        }
        private void NUD_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ControlLib.NumericUpDown objSend = (sender as ControlLib.NumericUpDown);
            if (objSend != null)
            {
                if (e.Delta >= 1 && objSend.MaxValue > objSend.Value)
                {
                    objSend.Value += 1;
                }
                else if (e.Delta <= -1 && objSend.MinValue < objSend.Value)
                {
                    objSend.Value -= 1;
                }
            }
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateFrameViewAndValues();
        }
        private void CanvasView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta >= 1)
            {
                if (Zoom <= 5) Zoom += 1;

            }
            else if (e.Delta <= -1)
            {
                if (Zoom >= 2) Zoom -= 1;
            }


            UpdateFrameViewAndValues(false, true);
        }

        #endregion

        #region Custom Classes

        public class Sonic3AIRAnim
        {
            public List<Sonic3AIRFrame> FrameList = new List<Sonic3AIRFrame>();
            public string Directory;
            public string FileLocation;



            public Sonic3AIRAnim(FileInfo file)
            {
                Directory = file.Directory.FullName;
                FileLocation = file.FullName;
                string data = File.ReadAllText(file.FullName);
                JToken stuff = JRaw.Parse(data);

                foreach (var child in stuff.Children())
                {
                    string _name = "";
                    string _file = "";
                    Rect _rect = new Rect();
                    int? _center_x = null;
                    int? _center_y = null;

                    if (child.HasValues)
                    {
                        _name = child.Path;
                        foreach (JProperty content in child.Children().Children())
                        {
                            if (content.HasValues)
                            {
                                if (content.Name == "File")
                                {
                                    _file = content.Value.ToString();
                                }
                                else if (content.Name == "Rect")
                                {
                                    List<int> Rect = new List<int>();
                                    foreach(string item in content.Value.ToString().Split(',').ToList())
                                    {
                                        Rect.Add(int.Parse(item));
                                    }
                                    _rect = new Rect(Rect[0], Rect[1], Rect[2], Rect[3]);
                                }
                                else if (content.Name == "Center")
                                {
                                    List<int> Center = new List<int>();
                                    foreach (string item in content.Value.ToString().Split(',').ToList())
                                    {
                                        Center.Add(int.Parse(item));
                                    }
                                    _center_x = Center[0];
                                    _center_y = Center[1];
                                }
                            }
                        }
                        if (_center_x != null && _center_y != null) FrameList.Add(new Sonic3AIRFrame(_name, _file, (int)_rect.X, (int)_rect.Y, (int)_rect.Width, (int)_rect.Height, (int)_center_x, (int)_center_y, Directory));
                        else FrameList.Add(new Sonic3AIRFrame(_name, _file, (int)_rect.X, (int)_rect.Y, (int)_rect.Width, (int)_rect.Height, 0, 0, Directory));
                    }
 
                }
            }

            public Sonic3AIRAnim(string _directory, string _fileLocation)
            {
                Directory = _directory;
                FileLocation = _fileLocation;
                FrameList.Add(new Sonic3AIRFrame(Directory));
            }

            public void Save(string SaveAsLocation = "")
            {
                string bc = "}";
                string bo = "{";
                string q = "\"";

                string output = "";
                output += "{";
                int count = FrameList.Count() -  1;
                foreach (Sonic3AIRFrame frame in FrameList)
                {
                    int index = FrameList.IndexOf(frame);
                    output += nL;
                    output += $"\t{q}{frame.Name}{q}:  {bo} {q}File{q}: {q}{frame.File}{q}, {q}Rect{q}: {q}{frame.X},{frame.Y},{frame.Width},{frame.Height}{q}, {q}Center{q}: {q}{frame.CenterX},{frame.CenterY}{q} {bc}";
                    if (index != count) output += ",";
                }
                output += nL;
                output += "}";
                if (SaveAsLocation != "")
                {
                    File.WriteAllText(SaveAsLocation, output);
                    FileLocation = SaveAsLocation;
                }
                else File.WriteAllText(FileLocation, output);

            }

            public class Sonic3AIRFrame
            {
                public string Name;
                public string File;
                public string Directory;
                public int X;
                public int Y;
                public int Width;
                public int Height;
                public int CenterX;
                public int CenterY;

                public override string ToString()
                {
                    return Name;
                }

                public ImageSource FrameImage { get { return GetImage(); } }

                public ImageSource GetImage()
                {
                    BitmapSource bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgr24, null, new byte[3] { 0, 0, 0 }, 3);
                    BitmapImage img = new BitmapImage();
                    string path = $"{Directory}\\{File}";

                    img.BeginInit();
                    img.UriSource = new Uri(path);
                    img.EndInit();

                    if (Width > 0 && Height > 0 && img != null)
                    {
                        try
                        {
                            bitmap = new CroppedBitmap(img,
                            new System.Windows.Int32Rect()
                            {
                                X = X,
                                Y = Y,
                                Width = Width,
                                Height = Height
                            });
                        }
                        catch (ArgumentException)
                        {
                        }
                    }



                    ImageSource result = bitmap;
                    return result;
                }

                public Sonic3AIRFrame(string _name, string _file, int _x, int _y, int _width, int _height, int _centerX, int _centerY, string _directory)
                {
                    Name = _name;
                    File = _file;
                    Directory = _directory;
                    X = _x;
                    Y = _y;
                    Width = _width;
                    Height = _height;
                    CenterX = _centerX;
                    CenterY = _centerY;
                }

                public Sonic3AIRFrame(string _directory)
                {
                    Name = "New Frame";
                    File = "";
                    Directory = _directory;
                    X = 0;
                    Y = 0;
                    Width = 0;
                    Height = 0;
                    CenterX = 0;
                    CenterY = 0;

                }


            }
        }






        #endregion

        private void ShowFrameBorderItem_Checked(object sender, RoutedEventArgs e)
        {
            if (ShowFrameBorderItem.IsChecked) ShowFrameBorder = true;
            else ShowFrameBorder = false;

            UpdateFrameViewAndValues();
        }

        private void ShowFullImageItem_Checked(object sender, RoutedEventArgs e)
        {
            if (ShowFullImageItem.IsChecked) ShowFullFrame = true;
            else ShowFullFrame = false;

            UpdateFrameViewAndValues();
        }

        private void NUD_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.IsUp)
            {
                ControlLib.NumericUpDown objSend = (sender as ControlLib.NumericUpDown);
                if (objSend != null)
                {
                    if (e.Key == Key.Up && objSend.MaxValue > objSend.Value)
                    {
                        objSend.Value += 1;
                    }
                    else if (e.Key == Key.Down && objSend.MinValue < objSend.Value)
                    {
                        objSend.Value -= 1;
                    }
                }
            }

        }

        private void AnimationScroller_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AllowListViewUpdate)
            {
                AllowListViewUpdate = false;
                if (AnimationScroller.Value == 3) UpdateFrameIndex(false);
                if (AnimationScroller.Value == 1) UpdateFrameIndex(true);
                AnimationScroller.Value = 2;
                AllowListViewUpdate = true;
            }

        }

        public void UpdateFrameIndex(bool subtract = false)
        {
            bool DidUpdateHappen = false;
            if (CurrentAnimation != null)
            {
                if (CurrentFrame != null && EntriesList.SelectedItem != null)
                {
                    int index = FrameViewer.SelectedIndex;
                    if (subtract && index != 0)
                    {
                        AllowListViewUpdate = false;
                        DidUpdateHappen = true;
                        EntriesList.SelectedIndex = index - 1;
                        FrameViewer.SelectedIndex = index - 1;

                    }
                    else if (index + 1 < CurrentAnimation.FrameList.Count())
                    {
                        AllowListViewUpdate = false;
                        DidUpdateHappen = true;
                        EntriesList.SelectedIndex = index + 1;
                        FrameViewer.SelectedIndex = index + 1;
                    }


                    if (DidUpdateHappen)
                    {
                        CurrentFrame = FrameViewer.SelectedItem as Sonic3AIRAnim.Sonic3AIRFrame;
                        FrameViewer.ScrollIntoView(FrameViewer.SelectedItem);
                        EntriesList.ScrollIntoView(EntriesList.SelectedItem);
                        UpdateFrameViewAndValues(true);
                        AllowListViewUpdate = true;
                    }
                }
            }

        }

        #region Recent Files (Lifted from Maniac Editor)

        private void OpenRecentsMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            RefreshRecentFiles(Properties.Settings.Default.RecentItems);
        }

        private void RecentItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as System.Windows.Controls.MenuItem;
            string selectedFile = menuItem.Tag.ToString();
            var recentFiles = Properties.Settings.Default.RecentItems;
            if (File.Exists(selectedFile))
            {
                AddItemToRecentsList(selectedFile);
                CurrentAnimation = new Sonic3AIRAnim(new FileInfo(selectedFile));
                UpdateUI();
            }
            else
            {
                recentFiles.Remove(selectedFile);
                RefreshRecentFiles(recentFiles);

            }
            Properties.Settings.Default.Save();


        }

        private void AddItemToRecentsList(string item)
        {
            try
            {
                var mySettings = Properties.Settings.Default;
                var dataDirectories = mySettings.RecentItems;

                if (dataDirectories == null)
                {
                    dataDirectories = new System.Collections.Specialized.StringCollection();
                    mySettings.RecentItems = dataDirectories;
                }

                if (dataDirectories.Contains(item))
                {
                    dataDirectories.Remove(item);
                }

                if (dataDirectories.Count >= 10)
                {
                    for (int i = 9; i < dataDirectories.Count; i++)
                    {
                        dataDirectories.RemoveAt(i);
                    }
                }

                dataDirectories.Insert(0, item);

                mySettings.Save();

                RefreshRecentFiles(dataDirectories);


            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write("Failed to add data folder to recent list: " + ex);
            }
        }

        private IList<MenuItem> RecentItems = new List<MenuItem>();

        public void UpdateRecentsDropDown(string itemToAdd = "")
        {
            if (itemToAdd != "") AddItemToRecentsList(itemToAdd);
            RefreshRecentFiles(Properties.Settings.Default.RecentItems);
        }

        public void RefreshRecentFiles(System.Collections.Specialized.StringCollection recentDataDirectories)
        {
            if (Properties.Settings.Default.RecentItems?.Count > 0)
            {
                NoRecentFiles.Visibility = Visibility.Collapsed;
                CleanUpRecentList();

                var startRecentItems = OpenRecentsMenuItem.Items.IndexOf(NoRecentFiles);

                foreach (var dataDirectory in recentDataDirectories)
                {
                    RecentItems.Add(CreateDataDirectoryMenuLink(dataDirectory));
                }



                foreach (MenuItem menuItem in RecentItems.Reverse())
                {
                    OpenRecentsMenuItem.Items.Insert(startRecentItems, menuItem);
                }
            }
            else
            {
                NoRecentFiles.Visibility = Visibility.Visible;
            }



        }

        private MenuItem CreateDataDirectoryMenuLink(string target)
        {
            MenuItem newItem = new MenuItem();
            newItem.Header = target;
            newItem.Tag = target;
            newItem.Click += RecentItem_Click;
            return newItem;
        }

        private void CleanUpRecentList()
        {
            foreach (var menuItem in RecentItems)
            {
                menuItem.Click -= RecentItem_Click;
                OpenRecentsMenuItem.Items.Remove(menuItem);
            }

            List<string> ItemsForRemoval = new List<string>();
            List<string> ItemsWithoutDuplicates = new List<string>();

            for (int i = 0; i < Properties.Settings.Default.RecentItems.Count; i++)
            {
                if (ItemsWithoutDuplicates.Contains(Properties.Settings.Default.RecentItems[i]))
                {
                    ItemsForRemoval.Add(Properties.Settings.Default.RecentItems[i]);
                }
                else
                {
                    ItemsWithoutDuplicates.Add(Properties.Settings.Default.RecentItems[i]);
                    if (File.Exists(Properties.Settings.Default.RecentItems[i])) continue;
                    else ItemsForRemoval.Add(Properties.Settings.Default.RecentItems[i]);
                }

            }
            foreach (string item in ItemsForRemoval)
            {
                Properties.Settings.Default.RecentItems.Remove(item);
            }

            Properties.Settings.Default.RecentItems.Cast<string>().Distinct().ToList();

            RecentItems.Clear();
        }



        #endregion

        private void ShowCenterAlignmentLines_Checked(object sender, RoutedEventArgs e)
        {
            if (ShowCenterAlignmentLines.IsChecked) ShowAlignmentLines = true;
            else ShowAlignmentLines = false;

            UpdateFrameViewAndValues();
        }

        private void CanvasImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateFrameViewAndValues();
        }

        private void UpdateRefrenceCheckBoxes(object sender)
        {
            RefrenceSonicButton.IsChecked = false;
            RefrenceSonicSnowboardButton.IsChecked = false;
            RefrenceSonicPeelOutButton.IsChecked = false;
            RefrenceSonicDropDashButton.IsChecked = false;
            RefrenceSuperSonicButton.IsChecked = false;
            RefrenceTailsButton.IsChecked = false;
            RefrenceKnucklesButton.IsChecked = false;
            RefrenceBSSonicButton.IsChecked = false;
            RefrenceBSTailsButton.IsChecked = false;
            RefrenceBSKnucklesButton.IsChecked = false;
            RefrenceHUDButton.IsChecked = false;
            RefrenceNothingButton.IsChecked = false;

            (sender as MenuItem).IsChecked = true;
        }

        private void RefrenceAnimationButton_Click(object sender, RoutedEventArgs e)
        {

            string item = "";
            if (sender.Equals(RefrenceSonicButton)) item = "character_sonic.json";
            else if (sender.Equals(RefrenceSonicSnowboardButton)) item = "character_sonic_snowboarding.json";
            else if (sender.Equals(RefrenceSonicPeelOutButton)) item = "character_sonic_peelout.json";
            else if (sender.Equals(RefrenceSonicDropDashButton)) item = "character_sonic_dropdash.json";
            else if (sender.Equals(RefrenceSuperSonicButton)) item = "character_supersonic.json";
            else if (sender.Equals(RefrenceTailsButton)) item = "character_tails.json";
            else if (sender.Equals(RefrenceKnucklesButton)) item = "character_knuckles.json";
            else if (sender.Equals(RefrenceBSSonicButton)) item = "bluesphere_sonic.json";
            else if (sender.Equals(RefrenceBSTailsButton)) item = "bluesphere_tails.json";
            else if (sender.Equals(RefrenceBSKnucklesButton)) item = "bluesphere_knuckles.json";
            else if (sender.Equals(RefrenceHUDButton)) item = "hud_sprites.json";
            else if (sender.Equals(RefrenceNothingButton)) item = "NULL";
            else item = "NULL";


            if (item == "NULL")
            {
                CurrentRefrenceAnimation = null;
                UpdateFrameViewAndValues();
            }
            else
            {
                string result = System.IO.Path.Combine(RefrenceFolderPath, item);

                if (File.Exists(result)) CurrentRefrenceAnimation = new Sonic3AIRAnim(new FileInfo(result));

                UpdateFrameViewAndValues();
            }

            UpdateRefrenceCheckBoxes(sender);
            CanvasView.InvalidateVisual();


        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RefrenceOpacity = OpacitySlider.Value;
            if (CanvasView != null) CanvasView.InvalidateVisual();
        }

        private void UnloadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CurrentFrame = null;
            CurrentAnimation = null;
            CurrentSpriteSheet = null;
            FrameViewer.ItemsSource = null;
            EntriesList.ItemsSource = null;
            UpdateUI();

        }

        private void CanvasView_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {      
            if (CurrentSpriteSheet != null || CurrentSpriteSheetFrame != null)
            {

                var info = e.Info;
                var canvas = e.Surface.Canvas;

                canvas.Scale(Zoom);

                float width = info.Width / Zoom;
                float height = info.Height / Zoom;

                float width_half = (info.Width / 2) / Zoom;
                float height_half = (info.Height / 2) / Zoom;

                canvas.Clear(SkiaSharp.SKColors.Transparent);

                DrawSprite(canvas, width_half, height_half, width, height);
                if (CurrentRefrenceSpriteSheet != null && CurrentRefrenceSpriteSheetFrame != null && CurrentRefrenceAnimation != null && CurrentRefrenceFrame != null) DrawRefrenceSprite(canvas, width_half, height_half, width, height);
            }
        }


        private void DrawSprite(SkiaSharp.SKCanvas canvas, float width_half, float height_half, float width, float height)
        {
            int frame_x = (int)XNUD.Value;
            int frame_y = (int)YNUD.Value;

            int frame_center_x = (int)CenterXNUD.Value;
            int frame_center_y = (int)CenterYNUD.Value;

            int frame_width = (int)WidthNUD.Value;
            int frame_height = (int)HeightNUD.Value;


            float img_center_x = width_half - frame_center_x;
            float img_center_y = height_half - frame_center_y;

            float img_full_center_x = width_half - frame_x - frame_center_x;
            float img_full_center_y = height_half - frame_y - frame_center_y;

            float img_full_border_center_x = width_half - frame_center_x;
            float img_full_border_center_y = height_half - frame_center_y;

            float x;
            float y;
            float w;
            float h;

            float bx;
            float by;
            float bw;
            float bh;

            if (ShowFullFrame)
            {
                x = img_full_center_x;
                y = img_full_center_y;
                w = frame_width;
                h = frame_height;

                bx = img_full_border_center_x - 1;
                by = img_full_border_center_y - 1;
                bw = w + 1;
                bh = h + 1;
            }
            else
            {
                x = img_center_x;
                y = img_center_y;
                w = frame_width;
                h = frame_height;

                bx = x - 1;
                by = y - 1;
                bw = w + 1;
                bh = h + 1;
            }

            canvas.DrawBitmap((ShowFullFrame ? CurrentSpriteSheet : CurrentSpriteSheetFrame), new SkiaSharp.SKPoint(x, y));

            if (ShowFrameBorder)
            {
                SkiaSharp.SKPoint x1 = new SkiaSharp.SKPoint(bx, by);
                SkiaSharp.SKPoint x2 = new SkiaSharp.SKPoint(bx + bw, by);
                SkiaSharp.SKPoint y1 = new SkiaSharp.SKPoint(bx, by + bh);
                SkiaSharp.SKPoint y2 = new SkiaSharp.SKPoint(bx + bw, by + bh);

                canvas.DrawLine(x1, x2, new SkiaSharp.SKPaint() { Color = SkiaSharp.SKColors.Black });
                canvas.DrawLine(y1, y2, new SkiaSharp.SKPaint() { Color = SkiaSharp.SKColors.Black });
                canvas.DrawLine(x1, y1, new SkiaSharp.SKPaint() { Color = SkiaSharp.SKColors.Black });
                canvas.DrawLine(x2, y2, new SkiaSharp.SKPaint() { Color = SkiaSharp.SKColors.Black });
            }

            if (ShowAlignmentLines)
            {
                SkiaSharp.SKPoint x1 = new SkiaSharp.SKPoint(0, height_half);
                SkiaSharp.SKPoint y1 = new SkiaSharp.SKPoint(width, height_half);
                SkiaSharp.SKPoint x2 = new SkiaSharp.SKPoint(width_half, 0);
                SkiaSharp.SKPoint y2 = new SkiaSharp.SKPoint(width_half, height);



                canvas.DrawLine(x1, y1, new SkiaSharp.SKPaint() { Color = SkiaSharp.SKColors.Red });
                canvas.DrawLine(x2, y2, new SkiaSharp.SKPaint() { Color = SkiaSharp.SKColors.Red });
            }
        }
        private void DrawRefrenceSprite(SkiaSharp.SKCanvas canvas, float width_half, float height_half, float width, float height)
        {
            int frame_x = CurrentRefrenceFrame.X;
            int frame_y = CurrentRefrenceFrame.Y;

            int frame_center_x = CurrentRefrenceFrame.CenterX;
            int frame_center_y = CurrentRefrenceFrame.CenterY;

            int frame_width = CurrentRefrenceFrame.Width;
            int frame_height = CurrentRefrenceFrame.Height;


            float img_center_x = width_half - frame_center_x;
            float img_center_y = height_half - frame_center_y;

            float img_full_center_x = width_half - frame_x - frame_center_x;
            float img_full_center_y = height_half - frame_y - frame_center_y;

            float img_full_border_center_x = width_half - frame_center_x;
            float img_full_border_center_y = height_half - frame_center_y;

            float x;
            float y;
            float w;
            float h;

            float bx;
            float by;
            float bw;
            float bh;

            if (ShowFullFrame)
            {
                x = img_full_center_x;
                y = img_full_center_y;
                w = frame_width;
                h = frame_height;

                bx = img_full_border_center_x - 1;
                by = img_full_border_center_y - 1;
                bw = w + 1;
                bh = h + 1;
            }
            else
            {
                x = img_center_x;
                y = img_center_y;
                w = frame_width;
                h = frame_height;

                bx = x - 1;
                by = y - 1;
                bw = w + 1;
                bh = h + 1;
            }

            SkiaSharp.SKPaint paint = new SkiaSharp.SKPaint();

            using (var cf = SkiaSharp.SKColorFilter.CreateBlendMode(SkiaSharp.SKColors.White, SkiaSharp.SKBlendMode.DstIn))
            {
                byte value = (byte)(OpacitySlider.Value * 2.56);

                var transparency = SkiaSharp.SKColors.White.WithAlpha(value); // 127 => 50%
                paint.ColorFilter = cf;
                paint.Color = transparency;

                canvas.DrawBitmap((ShowFullFrame ? CurrentRefrenceSpriteSheet : CurrentRefrenceSpriteSheetFrame), new SkiaSharp.SKPoint(x, y), paint);

                if (ShowFrameBorder)
                {
                    SkiaSharp.SKPoint x1 = new SkiaSharp.SKPoint(bx, by);
                    SkiaSharp.SKPoint x2 = new SkiaSharp.SKPoint(bx + bw, by);
                    SkiaSharp.SKPoint y1 = new SkiaSharp.SKPoint(bx, by + bh);
                    SkiaSharp.SKPoint y2 = new SkiaSharp.SKPoint(bx + bw, by + bh);

                    canvas.DrawLine(x1, x2, new SkiaSharp.SKPaint() { Color = SkiaSharp.SKColors.Black });
                    canvas.DrawLine(y1, y2, new SkiaSharp.SKPaint() { Color = SkiaSharp.SKColors.Black });
                    canvas.DrawLine(x1, y1, new SkiaSharp.SKPaint() { Color = SkiaSharp.SKColors.Black });
                    canvas.DrawLine(x2, y2, new SkiaSharp.SKPaint() { Color = SkiaSharp.SKColors.Black });
                }

                if (ShowAlignmentLines)
                {
                    SkiaSharp.SKPoint x1 = new SkiaSharp.SKPoint(0, height_half);
                    SkiaSharp.SKPoint y1 = new SkiaSharp.SKPoint(width, height_half);
                    SkiaSharp.SKPoint x2 = new SkiaSharp.SKPoint(width_half, 0);
                    SkiaSharp.SKPoint y2 = new SkiaSharp.SKPoint(width_half, height);



                    canvas.DrawLine(x1, y1, new SkiaSharp.SKPaint() { Color = SkiaSharp.SKColors.Red });
                    canvas.DrawLine(x2, y2, new SkiaSharp.SKPaint() { Color = SkiaSharp.SKColors.Red });
                }

                paint.ColorFilter = null;

            }


        }

        private void SetRefrenecFolderPath_Click(object sender, RoutedEventArgs e)
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string ModManagerVersionsPath = System.IO.Path.Combine(appdata, @"Sonic3AIR_MM\air_versions");
            using (var fldrDlg = new FolderSelectDialog())
            {
                fldrDlg.Title = "Set Refrence Folder Path...";
                fldrDlg.InitialDirectory = ModManagerVersionsPath;
                if (fldrDlg.ShowDialog() == true)
                {
                    RefrenceFolderPath = fldrDlg.FileName;
                }
            }
        }
    }


}
