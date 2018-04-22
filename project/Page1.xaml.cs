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
    public sealed partial class Page1 : Page
    {
        public Page1()
        {
            this.InitializeComponent();
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //创建和自定义 FileOpenPicker  
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail; //可通过使用图片缩略图创建丰富的视觉显示，以显示文件选取器中的文件  
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".gif");

            //选取单个文件  
            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();

            //文件处理  
            if (file != null)
            {
                 var inputFile = SharedStorageAccessManager.AddFile(file);
                 var destination = await ApplicationData.Current.LocalFolder.CreateFileAsync("Cropped.jpg", CreationCollisionOption.GenerateUniqueName);//在应用文件夹中建立文件用来存储裁剪后的图像  
                 var destinationFile = SharedStorageAccessManager.AddFile(destination);
                 var options = new LauncherOptions();
                 options.TargetApplicationPackageFamilyName = "Microsoft.Windows.Photos_8wekyb3d8bbwe";

                 //待会要传入的参数  
                 var parameters = new ValueSet();
                 parameters.Add("InputToken", inputFile);                //输入文件  
                 parameters.Add("DestinationToken", destinationFile);    //输出文件  
                 parameters.Add("ShowCamera", false);                    //它允许我们显示一个按钮，以允许用户采取当场图象(但是好像并没有什么卵用)  
                 parameters.Add("EllipticalCrop", true);                 //截图区域显示为圆（最后截出来还是方形）  
                 parameters.Add("CropWidthPixals", 300);
                 parameters.Add("CropHeightPixals", 300);

                 //调用系统自带截图并返回结果  
                 var result = await Launcher.LaunchUriForResultsAsync(new Uri("microsoft.windows.photos.crop:"), options, parameters);

                 //按理说下面这个判断应该没问题呀，但是如果裁剪界面点了取消的话后面会出现异常，所以后面我加了try catch  
                 if (result.Status == LaunchUriStatus.Success && result.Result != null)
                 {
                     //对裁剪后图像的下一步处理  
                     try
                     {
                         // 载入已保存的裁剪后图片  
                         var stream = await destination.OpenReadAsync();
                         var bitmap = new BitmapImage();
                         await bitmap.SetSourceAsync(stream);

                         // 显示  
                         Img.Source = bitmap;
                     }
                     catch (Exception ex)
                     {
                         Debug.WriteLine(ex.Message + ex.StackTrace);
                     }
                 }
               
            }
        }
        
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MyFrame.Navigate(typeof(draw));
        }
        private void Button_Click_afilter(object sender, RoutedEventArgs e)
        {
            MyFrame.Navigate(typeof(Addfilter));
        }
        
        private async void Button_Click_2(object sender, RoutedEventArgs e)
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

        private void MyFrame_Navigated(object sender, NavigationEventArgs e)
        {

        }
    }
}
