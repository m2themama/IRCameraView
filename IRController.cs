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

            //var a = 
                mediaCapture.InitializeAsync(settings).AsTask().Wait();

            //mediaCapture.InitializeAsync(settings).AsTask().Wait();
            //mediaCapture.Failed([this](auto && sender, auto && args) { MediaCaptureErrorHandler(sender, args); });
            var frameSources = mediaCapture.FrameSources;
            var frameSource = frameSources.First().Value;//\

            //v preferredFormat = frameSource.SupportedFormats().First().Current();
            //frameSource.SetFormatAsync(preferredFormat).get();
            //foreach (var frameSourceGroup in frameSources)
            //{
            //    var a = frameSources[frameSourceGroup];
            //}

            var preferredFormat = frameSource.SupportedFormats.First();

            //frameSourframeSource.Value

            //frameSource.set
            //var preferredFormat = frameSource;
            //frameSource.SetFormatAsync(preferredFormat).get();

            frameSource.SetFormatAsync(preferredFormat).AsTask().Wait();

            var infraredTorchControl = mediaCapture.VideoDeviceController.InfraredTorchControl;
            if (infraredTorchControl.IsSupported)
            {
                infraredTorchControl.CurrentMode = InfraredTorchMode.AlternatingFrameIllumination;
            }
            //m_mediaCapture.StopPreviewAsync();
            var frameReader = mediaCapture.CreateFrameReaderAsync(frameSource).AsTask().Result;
            frameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;

            MediaFrameReader = mediaCapture.CreateFrameReaderAsync(frameSource).AsTask().Result;



            //MediaPlayer = MediaFrameReader.

            //capElement.Stretch(Stretch::Fill);
            ////capElement.Width(preferredFormat.VideoFormat().Width());
            ////capElement.Height(preferredFormat.VideoFormat().Height());
            //capElement.Source(m_mediaCapture);
            ////capElement.Visibility(Visibility::Collapsed);
            ////m_mediaCapture.StartPreviewAsync();
            //auto infraredTorchControl = m_mediaCapture.VideoDeviceController().InfraredTorchControl();
            //if (infraredTorchControl.IsSupported())
            //{
            //    infraredTorchControl.CurrentMode(winrt::Windows::Media::Devices::InfraredTorchMode::AlternatingFrameIllumination);
            //}
            ////m_mediaCapture.StopPreviewAsync();
            //m_frameReader = m_mediaCapture.CreateFrameReaderAsync(frameSource).get();
            //m_frameReader.AcquisitionMode(MediaFrameReaderAcquisitionMode::Realtime);
            //m_frameReader.FrameArrived([this](auto && mfr, auto && args) { FrameArrivedHandler(mfr, args); });
            //MediaFrameReaderStartStatus status = m_frameReader.StartAsync().get();



        }
    }
}
