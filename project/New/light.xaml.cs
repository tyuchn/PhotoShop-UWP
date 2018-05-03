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

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace project.New
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
public sealed partial class light : Page, INotifyPropertyChanged

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
    public light()
    {
        this.InitializeComponent();
        Loaded += ImageEditorControl_Loaded;
    }
    private void ImageEditorControl_Loaded(object sender, RoutedEventArgs e)
    {
        SetCanvas();
    }
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
            WaitLoading.IsActive = true;
            CanvasDevice cd = CanvasDevice.GetSharedDevice();
            var stream = await image.OpenAsync(FileAccessMode.Read);
            _image = await CanvasBitmap.LoadAsync(cd, stream);
            WaitLoading.IsActive = false;
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
    private Stretch _stretch = Stretch.Uniform;  //底图图片填充方式
    private int _size_mode = 2;  //画布长宽比  
    private CanvasBitmap _image;  //底图
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
    private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        MyCanvas.Invalidate();
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

            //亮度
            ICanvasImage image = GetBrightnessEffect(_image);
            //锐化
            image = GetSharpenEffect(image);
            //模糊
            image = GetBlurEffect(image);
            graphics.DrawImage(image, des, _image.Bounds);
        }
    }
    private ICanvasImage GetBrightnessEffect(ICanvasImage source)
    {
        var t = Slider1.Value / 500 * 2;
        var exposureEffect = new ExposureEffect
        {
            Source = source,
            Exposure = (float)t
        };

        return exposureEffect;
    }
    private ICanvasImage GetBlurEffect(ICanvasImage source)
    {
        var t = Slider3.Value / 100 * 12;
        var blurEffect = new GaussianBlurEffect
        {
            Source = source,
            BlurAmount = (float)t
        };
        return blurEffect;
    }
    private ICanvasImage GetSharpenEffect(ICanvasImage source)
    {
        var sharpenEffect = new SharpenEffect
        {
            Source = source,
            Amount = (float)(Slider2.Value * 0.1)
        };
        return sharpenEffect;
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
    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        GenerateResultImage();
    }
}
}
