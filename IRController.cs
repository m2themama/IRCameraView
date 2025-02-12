using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Windows.Graphics.Imaging;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Media.Playback;
using Windows.Media.MediaProperties;
using Windows.Media;
using System.Diagnostics;

namespace IRCameraView
{
    public enum IRFrameFilter
    {
        None,
        Raw,
        Illuminated
    }

    public class IRController
    {
        public MediaFrameReader MediaFrameReader { get; private set; }
        public MediaPlayer MediaPlayer { get; private set; }
        public MediaCapture MediaCapture { get; private set; }
        public List<MediaFrameSourceGroup> Devices { get; private set; }
        public MediaFrameSourceGroup Device { get; private set; }

        public IRFrameFilter FrameFilter { get; set; }

        public delegate void FrameReady(SoftwareBitmap bitmap);
        public event FrameReady OnFrameReady;

        private SoftwareBitmap _backBuffer;

        public IRController()
        {
            FrameFilter = IRFrameFilter.None;
            MediaCapture = new MediaCapture();
            LoadCameras(MediaFrameSourceKind.Infrared);

            if (Devices.Count > 0) SelectDevice(Devices.FirstOrDefault());
            else throw new Exception("No infrared camera's were found.");
        }

        private List<MediaFrameSourceGroup> LoadCameras(MediaFrameSourceKind allowedKind)
        {
            return LoadCameras([allowedKind]);
        }

        private List<MediaFrameSourceGroup> LoadCameras(List<MediaFrameSourceKind> allowedKinds)
        {
            Devices = new List<MediaFrameSourceGroup>();
            var devices = MediaFrameSourceGroup.FindAllAsync().AsTask().Result;

            // Filter out the IR camera's
            foreach (var device in devices)
                foreach (var sourceInfo in device.SourceInfos)
                    if (allowedKinds.Contains(sourceInfo.SourceKind))
                        Devices.Add(device);

            return Devices;
        }

        private void SelectDevice(MediaFrameSourceGroup sourceGroup)
        {
            Device = sourceGroup;

            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings
            {
                SourceGroup = Device,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };

            MediaCapture.InitializeAsync(settings).AsTask().Wait();

        //    MediaEncodingProfile mediaEncodingProfile = new MediaEncodingProfile();

        //MediaEncodingProfile encodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Qvga);

        //    Windows.UI.Xaml.Controls.CaptureElement
        //var mfExtension = await mediaSink.InitializeAsync(encodingProfile.Audio, encodingProfile.Video);
        //MediaCapture.StartPreviewToCustomSinkAsync(mediaEncodingProfile, mediaExtension);
            //MediaCapture.StartPreviewAsync().AsTask().Wait();

            //var infraredTorchControl = MediaCapture.VideoDeviceController.InfraredTorchControl;

            //if (infraredTorchControl.IsSupported)
            //{
            //    //infraredTorchControl.SupportedModes
            //}

            var frameSources = MediaCapture.FrameSources;
            var frameSource = frameSources.First().Value;

            var preferredFormat = frameSource.SupportedFormats.First();

            frameSource.SetFormatAsync(preferredFormat).AsTask().Wait();

            MediaFrameReader = MediaCapture.CreateFrameReaderAsync(frameSource).AsTask().Result;
            MediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;

            MediaFrameReader.FrameArrived += FrameArrived;

            MediaFrameReader.StartAsync().AsTask().Wait();
        }

        private void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            using (var frameReference = sender.TryAcquireLatestFrame())
            {
                var videoMediaFrame = frameReference?.VideoMediaFrame;
                
                var softwareBitmap = videoMediaFrame?.SoftwareBitmap;

                if (softwareBitmap == null) return;
                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                softwareBitmap = Interlocked.Exchange(ref _backBuffer, softwareBitmap);
                softwareBitmap?.Dispose();
                
                SoftwareBitmap latestBitmap;
                while ((latestBitmap = Interlocked.Exchange(ref _backBuffer, null)) != null)
                {
                    var isIlluminated = videoMediaFrame.InfraredMediaFrame.IsIlluminated; // This filter gives a similar result to having the torch enabled or disabled even if we can't control the torch. (It halves framerate tho)
                    if (FrameFilter == IRFrameFilter.None || (!isIlluminated && FrameFilter == IRFrameFilter.Raw) || (isIlluminated && FrameFilter == IRFrameFilter.Illuminated))
                        OnFrameReady(latestBitmap);
                    //latestBitmap.Dispose(); Needs to be done by the event handler.
                }
            }
        }
    }
}
