using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Media.Playback;
using Windows.Media.Devices;

namespace IRCameraView
{
    public class IRController
    {
        public MediaFrameReader MediaFrameReader { get; private set; }
        public MediaPlayer MediaPlayer { get; private set; }
        public MediaCapture MediaCapture { get; private set; }

        public IRController()
        {
            MediaCapture mediaCapture = new MediaCapture();

            //mediaCapture.InitializeAsync().AsTask().Wait();
            MediaCapture = mediaCapture;
            var irDevices = new List<MediaFrameSourceGroup>();
            var devices = MediaFrameSourceGroup.FindAllAsync().AsTask().Result;
            foreach (var mdevice in devices)
            {
                var currentDevice = mdevice.SourceInfos.First();
                if (currentDevice.SourceKind == MediaFrameSourceKind.Infrared)
                {
                    irDevices.Add(mdevice);
                }
            }

            if (irDevices.Count == 0)
            {
                Console.WriteLine("No IR Cameras found.");
            }

            MediaFrameSourceGroup device = irDevices.FirstOrDefault();

            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings
            {
                SourceGroup = device,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };

            mediaCapture.InitializeAsync(settings).AsTask().Wait();

            var frameSources = mediaCapture.FrameSources;
            var frameSource = frameSources.First().Value;

            var preferredFormat = frameSource.SupportedFormats.First();

            frameSource.SetFormatAsync(preferredFormat).AsTask().Wait();

            var infraredTorchControl = mediaCapture.VideoDeviceController.InfraredTorchControl;
            if (infraredTorchControl.IsSupported)
            {
                infraredTorchControl.CurrentMode = InfraredTorchMode.AlternatingFrameIllumination;
            }

            MediaFrameReader = mediaCapture.CreateFrameReaderAsync(frameSource).AsTask().Result;
            MediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;

            MediaFrameReader.StartAsync().AsTask().Wait();
        }
    }
}
