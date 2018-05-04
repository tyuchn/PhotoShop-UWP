using project.Tools;
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
using ImageEditor.DrawingObjects;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace project
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class scrawl : Page, INotifyPropertyChanged
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
        public scrawl()
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

                this.Height = height * 0.8;
                this.Width = this.Height * 1.6;
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
                CanvasDevice cd = CanvasDevice.GetSharedDevice();
                var stream = await image.OpenAsync(FileAccessMode.Read);
                _image = await CanvasBitmap.LoadAsync(cd, stream);
                MyCanvas.Invalidate();
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

                this.Height = height * 0.8;
                this.Width = this.Height * 1.6;
            }
            SetCanvas();
            MyCanvas.Invalidate();
            await Task.Delay(10);
            SetCanvas();
            MyCanvas.Invalidate();
        }
        /// <summary>
        /// 画布绘制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void MyCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            var target = GetDrawings(true);  //
            if (target != null)
            {
                args.DrawingSession.DrawImage(target);
            }
        }



        private void OKBtn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            GenerateResultImage();
        }


        private Color _back_color = Colors.White;   //画布背景色
        private Stretch _stretch = Stretch.Fill;  //底图图片填充方式
        private int _size_mode = 1;  //画布长宽比  
        private int _pen_size = 2;   //涂鸦画笔粗细
        private Color _pen_color = Colors.Orange;  //涂鸦画笔颜色
        private DoodleUI _current_editing_doodleUI;  //当前涂鸦对象
        private CanvasBitmap _image;  //底图
        Stack<IDrawingUI> _doodleUIs = new Stack<IDrawingUI>();  //涂鸦



        private CanvasRenderTarget GetDrawings(bool edit)
        {
            double w, h;  //画布大小
            if (edit)  //编辑状态
            {
                w = MyCanvas.ActualWidth;
                h = MyCanvas.ActualHeight;
            }
            else
            {
                Rect des = GetImageDrawingRect();

                w = (_image.Size.Width / des.Width) * MyCanvas.Width;
                h = (_image.Size.Height / des.Height) * MyCanvas.Height;
            }
            var scale = edit ? 1 : w / MyCanvas.Width;  //缩放比例

            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget target = new CanvasRenderTarget(device, (float)w, (float)h, 96);
            using (CanvasDrawingSession graphics = target.CreateDrawingSession())
            {
                graphics.Clear(_back_color);

                DrawBackImage(graphics, scale);
                //绘制涂鸦
                if (_doodleUIs != null && _doodleUIs.Count > 0)
                {
                    var list = _doodleUIs.ToList(); list.Reverse();
                    list.ForEach((d) => { d.Draw(graphics, (float)scale); });
                }
                if (_current_editing_doodleUI != null)
                {
                    _current_editing_doodleUI.Draw(graphics, (float)scale); //正在涂鸦对象 在上面
                }

            }

            return target;
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

                graphics.DrawImage(image, des, _image.Bounds);
            }
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
                MyCanvas.Width = MyCanvas.Height = l;
            }
            else if (_size_mode == 1)  //4:3
            {
                if (w <= h)
                {
                    MyCanvas.Width = w;
                    MyCanvas.Height = MyCanvas.Width * 3 / 4;
                }
                else
                {
                    if (w / h <= (double)4 / 3)
                    {
                        MyCanvas.Width = w;
                        MyCanvas.Height = MyCanvas.Width * 3 / 4;
                    }
                    else
                    {
                        MyCanvas.Height = h;
                        MyCanvas.Width = MyCanvas.Height * 4 / 3;
                    }
                }
            }
            else  //3:4
            {
                if (h <= w)
                {
                    MyCanvas.Height = h;
                    MyCanvas.Width = MyCanvas.Height * 3 / 4;
                }
                else
                {
                    if (h / w <= (double)4 / 3)
                    {
                        MyCanvas.Height = h;
                        MyCanvas.Width = MyCanvas.Height * 3 / 4;
                    }
                    else
                    {
                        MyCanvas.Width = w;
                        MyCanvas.Height = MyCanvas.Width * 4 / 3;
                    }
                }
            }
            MyCanvas.Invalidate();
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
                var w = MyCanvas.Width - 10;
                var h = MyCanvas.Height - 10;
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
                var w = MyCanvas.Width;
                var h = MyCanvas.Height;
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



        /// <summary>
        /// 操作画布开始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyCanvas_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {

            if (_current_editing_doodleUI == null)
            {
                _current_editing_doodleUI = new DoodleUI() { DrawingColor = _pen_color, DrawingSize = _pen_size };
                _current_editing_doodleUI.InitImageBrush();  //可能是图片图片画刷  需要提前初始化
            }



        }


        /// <summary>
        /// 操作画布结束
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyCanvas_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {

            if (_current_editing_doodleUI != null)
            {
                _doodleUIs.Push(_current_editing_doodleUI);
                _current_editing_doodleUI = null;
                MyCanvas.Invalidate();
            }



        }

        /// <summary>
        /// 操作画布进行时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyCanvas_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {

            if (_current_editing_doodleUI != null)
            {
                _current_editing_doodleUI.Points.Add(e.Position);
                MyCanvas.Invalidate();
            }
        }



        /// <summary>
        /// 选择涂鸦画笔粗细
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PenSize_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var pen_size = sender as Border;
            List<Border> l = new List<Border> { PenSize1, PenSize2, PenSize3 };

            l.ForEach((b) => { (b as Border).Child.Visibility = Visibility.Collapsed; }); //不选中状态

            pen_size.Child.Visibility = Visibility.Visible; //选中

            _pen_size = int.Parse(pen_size.Tag.ToString());
        }
        /// <summary>
        /// 选择涂鸦画笔颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PenColor_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var pen_color = sender as Border;

            List<Border> l = new List<Border> { PenColor1, PenColor2, PenColor3, PenColor4, PenColor5 };
            l.ForEach((b) => { b.Child.Visibility = Visibility.Collapsed; });

            pen_color.Child.Visibility = Visibility.Visible;

            if (pen_color.Background is ImageBrush)  //图片刷子
            {
                _pen_color = Colors.Transparent;
                PenSize1.Background = PenSize2.Background = PenSize3.Background = pen_color.Background;
            }
            else
            {
                _pen_color = (pen_color.Background as SolidColorBrush).Color;
                PenSize1.Background = PenSize2.Background = PenSize3.Background = new SolidColorBrush(_pen_color);
            }
        }


        /// <summary>
        /// 涂鸦撤销
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SymbolIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_doodleUIs != null && _doodleUIs.Count > 0)
            {
                _doodleUIs.Pop();  //删除最近一次涂鸦 立即重绘
                MyCanvas.Invalidate();
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            GenerateResultImage();
        }



    }

    public delegate void ImageEditedCompletedEventHandler(BitmapImage image);
}
