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
using project.New;
using Windows.Media.Capture;
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

        private async void mohu_Click(object sender, RoutedEventArgs e)
        {


            string desiredName = DateTime.Now.Ticks + ".jpg";
            StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            StorageFolder folder = await applicationFolder.CreateFolderAsync("Pic", CreationCollisionOption.OpenIfExists);
            StorageFile saveFile = await folder.CreateFileAsync(desiredName, CreationCollisionOption.OpenIfExists);
            RenderTargetBitmap bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(Img);
            var pixelBuffer = await bitmap.GetPixelsAsync();
            using (var fileStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Ignore,
                     (uint)bitmap.PixelWidth,
                     (uint)bitmap.PixelHeight,
                     DisplayInformation.GetForCurrentView().LogicalDpi,
                     DisplayInformation.GetForCurrentView().LogicalDpi,
                     pixelBuffer.ToArray());
                await encoder.FlushAsync();
            }


            if (saveFile == null)
            {
                // The user cancelled the picking operation
                return;
            }


            SoftwareBitmap inputBitmap;
            using (IRandomAccessStream stream = await saveFile.OpenAsync(FileAccessMode.Read))
            {
                // Create the decoder from the stream
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                // Get the SoftwareBitmap representation of the file
                inputBitmap = await decoder.GetSoftwareBitmapAsync();
            }

            if (inputBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8
                        || inputBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
            {
                inputBitmap = SoftwareBitmap.Convert(inputBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            SoftwareBitmap outputBitmap = new SoftwareBitmap(inputBitmap.BitmapPixelFormat, inputBitmap.PixelWidth, inputBitmap.PixelHeight, BitmapAlphaMode.Premultiplied);


            var helper = new OpenCVBridge.OpenCVHelper();
            helper.Blur(inputBitmap, outputBitmap);

            var bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(outputBitmap);
            Img.Source = bitmapSource;

        }

        private void Button_Click_Draw(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(draw));
        }


        private async void Cut_Click(object sender, RoutedEventArgs e)
        {
            string desiredName = DateTime.Now.Ticks + ".jpg";
            StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            StorageFolder folder = await applicationFolder.CreateFolderAsync("Pic", CreationCollisionOption.OpenIfExists);
            StorageFile saveFile = await folder.CreateFileAsync(desiredName, CreationCollisionOption.OpenIfExists);
            RenderTargetBitmap bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(Img);
            var pixelBuffer = await bitmap.GetPixelsAsync();
            using (var fileStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Ignore,
                     (uint)bitmap.PixelWidth,
                     (uint)bitmap.PixelHeight,
                     DisplayInformation.GetForCurrentView().LogicalDpi,
                     DisplayInformation.GetForCurrentView().LogicalDpi,
                     pixelBuffer.ToArray());
                await encoder.FlushAsync();
            }



            if (saveFile != null)
            {
                CutPicture(saveFile);

            }

        }



        private async void CutPicture(StorageFile file)
        {

            #region 裁剪图片
            var inputFile = SharedStorageAccessManager.AddFile(file);//  获取一个文件共享Token,使应用程序能够与另一个应用程序共享指定的文件。
            var destination = await ApplicationData.Current.LocalFolder.CreateFileAsync("Cropped.jpg", CreationCollisionOption.ReplaceExisting);//在应用文件夹中建立文件用来存储裁剪后的图像 
            var destinationFile = SharedStorageAccessManager.AddFile(destination);
            var options = new LauncherOptions();
            options.TargetApplicationPackageFamilyName = "Microsoft.Windows.Photos_8wekyb3d8bbwe";//应用于启动文件或URI的目标包的包名称
                                                                                                  //待会要传入的参数 
            var parameters = new ValueSet();
            parameters.Add("InputToken", inputFile);                //输入文件 
            parameters.Add("DestinationToken", destinationFile);    //输出文件 
            parameters.Add("ShowCamera", false);                    //它允许我们显示一个按钮，以允许用户采取当场图象(但是好像并没有什么用) 
            parameters.Add("EllipticalCrop", true);                 //截图区域显示为圆（最后截出来还是方形） 
            parameters.Add("CropWidthPixals", 300);
            parameters.Add("CropHeightPixals", 300);
            //调用系统自带截图并返回结果 
            // var result = await Launcher.LaunchUriForResultsAsync(new Uri("microsoft.windows.photos.crop:"), options, parameters);
            var result = await Launcher.LaunchUriForResultsAsync(new Uri("microsoft.windows.photos.crop:"), options, parameters);
            if (result.Status == LaunchUriStatus.Success && result.Result != null)
            {
                //对裁剪后图像的下一步处理 
                try
                {
                    //载入已保存的裁剪后图片 
                    var stream = await destination.OpenReadAsync();
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(stream);
                    // 显示裁剪过后的图片 
                    Img.Source = bitmap;

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message + ex.StackTrace);
                }
            }


            #endregion


        }

        private async void Button_Click_afilter(object sender, RoutedEventArgs e)
        {
            
                string desiredName = DateTime.Now.Ticks + ".jpg";
                StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
                StorageFolder folder = await applicationFolder.CreateFolderAsync("Pic", CreationCollisionOption.OpenIfExists);
                StorageFile saveFile = await folder.CreateFileAsync(desiredName, CreationCollisionOption.OpenIfExists);
                RenderTargetBitmap bitmap = new RenderTargetBitmap();
                await bitmap.RenderAsync(Img);
                var pixelBuffer = await bitmap.GetPixelsAsync();
                using (var fileStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                    encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Ignore,
                         (uint)bitmap.PixelWidth,
                         (uint)bitmap.PixelHeight,
                         DisplayInformation.GetForCurrentView().LogicalDpi,
                         DisplayInformation.GetForCurrentView().LogicalDpi,
                         pixelBuffer.ToArray());
                    await encoder.FlushAsync();
                }

                if (saveFile != null)
                {
                    BlankPage2 editor = new BlankPage2();
                editor.Show(saveFile);

                editor.ImageEditedCompleted += (image_edited) =>
                {
                    Img.Source = image_edited;
                };
            }
        }
        private async void Button_Click_scrawl(object sender, RoutedEventArgs e)
        {
            string desiredName = DateTime.Now.Ticks + ".jpg";
            StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            StorageFolder folder = await applicationFolder.CreateFolderAsync("Pic", CreationCollisionOption.OpenIfExists);
            StorageFile saveFile = await folder.CreateFileAsync(desiredName, CreationCollisionOption.OpenIfExists);
            RenderTargetBitmap bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(Img);
            var pixelBuffer = await bitmap.GetPixelsAsync();
            using (var fileStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Ignore,
                     (uint)bitmap.PixelWidth,
                     (uint)bitmap.PixelHeight,
                     DisplayInformation.GetForCurrentView().LogicalDpi,
                     DisplayInformation.GetForCurrentView().LogicalDpi,
                     pixelBuffer.ToArray());
                await encoder.FlushAsync();
            }

            if (saveFile != null)
            {
                scrawl editor = new scrawl();
                editor.Show(saveFile);

                editor.ImageEditedCompleted += (image_edited) =>
                {
                    Img.Source = image_edited;
                };
            }
        }

        public async void Save_Click(object sender, RoutedEventArgs e)
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
        }
        

        private async void Launch_Camera(object sender, RoutedEventArgs e)
        {
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            captureUI.PhotoSettings.CroppedSizeInPixels = new Size(200, 200);

            StorageFile photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (photo == null)
            {
                // User cancelled photo capture
                return;
            }

            IRandomAccessStream stream = await photo.OpenAsync(FileAccessMode.Read);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            SoftwareBitmap softwareBitmapBGR8 = SoftwareBitmap.Convert(softwareBitmap,
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);

            SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(softwareBitmapBGR8);

            Img.Source = bitmapSource;
        }

        private void Button_delete(object sender, RoutedEventArgs e)
        {
            MyFrame.Navigate(typeof(Page1));
        }
    }
}
