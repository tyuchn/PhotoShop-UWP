using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace project
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class scenery : Page
    {
        public scenery()
        {
            this.InitializeComponent();
        }


        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            //文件选择器  
            FileOpenPicker openPicker = new FileOpenPicker();
            //初始位置  
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            //添加文件类型  
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".gif");
            //选取单个文件  
            Windows.Storage.StorageFile file = await openPicker.PickSingleFileAsync();
            
            if (file != null)
            {
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    var srcImage = new BitmapImage();
                    await srcImage.SetSourceAsync(stream);
                    Img.Source = srcImage;
                }
            }

        }


        private void MenuFlyoutItem_Click1(object sender, RoutedEventArgs e)
        {
            MyFrame.Navigate(typeof(draw));
        }
        private void MenuFlyoutItem_Click2(object sender, RoutedEventArgs e)
        {
            MyFrame.Navigate(typeof(draw));
        }
        private void MenuFlyoutItem_Click3(object sender, RoutedEventArgs e)
        {
            MyFrame.Navigate(typeof(draw));
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var saveFile = new FileSavePicker();
            //初始位置  
            saveFile.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            // 显示在下拉列表的文件类型  
            saveFile.FileTypeChoices.Add("图片", new List<string>() { ".png", ".jpg", ".jpeg", ".bmp" });
            // 默认的文件名  
            saveFile.SuggestedFileName = "SaveFile";

            StorageFile sFile = await saveFile.PickSaveFileAsync();

            if (sFile != null)
            {
                // 在用户完成更改并调用CompleteUpdatesAsync之前，阻止对文件的更新  
                CachedFileManager.DeferUpdates(sFile);
                //把控件变成图像  
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                //传入参数Image控件  
                await renderTargetBitmap.RenderAsync(Img);

                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                using (var fileStream = await sFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Ignore,
                        (uint)renderTargetBitmap.PixelWidth,
                        (uint)renderTargetBitmap.PixelHeight,
                        DisplayInformation.GetForCurrentView().LogicalDpi,
                        DisplayInformation.GetForCurrentView().LogicalDpi,
                        pixelBuffer.ToArray()
                        );
                    //刷新图像  
                    await encoder.FlushAsync();
                }
            }
            else
            {
                //information.Text = "取消保存";
            }
        
        }
    }
}
