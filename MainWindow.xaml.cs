using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.System;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IRCameraView
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private SoftwareBitmap _backBuffer;
        private MediaFrameReader _mediaFrameReader;
        private IRController _irController;
        private bool _taskRunning = false;

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void myButton_Click(object sender, RoutedEventArgs e)
        {
            myButton.Content = "Clicked";

            imageElement.Source = new SoftwareBitmapSource();

            IRController iRController = new IRController();
            _irController = iRController;
            MediaFrameReader mediaFrameReader = iRController.MediaFrameReader;
            _mediaFrameReader = mediaFrameReader;
            mediaFrameReader.FrameArrived += MediaFrameReader_FrameArrived;
            mediaFrameReader.StartAsync().AsTask().Wait();

            //mediaPlayerElement.SetMediaPlayer(.)KC
            //iRController.MediaCapture.VideoDeviceController.
            
        }

        private void MediaFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            using (var frameReference = sender.TryAcquireLatestFrame())
            {
                var videoMediaFrame = frameReference?.VideoMediaFrame;
                var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

                if (frameReference != null)
                {
                    // Process the frame (e.g., display it or analyze it)
                    // This demo just displays the frames directly
                }
                //C: \Users\Lasse\Pictures\Screenshots\2024 - 07 - 31 00_51_31 - Greenshot.jpg

                if (softwareBitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8 ||
    softwareBitmap.BitmapAlphaMode != Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                softwareBitmap = Interlocked.Exchange(ref _backBuffer, softwareBitmap);
                softwareBitmap?.Dispose();
               
                //var task = 
                    DispatcherQueue.TryEnqueue(
            async () =>
            {
                // Don't let two copies of this task run at the same time.
                if (_taskRunning)
                {
                    return;
                }
                _taskRunning = true;

                // Keep draining frames from the backbuffer until the backbuffer is empty.
                SoftwareBitmap latestBitmap;
                while ((latestBitmap = Interlocked.Exchange(ref _backBuffer, null)) != null)
                {
                    var imageSource = (SoftwareBitmapSource)imageElement.Source;
                    await imageSource.SetBitmapAsync(latestBitmap);
                    latestBitmap.Dispose();
                }

                _taskRunning = false;
            });
                //task.AsTask().Wait();
            }
        }

    }
}
