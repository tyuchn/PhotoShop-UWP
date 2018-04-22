﻿using project.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas;
using project.DrawingObjects;
using System.Threading.Tasks;
using Windows.UI.Xaml.Shapes;
using Windows.Storage;
using Microsoft.Graphics.Canvas.Effects;
using System.Collections.ObjectModel;
using Windows.Storage.Streams;


// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace project.New
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class BlankPage2 : Page, INotifyPropertyChanged

    {
        public event ImageEditedCompletedEventHandler ImageEditedCompleted;
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property_name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property_name));
            }
        }
       

        private ObservableCollection<string> _wallpapers = new ObservableCollection<string>();
        
        Border border = new Border();
        Popup popup = new Popup();
        public BlankPage2()
        {
            this.InitializeComponent();
            Loaded += ImageEditorControl_Loaded;
        }
        private void ImageEditorControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetCanvas();
        }

        /// <summary>
        /// 显示编辑器
        /// </summary>
        private void Show()
        {
            var height = ApplicationView.GetForCurrentView().VisibleBounds.Height;
            var width = ApplicationView.GetForCurrentView().VisibleBounds.Width;

            border.Background = new SolidColorBrush(Color.FromArgb(0XAA, 0X00, 0X00, 0X00));

            if (UWPPlatformTool.IsMobile)
            {
                border.Width = this.Width = width;
                border.Height = this.Height = height;
            }
            else
            {
                border.Width = width;
                border.Height = height;

                this.Height = height*0.8;
                this.Width = this.Height*1.6;
            }

            SetCanvas();

            if (UWPPlatformTool.IsMobile)
            {
                popup.VerticalOffset = 24;
            }
            border.Child = this;

            popup.Child = border;
            popup.Opened += (s, e) =>
            {
                Window.Current.SizeChanged += Current_SizeChanged;
            };
            popup.Closed += (s, e) =>
            {
                Window.Current.SizeChanged -= Current_SizeChanged;
            };
            popup.IsOpen = true;
        }
        public async void Show(StorageFile image)
        {
            try
            {
                Show();
                WaitLoading.IsActive = true;
                CanvasDevice cd = CanvasDevice.GetSharedDevice();
                var stream = await image.OpenAsync(FileAccessMode.Read);
                _image = await CanvasBitmap.LoadAsync(cd, stream);
                WaitLoading.IsActive = false;
                MainCanvas.Invalidate();
            }
            catch
            {

            }
        }
        private async void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            var height = ApplicationView.GetForCurrentView().VisibleBounds.Height;
            var width = ApplicationView.GetForCurrentView().VisibleBounds.Width;

            if (UWPPlatformTool.IsMobile)
            {
                border.Width = this.Width = width;
                border.Height = this.Height = height;
            }
            else
            {
                border.Width = width;
                border.Height = height;

                this.Height = height *0.8 ;
                this.Width = this.Height *1.6 ;
            }
            SetCanvas();
            MainCanvas.Invalidate();
            await Task.Delay(10);
            SetCanvas();
            MainCanvas.Invalidate();
        }
        /// <summary>
        /// 画布绘制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void MainCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            var target = GetDrawings(true);  //
            if (target != null)
            {
                args.DrawingSession.DrawImage(target);
            }
        }
        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (sender as Pivot).SelectedIndex;
            RelativePanel tab = null;
            switch (selected)
            {
                case 0:
                    {
                        tab = tab0;
                        break;
                    }
            }
            List<RelativePanel> l = new List<RelativePanel> { tab0 };
            foreach (RelativePanel t in l)
            {
                (t.Children[0] as TextBlock).Foreground = new SolidColorBrush(Colors.White);
                (t.Children[1] as Rectangle).Fill = new SolidColorBrush(Colors.White);
            }
            (tab.Children[0] as TextBlock).Foreground = new SolidColorBrush(Colors.Black);
            (tab.Children[1] as Rectangle).Fill = new SolidColorBrush(Colors.Black);
        }
        /// <summary>
        /// 点击tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tab_Tapped(object sender, TappedRoutedEventArgs e)
        {
            List<RelativePanel> tabs = new List<RelativePanel> { tab0 };
            int selected = tabs.IndexOf(sender as RelativePanel);

            MainCommandPanel.SelectedIndex = selected;
        }




        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            MainCanvas.Invalidate();
        }

        private void Filters_ItemClick(object sender, ItemClickEventArgs e)
        {
            foreach (var item in Filters.Items)
            {
                (((item as GridViewItem).Content as StackPanel).Children[1] as Border).Background = new SolidColorBrush(Colors.Pink);
                ((((item as GridViewItem).Content as StackPanel).Children[1] as Border).Child as TextBlock).Foreground = new SolidColorBrush(Colors.Black);
            }
            ((e.ClickedItem as StackPanel).Children[1] as Border).Background = new SolidColorBrush(Colors.Pink);
            (((e.ClickedItem as StackPanel).Children[1] as Border).Child as TextBlock).Foreground = new SolidColorBrush(Colors.DeepPink);

            _filter_index = int.Parse((e.ClickedItem as StackPanel).Tag.ToString());

            MainCanvas.Invalidate();
        }

        private void CancelBtn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (popup != null)
            {
                popup.IsOpen = false;
            }
        }

        private void OKBtn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            GenerateResultImage();
        }

        #region fields
        private Color _back_color = Colors.White;   //画布背景色
        private Stretch _stretch = Stretch.Uniform;  //底图图片填充方式
        private int _size_mode = 2;  //画布长宽比  
        private Color _pen_color = Colors.Orange;  //涂鸦画笔颜色
        private CanvasBitmap _image;  //底图
       


        private int _filter_index = 0;  //滤镜



        private CanvasRenderTarget GetDrawings(bool edit)
        {
            double w, h;  //画布大小
            if (edit)  //编辑状态
            {
                w = MainCanvas.ActualWidth;
                h = MainCanvas.ActualHeight;
            }
            else 
            {
                Rect des = GetImageDrawingRect();

                w = (_image.Size.Width / des.Width) * MainCanvas.Width;
                h = (_image.Size.Height / des.Height) * MainCanvas.Height;
            }
            var scale = edit ? 1 : w / MainCanvas.Width;  //缩放比例

            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget target = new CanvasRenderTarget(device, (float)w, (float)h, 96);
            using (CanvasDrawingSession graphics = target.CreateDrawingSession())
            {
                graphics.Clear(_back_color);
                
                DrawBackImage(graphics, scale);
         

            }

            return target;
        }
        private void SetCanvas()
        {
            var w = MainWorkSapce.ActualWidth - 40;  //
            var h = MainWorkSapce.ActualHeight - 40;  //
            if (w <= 0 || h <= 0)
            {
                return;
            }
            if (_size_mode == 0)  //1:1
            {
                var l = w > h ? h : w;
                MainCanvas.Width = MainCanvas.Height = l;
            }
            else if (_size_mode == 1)  //4:3
            {
                if (w <= h)
                {
                    MainCanvas.Width = w;
                    MainCanvas.Height = MainCanvas.Width * 3 / 4;
                }
                else
                {
                    if (w / h <= (double)4 / 3)
                    {
                        MainCanvas.Width = w;
                        MainCanvas.Height = MainCanvas.Width * 3 / 4;
                    }
                    else
                    {
                        MainCanvas.Height = h;
                        MainCanvas.Width = MainCanvas.Height * 4 / 3;
                    }
                }
            }
            else  //3:4
            {
                if (h <= w)
                {
                    MainCanvas.Height = h;
                    MainCanvas.Width = MainCanvas.Height * 3 / 4;
                }
                else
                {
                    if (h / w <= (double)4 / 3)
                    {
                        MainCanvas.Height = h;
                        MainCanvas.Width = MainCanvas.Height * 3 / 4;
                    }
                    else
                    {
                        MainCanvas.Width = w;
                        MainCanvas.Height = MainCanvas.Width * 4 / 3;
                    }
                }
            }
            MainCanvas.Invalidate();
        }
        private void DrawBackImage(CanvasDrawingSession graphics, double scale)
        {
            if (_image != null)
            {
                Rect des = GetImageDrawingRect();
                des.X *= scale;
                des.Y *= scale;
                des.Width *= scale;
                des.Height *= scale;
                
                ICanvasImage image = GetBrightnessEffect(_image);
                


                //应用滤镜模板
                image = ApplyFilterTemplate(image);

                graphics.DrawImage(image, des, _image.Bounds);
            }
        }
        private ICanvasImage GetBrightnessEffect(ICanvasImage source)
        {
            var exposureEffect = new ExposureEffect
            {
                Source = source,

            };

            return exposureEffect;
        }
        
        private async void GenerateResultImage()
        {
            var img = GetDrawings(false);
            if (img != null)
            {
                IRandomAccessStream stream = new InMemoryRandomAccessStream();
                await img.SaveAsync(stream, CanvasBitmapFileFormat.Jpeg);
                BitmapImage result = new BitmapImage();
                stream.Seek(0);
                await result.SetSourceAsync(stream);
                if (ImageEditedCompleted != null)
                {
                    ImageEditedCompleted(result);
                }
                if (popup != null)
                {
                    popup.IsOpen = false;
                }
            }
        }
        private Rect GetImageDrawingRect()
        {
            Rect des;

            var image_w = _image.Size.Width;
            var image_h = _image.Size.Height;

            if (_stretch == Stretch.Uniform)
            {
                var w = MainCanvas.Width-10;
                var h = MainCanvas.Height-10;
                if (image_w / image_h > w / h)
                {
                    var left = 10;

                    var width = w;
                    var height = (image_h / image_w) * width;

                    var top = (h - height) / 2 + 10;

                    des = new Rect(left, top, width, height);
                }
                else
                {
                    var top = 10;
                    var height = h;
                    var width = (image_w / image_h) * height;
                    var left = (w - width) / 2 + 10;
                    des = new Rect(left, top, width, height);
                }
            }
            else
            {
                var w = MainCanvas.Width;
                var h = MainCanvas.Height;
                var left = 0;
                var top = 0;
                if (image_w / image_h > w / h)
                {
                    var height = h;
                    var width = (image_w / image_h) * height;
                    des = new Rect(left, top, width, height);
                }
                else
                {
                    var width = w;
                    var height = (image_h / image_w) * width;

                    des = new Rect(left, top, width, height);
                }
            }
            return des;
        }
        #endregion

     
        private ICanvasImage ApplyFilterTemplate(ICanvasImage source)
        {
            if (_filter_index == 0)  //无滤镜
            {
                return source;
            }
            else if (_filter_index == 3)  // 黑白
            {
                return new GrayscaleEffect
                {
                    Source = source
                };
            }
            else if (_filter_index == 1)  //反色
            {
                return new InvertEffect
                {
                    Source = source
                };
            }
            else if (_filter_index == 2) //冷淡
            {
                var hueRotationEffect = new HueRotationEffect
                {
                    Source = source,
                    Angle = 0.5f
                };
                return hueRotationEffect;
            }
            else if (_filter_index == 4)  //美食
            {
                var temperatureAndTintEffect = new TemperatureAndTintEffect
                {
                    Source = source
                };
                temperatureAndTintEffect.Temperature = 0.6f;
                temperatureAndTintEffect.Tint = 0.6f;

                return temperatureAndTintEffect;
            }
           
            else if (_filter_index == 5) //雕刻
            {
                var embossEffect = new EmbossEffect
                {
                    Source = source
                };
                embossEffect.Amount = 5;
                embossEffect.Angle = 0;
                return embossEffect;
            }
           
            else
            {
                return source;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            GenerateResultImage();
        }
    }

    public delegate void ImageEditedCompletedEventHandler(BitmapImage image);
}
  