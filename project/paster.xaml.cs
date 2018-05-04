using ImageEditor.DrawingObjects;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using project.DrawingObjects;
using project.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
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
using Windows.UI.Xaml.Shapes;
using Windows.Web.Http;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace project
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class paster : Page, INotifyPropertyChanged
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
        public ObservableCollection<string> WallPapers
        {
            get
            {
                return _wallpapers;
            }
            set
            {
                _wallpapers = value;
                OnPropertyChanged("WallPapers");
            }
        }
        Border border = new Border();
        Popup popup = new Popup();
        public paster()
        {
            this.InitializeComponent();
            Loaded += Paster_Loaded;
        }
        private void Paster_Loaded(object sender, RoutedEventArgs e)
        {
            SetCanvas();
            LoadWallPapers();
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
        public async void Show(Uri uri)
        {
            try
            {
                Show();
                //WaitLoading.IsActive = true;
                CanvasDevice cd = CanvasDevice.GetSharedDevice();
                _image = await CanvasBitmap.LoadAsync(cd, uri, 96);
                //WaitLoading.IsActive = false;
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
            var target = GetDrawings(true);  
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
        private Color _pen_color = Colors.Orange;  //涂鸦画笔颜色
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
                //绘制背景
                graphics.Clear(_back_color);

                //绘制底图
                DrawBackImage(graphics, scale);
                
                //绘制贴图
                if (_wall_paperUI != null)
                {
                    _wall_paperUI.Draw(graphics, (float)scale);
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

        private async void LoadWallPapers()
        {
            var url = "http://files.cnblogs.com/files/xiaozhi_5638/Papers.zip" + "?t=" + DateTime.Now.Ticks;
            var json = await HttpTool.GetJson(url);
            if (json != null)
            {
                var papers = json["papers"].GetArray();
                var image_url = "";
                foreach (var paper in papers)
                {
                    var p = paper.GetObject();
                    image_url = p["image_url"].GetString();
                    if (!String.IsNullOrEmpty(image_url))
                    {
                        WallPapers.Add(image_url);
                    }
                }
            }
        }
        public static async Task<JsonObject> GetJson(string url)
        {
            try
            {
                string json = await SendGetRequest(url);
                if (json != null)
                {
                    Printlog("请求Json数据成功 URL：" + url);
                    return JsonObject.Parse(json);
                }
                else
                {
                    Printlog("请求Json数据失败 URL：" + url);
                    return null;
                }
            }
            catch
            {
                Printlog("请求Json数据失败 URL：" + url);
                return null;
            }
        }
        public async static Task<string> SendGetRequest(string url)
        {
            try
            {
                HttpClient client = new HttpClient();
                Uri uri = new Uri(url);

                HttpResponseMessage response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }

        }
        private static void Printlog(string info)
        {
#if DEBUG
            Debug.WriteLine(DateTime.Now.ToString() + " " + info);
#endif
        }

        private async void WallPapersList_ItemClick(object sender, ItemClickEventArgs e)
        {
            foreach (var item in WallPapersList.Items)
            {
                (((WallPapersList.ContainerFromItem(item) as GridViewItem).ContentTemplateRoot as RelativePanel).Children[1] as Rectangle).Visibility = Visibility.Collapsed;
            }

    (((WallPapersList.ContainerFromItem(e.ClickedItem) as GridViewItem).ContentTemplateRoot as RelativePanel).Children[1] as Rectangle).Visibility = Visibility.Visible;

            //创建墙纸
            _wall_paperUI = new WallPaperUI() { Editing = true, Height = 100, Width = 100, Image = null, X = 150, Y = 150 };
            //MyCanvas.Invalidate();

            CanvasDevice device = CanvasDevice.GetSharedDevice();
            var img = await CanvasBitmap.LoadAsync(device, new Uri(e.ClickedItem.ToString()));
            if (img != null)
            {
                if (_wallpapers != null)
                {
                    (_wall_paperUI as WallPaperUI).Width = img.Size.Width;
                    (_wall_paperUI as WallPaperUI).Height = img.Size.Height;
                    (_wall_paperUI as WallPaperUI).Image = img;

                    MyCanvas.Invalidate();
                }
            }
;
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

       /* private void MainCanvas_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (MainCommandPanel.SelectedIndex == 4)  //涂鸦状态
            {
                if (_current_editing_doodleUI == null)
                {
                    _current_editing_doodleUI = new DoodleUI() { DrawingColor = _pen_color, DrawingSize = _pen_size };
                    _current_editing_doodleUI.InitImageBrush();  //可能是图片图片画刷  需要提前初始化
                }
                return;
            }
            if (_tagsUIs != null)
            {
                foreach (var tag in _tagsUIs)
                {
                    if ((tag as TagUI).Region.Contains(e.Position))
                    {
                        _current_tag = tag;
                        _pre_manipulation_position = e.Position;
                        _manipulation_type = 2;
                        break;
                    }
                }
            }
            if (MainCommandPanel.SelectedIndex == 0) //可能是剪切状态
            {
                if (_cropUI != null)  //确实是剪切状态
                {
                    if ((_cropUI as CropUI).Region.Contains(e.Position)) //移动剪切对象
                    {
                        _manipulation_type = 0;
                        _pre_manipulation_position = e.Position;
                    }
                    if ((_cropUI as CropUI).RightBottomRegion.Contains(e.Position)) //缩放剪切区域
                    {
                        _manipulation_type = 1;
                        _pre_manipulation_position = e.Position;
                    }

                }
                return;
            }*/

            /// <summary>
            /// 操作画布开始
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void MyCanvas_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {

            if (_wall_paperUI != null)
            {
                if ((_wall_paperUI as WallPaperUI).Region.Contains(e.Position))  //移动墙纸
                {
                    _manipulation_type = 3;
                    _pre_manipulation_position = e.Position;
                    (_wall_paperUI as WallPaperUI).Editing = true;
                }
                if ((_wall_paperUI as WallPaperUI).RightBottomRegion.Contains(e.Position) && (_wall_paperUI as WallPaperUI).Editing)  //缩放墙纸
                {
                    _manipulation_type = 4;
                    _pre_manipulation_position = e.Position;
                }
                MyCanvas.Invalidate();
            }
            return;



        }


        /// <summary>
        /// 操作画布结束
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyCanvas_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (_wall_paperUI != null)
            {
                _pre_manipulation_position = null;
            }
            return;

        }

        /// <summary>
        /// 操作画布进行时
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyCanvas_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {

            if (_wall_paperUI != null && _pre_manipulation_position != null)
            {
                var deltaX = e.Position.X - _pre_manipulation_position.Value.X;
                var deltaY = e.Position.Y - _pre_manipulation_position.Value.Y;
                if (_manipulation_type == 3)  //移动
                {
                    (_wall_paperUI as WallPaperUI).X += deltaX;
                    (_wall_paperUI as WallPaperUI).Y += deltaY;
                }
                else if (_manipulation_type == 4)  //缩放
                {
                    (_wall_paperUI as WallPaperUI).Width += deltaX * 2;
                    (_wall_paperUI as WallPaperUI).SyncWH();  //只需要设置宽度  高度自动同步
                }
                _pre_manipulation_position = e.Position;

                MyCanvas.Invalidate();
            }
        }
        /// <summary>
        /// 异步从网络位置加载墙纸
        /// </summary>


        #region fields

        private Point? _pre_manipulation_position;  //操作起始点
        private int _manipulation_type = 0; // 0表示移动剪切对象 1表示缩放剪切对象 2表示移动tag 3表示移动墙纸 4表示缩放墙纸


        IDrawingUI _wall_paperUI; // 墙纸

        #endregion

        public delegate void ImageEditedCompletedEventHandler(BitmapImage image);

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            GenerateResultImage();
        }
    }
}
