using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.Graphics;
using Windows.Storage;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas;
using System.Threading.Tasks;
using Windows.UI;
using System.Numerics;
using Windows.Storage.Pickers;
using project.New;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace project
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class Addfilter : Page
    {
        public Addfilter()
        {
            this.InitializeComponent();
        }

        private async void EditLocal_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FileOpenPicker fo = new FileOpenPicker();
            fo.FileTypeFilter.Add(".png");
            fo.FileTypeFilter.Add(".jpg");
            fo.SuggestedStartLocation = PickerLocationId.Desktop;

            var f = await fo.PickSingleFileAsync();
            if (f != null)
            {
                BlankPage2 editor = new BlankPage2();
                editor.Show(f);
                
                editor.ImageEditedCompleted += (image_edited) =>
                {
                    image.Source = image_edited;
                };
            }
        }


    }
}
