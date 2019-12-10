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
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Net.Http;
using Microsoft.Win32;
using AIR_API;


namespace Sonic_3_AIR_Animation_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables
        private string RefrenceFolderPath { get; set; } = "";
        private int Zoom = 1;
        public bool AllowUpdate = true;
        public bool AllowColorUpdate = false;
        public static string nL = Environment.NewLine;
        #region Modes
        public bool ShowFullFrame = false;
        public bool ShowFrameBorder = false;
        public bool ShowAlignmentLines = false;
        public bool ShowSolidImageBackground = false;
        public bool ForceCenterFrame = false;
        public bool ShowOnlyRefrenceSection = false;
        #endregion

        #region Animations and Bitmaps
        public Animation CurrentAnimation;
        public Animation.Anim.Frame CurrentFrame;

        public Animation CurrentRefrenceAnimation;
        public Animation.Anim.Frame CurrentRefrenceFrame;

        public SkiaSharp.SKBitmap CurrentSpriteSheet;
        public SkiaSharp.SKBitmap CurrentSpriteSheetFrame;
        public string CurrentSpriteSheetName;

        public SkiaSharp.SKBitmap CurrentRefrenceSpriteSheet;
        public SkiaSharp.SKBitmap CurrentRefrenceSpriteSheetFrame;
        public string CurrentRefrenceSpriteSheetName;

        #endregion

        #region Colors
        public Color AlignmentLinesColor = Colors.Red;
        public Color ImgBG = Colors.White;
        public Color RefImgBG = Colors.White;
        public Color FrameBorder = Colors.Black;
        public Color RefFrameBorder = Colors.Black;
        public Color FrameBG = Colors.Transparent;
        public Color RefFrameBG = Colors.Transparent;
        #endregion

        #region Opacity
        private double _RefrenceOpacity = 100;
        public double RefrenceOpacity { get => _RefrenceOpacity; set => _RefrenceOpacity = value * 0.01; }
        #endregion

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            this.Title = string.Format("{0} {1}", this.Title, Program.Version);



            ImportPersonalColorPresets();
            SetupSavedConfigurations();
            UpdateUI();

        }

        #region Init

        private void SetupSavedConfigurations()
        {
            if (Properties.Settings.Default.UseDarkMode)
            {
                DarkModeButton.IsChecked = true;
            }
            else
            {
                DarkModeButton.IsChecked = false;
            }

            if (Properties.Settings.Default.LastRefrenceFolder != null)
            {
                RefrenceFolderPath = Properties.Settings.Default.LastRefrenceFolder;
            }
        }

        private void ImportPersonalColorPresets()
        {
            AlignmentLinesColor = Properties.Settings.Default.DefaultAlignmentLinesColor;
            ImgBG = Properties.Settings.Default.DefaultImageBG;
            RefImgBG = Properties.Settings.Default.DefaultImageBGRefrence;
            FrameBorder = Properties.Settings.Default.DefaultFrameBorder;
            RefFrameBorder = Properties.Settings.Default.DefaultFrameBorderRefrence;
            FrameBG = Properties.Settings.Default.DefaultFrameBackground;
            RefFrameBG = Properties.Settings.Default.DefaultFrameBackgroundRefrence;

            AlignmentLinesColorPick.SelectedColor = AlignmentLinesColor;
            ImgBGColorPick.SelectedColor = ImgBG;
            RefImgBGColorPick.SelectedColor = RefImgBG;
            ImgFrameBorderColorPick.SelectedColor = FrameBorder;
            RefImgFrameBorderColorPick.SelectedColor = RefFrameBorder;
            ImgFrameBGColorPick.SelectedColor = FrameBG;
            RefImgFrameBGColorPick.SelectedColor = RefFrameBG;

            AllowColorUpdate = true;

        }

        private void SavePersonalColorPresets()
        {
            Properties.Settings.Default.DefaultAlignmentLinesColor = AlignmentLinesColor;
            Properties.Settings.Default.DefaultImageBG = ImgBG;
            Properties.Settings.Default.DefaultImageBGRefrence = RefImgBG;
            Properties.Settings.Default.DefaultFrameBorder = FrameBorder;
            Properties.Settings.Default.DefaultFrameBorderRefrence = RefFrameBorder;
            Properties.Settings.Default.DefaultFrameBackground = FrameBG;
            Properties.Settings.Default.DefaultFrameBackgroundRefrence = RefFrameBG;

            Properties.Settings.Default.Save();
        }

        #endregion

        #region UI Refreshing

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
            else
            {

            }

        }

        #endregion

        #region File Tab Methods

        private void OpenFileEvent(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog()
            {
                Filter = "JSON Files (*.json) | *.json"
            };
            if (fileDialog.ShowDialog() == true)
            {
                CurrentAnimation = new Animation(new FileInfo(fileDialog.FileName));
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

        private void UnloadEvent(object sender, RoutedEventArgs e)
        {
            CurrentFrame = null;
            CurrentAnimation = null;
            CurrentSpriteSheetFrame.Dispose();
            CurrentSpriteSheetFrame = null;
            CurrentSpriteSheet.Dispose();
            CurrentSpriteSheet = null;
            FrameViewer.ItemsSource = null;
            EntriesList.ItemsSource = null;
            VoidValues();
            CanvasView.InvalidateVisual();
            UpdateUI();

        }

        private void ExitEditor(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DarkModeButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.UseDarkMode = !Properties.Settings.Default.UseDarkMode;
            Properties.Settings.Default.Save();

            if (Properties.Settings.Default.UseDarkMode)
            {
                App.ChangeSkin(Skin.Dark);
                DarkModeButton.IsChecked = true;
            }
            else
            {
                App.ChangeSkin(Skin.Light);
                DarkModeButton.IsChecked = false;
            }

            RefreshTheming();


            void RefreshTheming()
            {
                this.InvalidateVisual();
                foreach (UIElement element in Extensions.FindVisualChildren<UIElement>(this))
                {
                    element.InvalidateVisual();
                }
            }
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
                    CurrentAnimation.FrameList.Insert(index, new Animation.Anim.Frame(CurrentAnimation.Directory));
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

        #region Sprite Loading/Updating

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
            YNUD.Maximum = (int)(img_height - area.Height);
            XNUD.Maximum = (int)(img_width - area.Width);
            WidthNUD.Maximum = (int)(img_width - area.X);
            HeightNUD.Maximum = (int)(img_height - area.Y);

            return new FrameValues(x, y, width, height, centerX, centerY, area);
        }

        public void UpdateValues()
        {
            Program.Log.InfoFormat("Setting Editor Control Values from Animation Entry...");
            AllowUpdate = false;
            NameTextBox.Text = CurrentFrame.Name;
            FileTextBox.Text = CurrentFrame.File;

            YNUD.Maximum = CurrentFrame.Y;
            XNUD.Maximum = CurrentFrame.X;
            WidthNUD.Maximum = CurrentFrame.Width;
            HeightNUD.Maximum = CurrentFrame.Height;

            YNUD.Value = CurrentFrame.Y;
            XNUD.Value = CurrentFrame.X;
            WidthNUD.Value = CurrentFrame.Width;
            HeightNUD.Value = CurrentFrame.Height;

            CenterXNUD.Value = CurrentFrame.CenterX;
            CenterYNUD.Value = CurrentFrame.CenterY;

            CenterXNUD.Minimum = int.MinValue;
            CenterXNUD.Maximum = int.MaxValue;

            CenterYNUD.Minimum = int.MinValue;
            CenterYNUD.Maximum = int.MaxValue;

            AllowUpdate = true;
        }

        public void VoidValues()
        {
            Program.Log.InfoFormat("Voiding Editor Control Values...");
            AllowUpdate = false;
            NameTextBox.Text = "";
            FileTextBox.Text = "";

            YNUD.Maximum = 0;
            XNUD.Maximum = 0;
            WidthNUD.Maximum = 0;
            HeightNUD.Maximum = 0;

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

            private PropertyInfo[] _PropertyInfos = null;

            public override string ToString()
            {
                if (_PropertyInfos == null)
                    _PropertyInfos = this.GetType().GetProperties();

                var sb = new StringBuilder();

                foreach (var info in _PropertyInfos)
                {
                    var value = info.GetValue(this, null) ?? "(null)";
                    sb.AppendLine(info.Name + ": " + value.ToString());
                }

                return sb.ToString();
            }

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
            UpdateRefrenceSelect();

        }

        private void UpdateRefrenceSelect()
        {
            bool enabled = Directory.Exists(RefrenceFolderPath);
            StandardRefrence.IsEnabled = enabled;
            BSRefrence.IsEnabled = enabled;
            RefrenceHUDButton.IsEnabled = enabled;

            if (RefrenceFolderPath != "")
            {
                RefrenceFolderPathLabel.Text = RefrenceFolderPath;
            }
            else RefrenceFolderPathLabel.Text = "N/A";

            RefrenceFolderPathLabelTooltip.Text = RefrenceFolderPathLabel.Text;
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
                    try
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
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message + nL + nL + values.ToString());
                    }

                }



            }

            void DrawRefrence()
            {
                FrameValues refValues = UpdateRefrenceValues(NewFrame);
                if (refValues.Width != 0 && refValues.Height != 0 && CurrentRefrenceSpriteSheet != null)
                {
                    try
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
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message + nL + nL + refValues.ToString());
                    }

                }
            }


        }

        private void UpdateFrameImage()
        {
            MainImage();
            if (CurrentRefrenceAnimation != null && CurrentRefrenceFrame != null) RefrenceImage();

            void MainImage()
            {

                if (CurrentSpriteSheetName != CurrentFrame.File || CurrentSpriteSheet == null)
                {
                    Program.Log.InfoFormat("Collecting Main Image...");
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
                        Program.Log.InfoFormat("Main Image Collected!");
                    }
                    catch
                    {
                        Program.Log.InfoFormat("Main Image Collection Failed!");
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
                if (CurrentRefrenceSpriteSheetName != CurrentRefrenceFrame.File || CurrentRefrenceSpriteSheet == null)
                {
                    Dispose();
                    CurrentRefrenceSpriteSheetName = "";
                    try
                    {
                        Program.Log.InfoFormat("Collecting Refrence Image...");
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
                        Program.Log.InfoFormat("Refrence Image Collected!");
                    }
                    catch
                    {
                        Program.Log.InfoFormat("Refrence Image Collection Failed!");
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
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.Owner = this;
            about.ShowDialog();
        }

        private bool AllowListViewUpdate = true;
        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RefrenceOpacity = OpacitySlider.Value;
            if (CanvasView != null) CanvasView.InvalidateVisual();
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
        private void EntriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EntriesList.SelectedItem != null && AllowListViewUpdate)
            {
                AllowListViewUpdate = false;
                FrameViewer.SelectedItem = EntriesList.SelectedItem;
                EntriesList.ScrollIntoView(EntriesList.SelectedItem);
                FrameViewer.ScrollIntoView(EntriesList.SelectedItem);
                CurrentFrame = EntriesList.SelectedItem as Animation.Anim.Frame;
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
                CurrentFrame = EntriesList.SelectedItem as Animation.Anim.Frame;
                if (CurrentRefrenceAnimation != null) CurrentRefrenceFrame = CurrentRefrenceAnimation.FrameList.Where(x => x.Name == CurrentFrame.Name).FirstOrDefault();
                UpdateFrameViewAndValues(true);
                AllowListViewUpdate = true;
            }
        }
        private void NUD_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null) (sender as Xceed.Wpf.Toolkit.IntegerUpDown).Value = (int)e.OldValue;
            else if (AllowUpdate) UpdateUI();
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
                ChangeZoomLevel(true);

            }
            else if (e.Delta <= -1)
            {
                ChangeZoomLevel(false);
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
        private void ColorPick_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (AllowColorUpdate)
            {
                AlignmentLinesColor = AlignmentLinesColorPick.SelectedColor.Value;
                ImgBG = ImgBGColorPick.SelectedColor.Value;
                RefImgBG = RefImgBGColorPick.SelectedColor.Value;
                FrameBorder = ImgFrameBorderColorPick.SelectedColor.Value;
                RefFrameBorder = RefImgFrameBorderColorPick.SelectedColor.Value;
                FrameBG = ImgFrameBGColorPick.SelectedColor.Value;
                RefFrameBG = RefImgFrameBGColorPick.SelectedColor.Value;

                SavePersonalColorPresets();

                UpdateFrameViewAndValues();
            }

        }

        #endregion

        #region Frame Manipulation

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
                        CurrentFrame = FrameViewer.SelectedItem as Animation.Anim.Frame;
                        FrameViewer.ScrollIntoView(FrameViewer.SelectedItem);
                        EntriesList.ScrollIntoView(EntriesList.SelectedItem);
                        UpdateFrameViewAndValues(true);
                        AllowListViewUpdate = true;
                    }
                }
            }

        }

        #endregion

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
                CurrentAnimation = new Animation(new FileInfo(selectedFile));
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

                int index = 1;
                foreach (var dataDirectory in recentDataDirectories)
                {
                    int item_key;
                    if (index == 9) item_key = index;
                    else if (index >= 10) item_key = -1;
                    else item_key = index;
                    RecentItems.Add(CreateDataDirectoryMenuLink(dataDirectory, index));
                    index++;
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

        private MenuItem CreateDataDirectoryMenuLink(string target, int index)
        {
            MenuItem newItem = new MenuItem();
            newItem.Header = target;
            if (index != -1) newItem.InputGestureText = string.Format("Ctrl + {0}", index);
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

        #region Rendering
        private void CanvasView_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {

            var info = e.Info;
            var canvas = e.Surface.Canvas;

            canvas.Scale(Zoom);

            float width = info.Width / Zoom;
            float height = info.Height / Zoom;

            float width_half = width / 2;
            float height_half = height / 2;

            canvas.Clear(SkiaSharp.SKColors.Transparent);

            if (CurrentSpriteSheet != null || CurrentSpriteSheetFrame != null)
            {


                DrawSprite(canvas, width_half, height_half, width, height);
                if (CurrentRefrenceSpriteSheet != null && CurrentRefrenceSpriteSheetFrame != null && CurrentRefrenceAnimation != null && CurrentRefrenceFrame != null) DrawRefrenceSprite(canvas, width_half, height_half, width, height);

                if (ShowAlignmentLines)
                {
                    SkiaSharp.SKPoint x1 = new SkiaSharp.SKPoint(0, height_half);
                    SkiaSharp.SKPoint y1 = new SkiaSharp.SKPoint(width, height_half);
                    SkiaSharp.SKPoint x2 = new SkiaSharp.SKPoint(width_half, 0);
                    SkiaSharp.SKPoint y2 = new SkiaSharp.SKPoint(width_half, height);



                    canvas.DrawLine(x1, y1, new SkiaSharp.SKPaint() { Color = SkiaSharp.Views.WPF.WPFExtensions.ToSKColor(AlignmentLinesColor) });
                    canvas.DrawLine(x2, y2, new SkiaSharp.SKPaint() { Color = SkiaSharp.Views.WPF.WPFExtensions.ToSKColor(AlignmentLinesColor) });
                }
            }
        }
        private void DrawSprite(SkiaSharp.SKCanvas canvas, float width_half, float height_half, float width, float height)
        {
            int frame_x = (int)XNUD.Value;
            int frame_y = (int)YNUD.Value;

            int frame_width = (int)WidthNUD.Value;
            int frame_height = (int)HeightNUD.Value;

            int frame_center_x = (ForceCenterFrame ? frame_width / 2 : (int)CenterXNUD.Value);
            int frame_center_y = (ForceCenterFrame ? frame_height / 2 : (int)CenterYNUD.Value);

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



            if (ShowFullFrame)
            {
                x = img_full_center_x;
                y = img_full_center_y;
                w = frame_width;
                h = frame_height;

                bx = img_center_x;
                by = img_center_y;
            }
            else
            {
                x = img_center_x;
                y = img_center_y;
                w = frame_width;
                h = frame_height;

                bx = x;
                by = y;
            }



            if (ShowSolidImageBackground)
            {
                var paint = new SkiaSharp.SKPaint() { Color = SkiaSharp.Views.WPF.WPFExtensions.ToSKColor(ImgBG) };
                SkiaSharp.SKRect rect;
                if (ShowFullFrame && CurrentSpriteSheet != null) rect = new SkiaSharp.SKRect() { Top = y, Left = x, Size = new SkiaSharp.SKSize(CurrentSpriteSheet.Width, CurrentSpriteSheet.Height) };
                else rect = new SkiaSharp.SKRect() { Top = y, Left = x, Size = new SkiaSharp.SKSize(w, h) };

                canvas.DrawRect(rect, paint);
            }

            canvas.DrawBitmap((ShowFullFrame ? CurrentSpriteSheet : CurrentSpriteSheetFrame), new SkiaSharp.SKPoint(x, y));

            if (ShowFrameBorder)
            {
                SkiaSharp.SKPoint x1 = new SkiaSharp.SKPoint(bx, by);
                SkiaSharp.SKPoint x2 = new SkiaSharp.SKPoint(bx + w, by);
                SkiaSharp.SKPoint y1 = new SkiaSharp.SKPoint(bx, by + h);
                SkiaSharp.SKPoint y2 = new SkiaSharp.SKPoint(bx + w, by + h);

                canvas.DrawLine(x1, x2, new SkiaSharp.SKPaint() { Color = SkiaSharp.Views.WPF.WPFExtensions.ToSKColor(FrameBorder) });
                canvas.DrawLine(y1, y2, new SkiaSharp.SKPaint() { Color = SkiaSharp.Views.WPF.WPFExtensions.ToSKColor(FrameBorder) });
                canvas.DrawLine(x1, y1, new SkiaSharp.SKPaint() { Color = SkiaSharp.Views.WPF.WPFExtensions.ToSKColor(FrameBorder) });
                canvas.DrawLine(x2, y2, new SkiaSharp.SKPaint() { Color = SkiaSharp.Views.WPF.WPFExtensions.ToSKColor(FrameBorder) });

                var paint = new SkiaSharp.SKPaint();
                var transparency = SkiaSharp.Views.WPF.WPFExtensions.ToSKColor(FrameBG);
                paint.Color = transparency;

                canvas.DrawRect(new SkiaSharp.SKRect() { Top = by, Left = bx, Size = new SkiaSharp.SKSize(w, h) }, paint);
            }


        }
        private void DrawRefrenceSprite(SkiaSharp.SKCanvas canvas, float width_half, float height_half, float width, float height)
        {
            int frame_x = CurrentRefrenceFrame.X;
            int frame_y = CurrentRefrenceFrame.Y;



            int frame_width = CurrentRefrenceFrame.Width;
            int frame_height = CurrentRefrenceFrame.Height;

            int frame_center_x = (ForceCenterFrame ? frame_width / 2 : CurrentRefrenceFrame.CenterX);
            int frame_center_y = (ForceCenterFrame ? frame_height / 2 : CurrentRefrenceFrame.CenterY);


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

            bool isFullFrame = ShowFullFrame && !ShowOnlyRefrenceSection;

            if (isFullFrame)
            {
                x = img_full_center_x;
                y = img_full_center_y;
                w = frame_width;
                h = frame_height;

                bx = img_center_x;
                by = img_center_y;
            }
            else
            {
                x = img_center_x;
                y = img_center_y;
                w = frame_width;
                h = frame_height;

                bx = x;
                by = y;
            }

            SkiaSharp.SKRect rect;
            if (isFullFrame && CurrentRefrenceSpriteSheet != null) rect = new SkiaSharp.SKRect() { Top = y, Left = x, Size = new SkiaSharp.SKSize(CurrentRefrenceSpriteSheet.Width, CurrentRefrenceSpriteSheet.Height) };
            else rect = new SkiaSharp.SKRect() { Top = y, Left = x, Size = new SkiaSharp.SKSize(w, h) };

            SkiaSharp.SKPaint paint2 = new SkiaSharp.SKPaint();

            using (var cf = SkiaSharp.SKColorFilter.CreateBlendMode(SkiaSharp.SKColors.White, SkiaSharp.SKBlendMode.DstIn))
            {
                byte value = (byte)(OpacitySlider.Value * 2.56);

                var transparency = SkiaSharp.SKColors.White.WithAlpha(value); // 127 => 50%
                paint2.ColorFilter = cf;
                paint2.Color = transparency;

                if (ShowSolidImageBackground)
                {
                    var paint = new SkiaSharp.SKPaint() { Color = SkiaSharp.Views.WPF.WPFExtensions.ToSKColor(RefImgBG) };
                    canvas.DrawRect(rect, paint);
                }


                canvas.DrawBitmap((isFullFrame ? CurrentRefrenceSpriteSheet : CurrentRefrenceSpriteSheetFrame), new SkiaSharp.SKPoint(x, y), paint2);

                if (ShowFrameBorder)
                {
                    SkiaSharp.SKPoint x1 = new SkiaSharp.SKPoint(bx, by);
                    SkiaSharp.SKPoint x2 = new SkiaSharp.SKPoint(bx + w, by);
                    SkiaSharp.SKPoint y1 = new SkiaSharp.SKPoint(bx, by + h);
                    SkiaSharp.SKPoint y2 = new SkiaSharp.SKPoint(bx + w, by + h);

                    canvas.DrawLine(x1, x2, new SkiaSharp.SKPaint() { Color = SkiaSharp.Views.WPF.WPFExtensions.ToSKColor(RefFrameBorder) });
                    canvas.DrawLine(y1, y2, new SkiaSharp.SKPaint() { Color = SkiaSharp.Views.WPF.WPFExtensions.ToSKColor(RefFrameBorder) });
                    canvas.DrawLine(x1, y1, new SkiaSharp.SKPaint() { Color = SkiaSharp.Views.WPF.WPFExtensions.ToSKColor(RefFrameBorder) });
                    canvas.DrawLine(x2, y2, new SkiaSharp.SKPaint() { Color = SkiaSharp.Views.WPF.WPFExtensions.ToSKColor(RefFrameBorder) });

                    var paint = new SkiaSharp.SKPaint();
                    var transparency2 = SkiaSharp.Views.WPF.WPFExtensions.ToSKColor(RefFrameBG);
                    paint.Color = transparency2;

                    canvas.DrawRect(new SkiaSharp.SKRect() { Top = by, Left = bx, Size = new SkiaSharp.SKSize(w, h) }, paint);
                }

                paint2.ColorFilter = null;

            }


        }

        #endregion

        #region Toggles, Buttons, and Zoom Controls

        private void ShowSolidImageTransparencyBackground_Checked(object sender, RoutedEventArgs e)
        {
            if (ShowSolidImageTransparencyBackground.IsChecked) ShowSolidImageBackground = true;
            else ShowSolidImageBackground = false;

            ButtonShowBackground.IsChecked = ShowSolidImageBackground;

            UpdateFrameViewAndValues();
        }

        private void ChangeZoomLevel(bool increase)
        {
            if (increase)
            {
                if (Zoom <= 5) Zoom += 1;

            }
            else
            {
                if (Zoom >= 2) Zoom -= 1;
            }

            UpdateFrameViewAndValues(false, true);
        }

        private void ShowFrameBorderItem_Checked(object sender, RoutedEventArgs e)
        {
            if (ShowFrameBorderItem.IsChecked) ShowFrameBorder = true;
            else ShowFrameBorder = false;

            ButtonShowFrameBorder.IsChecked = ShowFrameBorder;

            UpdateFrameViewAndValues();
        }

        private void ShowFullImageItem_Checked(object sender, RoutedEventArgs e)
        {
            if (ShowFullImageItem.IsChecked) ShowFullFrame = true;
            else ShowFullFrame = false;

            ButtonShowFullImage.IsChecked = ShowFullFrame;

            ButtonShowOnlyRefrenceSection.IsEnabled = ShowFullFrame;
            ClipRefrenceFrameItem.IsEnabled = ShowFullFrame;

            UpdateFrameViewAndValues();
        }

        private void ButtonZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ChangeZoomLevel(true);
        }

        private void ButtonShowCenter_Click(object sender, RoutedEventArgs e)
        {
            ShowCenterAlignmentLines.IsChecked = !ShowCenterAlignmentLines.IsChecked;
            ShowCenterAlignmentLines_Checked(null, null);
        }

        private void ShowCenterAlignmentLines_Checked(object sender, RoutedEventArgs e)
        {
            if (ShowCenterAlignmentLines.IsChecked) ShowAlignmentLines = true;
            else ShowAlignmentLines = false;

            ButtonShowCenter.IsChecked = ShowAlignmentLines;

            UpdateFrameViewAndValues();
        }

        private void ButtonZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ChangeZoomLevel(false);
        }

        private void ButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            ButtonHelp.ContextMenu.IsOpen = true;
        }

        private void ButtonShowBackground_Click(object sender, RoutedEventArgs e)
        {
            ShowSolidImageTransparencyBackground.IsChecked = !ShowSolidImageTransparencyBackground.IsChecked;
            ShowSolidImageTransparencyBackground_Checked(null, null);
        }

        private void ButtonShowFrameBorder_Click(object sender, RoutedEventArgs e)
        {
            ShowFrameBorderItem.IsChecked = !ShowFrameBorderItem.IsChecked;
            ShowFrameBorderItem_Checked(null, null);
        }

        private void ButtonShowFullImage_Click(object sender, RoutedEventArgs e)
        {
            ShowFullImageItem.IsChecked = !ShowFullImageItem.IsChecked;
            ShowFullImageItem_Checked(null, null);
        }

        private void ButtonForceCenterImage_Click(object sender, RoutedEventArgs e)
        {
            ForceCenterFrameItem.IsChecked = !ForceCenterFrameItem.IsChecked;
            ForceCenterFrameItem_Checked(null, null);
        }

        private void ForceCenterFrameItem_Checked(object sender, RoutedEventArgs e)
        {
            if (ForceCenterFrameItem.IsChecked) ForceCenterFrame = true;
            else ForceCenterFrame = false;

            ButtonForceCenterFrame.IsChecked = ForceCenterFrame;

            UpdateFrameViewAndValues();
        }

        private void ButtonShowOnlyRefrenceSection_Click(object sender, RoutedEventArgs e)
        {
            ClipRefrenceFrameItem.IsChecked = !ClipRefrenceFrameItem.IsChecked;
            ClipRefrenceFrameItem_Checked(null, null);
        }

        private void ClipRefrenceFrameItem_Checked(object sender, RoutedEventArgs e)
        {
            if (ClipRefrenceFrameItem.IsChecked) ShowOnlyRefrenceSection = true;
            else ShowOnlyRefrenceSection = false;

            ButtonShowOnlyRefrenceSection.IsChecked = ShowOnlyRefrenceSection;

            UpdateFrameViewAndValues();
        }


        #endregion

        #region File Select Toolstrip


        private void FileSpriteSelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentFrame != null)
            {
                UpdateFileSelectToolstrip();
                FileSpriteSelectButton.ContextMenu.IsOpen = true;
            }

        }

        private void UpdateFileSelectToolstrip()
        {
            CleanUpFileSelectToolStrip();
            if (Directory.Exists(CurrentFrame.Directory))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(CurrentFrame.Directory);
                var files = directoryInfo.GetFiles().Where(s => s.Extension == ".bmp" || s.Extension == ".png");
                foreach (var file in files)
                {
                    if (File.Exists(file.FullName))
                    {
                        SpriteSheetFileList.Items.Add(GenerateInstalledVersionsToolstripItem(file.Name));
                    }
                }

            }

        }

        private void CleanUpFileSelectToolStrip()
        {
            foreach (var item in SpriteSheetFileList.Items.Cast<MenuItem>())
            {
                item.Click -= SelectSpriteFromToolstrip;
            }
            SpriteSheetFileList.Items.Clear();
        }

        private MenuItem GenerateInstalledVersionsToolstripItem(string name)
        {
            MenuItem item = new MenuItem();
            item.Header = name;
            item.Tag = name;
            item.Click += SelectSpriteFromToolstrip;
            return item;
        }

        private void SelectSpriteFromToolstrip(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;
            FileTextBox.Text = item.Tag.ToString();
        }

        #endregion

        #region Refrence Frames Methods

        private void CustomRefrenceButton_Click(object sender, RoutedEventArgs e)
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string ModManagerVersionsPath = System.IO.Path.Combine(appdata, @"Sonic3AIR\mods");
            using (var fldrDlg = new FolderSelectDialog())
            {
                OpenFileDialog fileDialog = new OpenFileDialog()
                {
                    Filter = "JSON Files (*.json) | *.json",
                    InitialDirectory = ModManagerVersionsPath
                };
                if (fileDialog.ShowDialog() == true)
                {
                    CurrentRefrenceAnimation = new Animation(new FileInfo(fileDialog.FileName));
                    UpdateRefrenceSelect();
                    UpdateRefrenceCheckBoxes((CurrentRefrenceAnimation != null ? CustomRefrenceButton : null));
                }
            }
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
            CustomRefrenceButton.IsChecked = false;
            RefrenceNothingButton.IsChecked = false;

            if (sender != null) (sender as MenuItem).IsChecked = true;
            else RefrenceNothingButton.IsChecked = true;

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

                if (File.Exists(result)) CurrentRefrenceAnimation = new Animation(new FileInfo(result));

                UpdateFrameViewAndValues();
            }

            UpdateRefrenceCheckBoxes(sender);
            CanvasView.InvalidateVisual();


        }

        private void SetRefreneceFolderPath_Click(object sender, RoutedEventArgs e)
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
                    Properties.Settings.Default.LastRefrenceFolder = fldrDlg.FileName;
                    Properties.Settings.Default.Save();
                    UpdateFrameViewAndValues();
                }
            }

        }

        #endregion

        #region Keyboard Input Events

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                //NewFile
            }
            else if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                //OpenFile
                if (OpenMenuItem.IsEnabled) OpenFileEvent(null, null);
            }
            else if(e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                //SaveFile
                if (SaveMenuItem.IsEnabled) SaveFileEvent(null, null);
            }
            else if(e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                //SaveFileAs
                if (SaveAsMenuItem.IsEnabled) SaveFileAsEvent(null, null);
            }
            else if(e.Key == Key.U && Keyboard.Modifiers == ModifierKeys.Control)
            {
                //UnloadFile
                if (UnloadMenuItem.IsEnabled) UnloadEvent(null, null);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                int recentItem = -1;

                if (e.Key == Key.D1) recentItem = 0;
                else if (e.Key == Key.D2) recentItem = 1;
                else if (e.Key == Key.D3) recentItem = 2;
                else if (e.Key == Key.D4) recentItem = 3;
                else if (e.Key == Key.D5) recentItem = 4;
                else if (e.Key == Key.D6) recentItem = 5;
                else if (e.Key == Key.D7) recentItem = 6;
                else if (e.Key == Key.D8) recentItem = 7;
                else if (e.Key == Key.D9) recentItem = 8;
                else if (e.Key == Key.D0) recentItem = 9;

                if (recentItem != -1)
                {
                    RefreshRecentFiles(Properties.Settings.Default.RecentItems);
                    if (RecentItems.Count >= recentItem && RecentItems.Count != 0) RecentItem_Click(RecentItems.ElementAt(recentItem), null);
                }




            }
        }

        #endregion
    }


}
