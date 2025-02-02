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
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
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
        //private MediaPlayer _mediaPlayer;
        private MediaCapture _mediaCapture;
        private IRController _irController;
        private bool _taskRunning = false;

        public MainWindow()
        {
            InitializeComponent();

            StartCapture();
        }

        private void StartCapture()
        {
            imageElement.Source = new SoftwareBitmapSource();

            _irController = new IRController();
            _irController.MediaFrameReader.FrameArrived += MediaFrameReader_FrameArrived;

            //_mediaCapture = new MediaCapture();

            //var irDevices = new List<MediaFrameSourceGroup>();
            //var devices = MediaFrameSourceGroup.FindAllAsync().AsTask().Result;
            //foreach (var mdevice in devices)
            //{
            //    var currentDevice = mdevice.SourceInfos.First();
            //    if (currentDevice.SourceKind == MediaFrameSourceKind.Infrared)
            //    {
            //        irDevices.Add(mdevice);
            //    }
            //}

            //if (irDevices.Count == 0)
            //{
            //    Console.WriteLine("No IR Cameras found.");
            //}

            //MediaFrameSourceGroup device = irDevices.FirstOrDefault();

            //MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings
            //{
            //    SourceGroup = device,
            //    SharingMode = MediaCaptureSharingMode.ExclusiveControl,
            //    StreamingCaptureMode = StreamingCaptureMode.Video,
            //    MemoryPreference = MediaCaptureMemoryPreference.Cpu
            //};

            //_mediaCapture.InitializeAsync(settings).AsTask().Wait();

            //var frameSources = _mediaCapture.FrameSources;
            //var frameSource = frameSources.First().Value;

            //var preferredFormat = frameSource.SupportedFormats.First();
            //var width = preferredFormat.VideoFormat.Width;
            //var height = preferredFormat.VideoFormat.Width;
            //imageElement.Width = width;
            //imageElement.Height = height;
            //frameSource.SetFormatAsync(preferredFormat).AsTask().Wait();


            //_mediaFrameReader = _mediaCapture.CreateFrameReaderAsync(frameSource).AsTask().Result;
            //_mediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;

            //_mediaFrameReader.FrameArrived += MediaFrameReader_FrameArrived;
            //_mediaFrameReader.StartAsync().AsTask().Wait

        }

        private void MediaFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            using (var frameReference = sender.TryAcquireLatestFrame())
            {
                var videoMediaFrame = frameReference?.VideoMediaFrame;
                var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

                if (softwareBitmap == null) return;
                if (softwareBitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8 ||
    softwareBitmap.BitmapAlphaMode != Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                softwareBitmap = Interlocked.Exchange(ref _backBuffer, softwareBitmap);
                softwareBitmap?.Dispose();
                if (imageElement.DispatcherQueue != null) imageElement.DispatcherQueue.TryEnqueue(
                    async () =>
                    {
                        // Don't let two copies of this task run at the same time.
                        if (_taskRunning)
                            return;
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
            }
        }

    }
}
