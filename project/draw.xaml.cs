using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
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
    public sealed partial class draw : Page
    {
        public draw()
        {
            this.InitializeComponent();
            inkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse;
        }


        private async void BtnSave_Click(object sender, RoutedEventArgs e)
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
                CanvasDevice device = CanvasDevice.GetSharedDevice();
                CanvasRenderTarget renderTarget = new CanvasRenderTarget(device, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight, 96);
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    ds.Clear(Colors.White);
                    ds.DrawInk(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());
                }

                using (var fileStream = await sFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await renderTarget.SaveAsync(fileStream, CanvasBitmapFileFormat.Jpeg, 1f);
                }
            }
        }




        
    }
}
