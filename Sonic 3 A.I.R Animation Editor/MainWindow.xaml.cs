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
        private int Zoom = 1;
        public Sonic3AIRAnim CurrentAnimation;
        public Sonic3AIRAnim.Sonic3AIRFrame CurrentFrame;
        public bool AllowUpdate = true;
        public BitmapImage CurrentFrameImage;
        public static string nL = Environment.NewLine;
        public MainWindow()
        {
            InitializeComponent();
            UpdateUI();
        }

        private void UpdateUI()
        {
            bool isAnimationLoaded = CurrentAnimation != null;
            OpenMenuItem.IsEnabled = true;
            SaveAsMenuItem.IsEnabled = isAnimationLoaded;
            SaveMenuItem.IsEnabled = isAnimationLoaded;
            NewMenuItem.IsEnabled = false;
            ExitMenuItem.IsEnabled = true;

            if (isAnimationLoaded)
            {
                EntriesList.ItemsSource = null;
                EntriesList.ItemsSource = CurrentAnimation.FrameList;
                UpdateFrameVisualizer();
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
                    CurrentAnimation.FrameList.Insert(index, new Sonic3AIRAnim.Sonic3AIRFrame());
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

        private void UpdateFrameVisualizer(bool NewFrame = false)
        {
            if (CurrentAnimation != null && CurrentFrame != null)
            {
                UpdateFrameImage();
                if (CurrentFrameImage != null)
                {
                    Rect area;

                    int x = 0;
                    int y = 0;
                    int width = 0;
                    int height = 0;
                    int centerX = 0;
                    int centerY = 0;

                    if (NewFrame)
                    {
                        area = GetCOBRAF(CurrentFrame.X, CurrentFrame.Y, CurrentFrame.Width, CurrentFrame.Height, CurrentFrameImage.PixelWidth, CurrentFrameImage.PixelHeight);

                        x = (int)area.X;
                        y = (int)area.Y;
                        width = (int)area.Width;
                        height = (int)area.Height;

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
                        area = GetCOBRAF((int)XNUD.Value, (int)YNUD.Value, (int)WidthNUD.Value, (int)HeightNUD.Value, CurrentFrameImage.PixelWidth, CurrentFrameImage.PixelHeight);

                        x = (int)area.X;
                        y = (int)area.Y;
                        width = (int)area.Width;
                        height = (int)area.Height;
                        centerX = (int)CenterXNUD.Value;
                        centerY = (int)CenterYNUD.Value;

                        CurrentFrame.X = x;
                        CurrentFrame.Y = y;
                        CurrentFrame.Width = width;
                        CurrentFrame.Height = height;

                        CurrentFrame.CenterX = centerX;
                        CurrentFrame.CenterY = centerY;

                    }

                    //Max Values
                    YNUD.MaxValue = (int)(CurrentFrameImage.PixelHeight - area.Height);
                    XNUD.MaxValue = (int)(CurrentFrameImage.PixelWidth - area.Width);
                    WidthNUD.MaxValue = (int)(CurrentFrameImage.PixelWidth - area.X);
                    HeightNUD.MaxValue = (int)(CurrentFrameImage.PixelHeight - area.Y);

                    //Update Image Display and Crop the Image
                    if (width != 0 && height != 0)
                    {
                        CroppedBitmap cropped = new CroppedBitmap(CurrentFrameImage, new Int32Rect(x, y, width, height));

                        Geomotry.Rect = new Rect(0, 0, cropped.Width, cropped.Height);

                        CanvasImage.Source = cropped;
                        ImageScale.ScaleX = Zoom;
                        ImageScale.ScaleY = Zoom;
                        CanvasImage.RenderTransformOrigin = new Point(0, 0);

                        double left = GetLeft(CanvasView.ActualWidth, 0, -(int)(CurrentFrame.CenterX), (int)CurrentFrame.Width, Zoom);
                        double top = GetTop(CanvasView.ActualHeight, 0, -(int)(CurrentFrame.CenterY), (int)CurrentFrame.Height, Zoom);
                        //double right = GetRight(left, (int)WidthNUD.Value, Zoom);
                        //double bottom = GetBottom(top, (int)HeightNUD.Value, Zoom);


                        System.Windows.Controls.Canvas.SetLeft(CanvasImage, left);
                        System.Windows.Controls.Canvas.SetTop(CanvasImage, top);
                        //System.Windows.Controls.Canvas.SetRight(CanvasImage, right);
                        //System.Windows.Controls.Canvas.SetBottom(CanvasImage, bottom);
                    }
                    else
                    {
                        CanvasImage.Source = null;
                    }
                }
                else
                {
                    AllowUpdate = false;
                    CanvasImage.Source = null;
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




            }
            else
            {
                AllowUpdate = false;
                CanvasImage.Source = null;
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

        }


        private void UpdateFrameImage()
        {
            try
            {
                CurrentFrameImage = new BitmapImage();
                CurrentFrameImage.BeginInit();
                CurrentFrameImage.UriSource = new Uri($"{CurrentAnimation.Directory}\\{CurrentFrame.File}");
                CurrentFrameImage.EndInit();
            }
            catch
            {
                CurrentFrameImage = null;
            }

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

        private double GetTop(double ViewHeight, int? SelectedFrameTop, int? SelectedFramePivotY, int? SelectedFrameHeight, double Zoom)
        {
            double Center = ViewHeight / 2.0;
            double FrameTop = SelectedFrameTop ?? 0;
            double FrameCenterY = SelectedFramePivotY ?? 0;
            double FrameHeight = SelectedFrameHeight ?? 0;
            return (int)(Center - FrameTop * Zoom) + FrameCenterY * Zoom;
        }

        private double GetLeft(double ViewWidth, int? SelectedFrameLeft, int? SelectedFramePivotX, int? SelectedFrameWidth, double Zoom)
        {
            double Center = ViewWidth / 2.0;
            double FrameLeft = SelectedFrameLeft ?? 0;
            double FrameCenterX = SelectedFramePivotX ?? 0;
            double FrameWidth = SelectedFrameWidth ?? 0;
            return (int)(Center - FrameLeft * Zoom) + FrameCenterX * Zoom;
        }

        public double GetRight(double SpriteLeft, int? SelectedFrameWidth, double Zoom)
        {
            double FrameWidth = SelectedFrameWidth ?? 0;
            return (int)(SpriteLeft + FrameWidth * Zoom);

        }

        public double GetBottom(double SpriteTop, int? SelectedFrameHeight, double Zoom)
        {
            double FrameHeight = SelectedFrameHeight ?? 0;
            return (int)(SpriteTop + FrameHeight * Zoom);

        }

        #endregion

        #region Events
        private void EntriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EntriesList.SelectedItem != null)
            {
                CurrentFrame = CurrentAnimation.FrameList.ElementAt(EntriesList.SelectedIndex);
                UpdateFrameVisualizer(true);
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
            UpdateFrameVisualizer();
        }

        private void CanvasView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta >= 1)
            {
                Zoom += 1;
            }
            else if (e.Delta <= -1)
            {
                if (Zoom >= 2) Zoom -= 1;

            }
            UpdateFrameVisualizer();
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
                        if (_center_x != null && _center_y != null) FrameList.Add(new Sonic3AIRFrame(_name, _file, (int)_rect.X, (int)_rect.Y, (int)_rect.Width, (int)_rect.Height, (int)_center_x, (int)_center_y));
                        else FrameList.Add(new Sonic3AIRFrame(_name, _file, (int)_rect.X, (int)_rect.Y, (int)_rect.Width, (int)_rect.Height));
                    }
 
                }
            }

            public Sonic3AIRAnim(string _directory, string _fileLocation)
            {
                Directory = _directory;
                FileLocation = _fileLocation;
                FrameList.Add(new Sonic3AIRFrame());
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

                public Sonic3AIRFrame(string _name, string _file, int _x, int _y, int _width, int _height, int _centerX = 0, int _centerY = 0)
                {
                    Name = _name;
                    File = _file;
                    X = _x;
                    Y = _y;
                    Width = _width;
                    Height = _height;
                    CenterX = _centerX;
                    CenterY = _centerY;
                }

                public Sonic3AIRFrame()
                {
                    Name = "New Frame";
                    File = "";
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


    }
}
