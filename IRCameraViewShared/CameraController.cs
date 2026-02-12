using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Devices;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;

namespace IRCameraView
{
	public enum IRFrameFilter
	{
		None,
		Raw,
		Illuminated
	}

	public enum IRMappingMode
	{
		None,
		Green
	}

	public class CameraController
    {
		[System.Runtime.InteropServices.Guid("5B0D3235-4DBA-4D44-865E-8F1D0E1B3D3A")]
		[System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
		interface IMemoryBufferByteAccess
		{
			void GetBuffer(out byte[] buffer, out uint capacity);
		}

		public MediaFrameReader? MediaFrameReader { get; private set; }
		public MediaPlayer MediaPlayer { get; private set; }
		public MediaCapture? MediaCapture { get;
			private set; }
		public List<MediaFrameSourceGroup> Devices { get; private set; }
		public MediaFrameSourceGroup SourceGroup { get; private set; }

		public IRFrameFilter FrameFilter { get; set; }
		public IRMappingMode MappingMode { get; set; }

        public SoftwareBitmap LatestBitmap { get
			{
				return _latestBitmap;
			}
			set
			{
				if(_latestBitmap!=null) _latestBitmap.Dispose();
				_latestBitmap = value;
			}
		}
        private SoftwareBitmap _latestBitmap;


        public VideoDeviceController? Controller { get {
				return MediaCapture?.VideoDeviceController; } }

		public delegate void FrameReady(SoftwareBitmap bitmap);
		public event FrameReady OnFrameReady;

		private SoftwareBitmap _backBuffer;
		//private bool _isInitialized = false;

		public CameraController()
		{
			FrameFilter = IRFrameFilter.None;
			MappingMode = IRMappingMode.None;
			MediaCapture = null;
			LoadCameras(MediaFrameSourceKind.Infrared);

			if (Devices?.Count == 0)
				throw new Exception("No infrared cameras were found.");
		}

        public List<MediaFrameSourceGroup> LoadCameras(MediaFrameSourceKind allowedKind)
		{
			return LoadCameras([allowedKind]);
		}

        public List<MediaFrameSourceGroup> LoadCameras(List<MediaFrameSourceKind>? allowedKinds = null)
		{
			Devices = new List<MediaFrameSourceGroup>();
			var frameSources = MediaFrameSourceGroup.FindAllAsync().AsTask().Result;

			// Filter out unwanted camera types
			foreach (var device in frameSources)
				foreach (var sourceInfo in device.SourceInfos)
					if (allowedKinds == null || allowedKinds.Contains(sourceInfo.SourceKind))
						Devices.Add(device);

			return Devices;
		}

		public void SelectDevice(MediaFrameSourceGroup sourceGroup, bool exclusive = true)
		{
			if (MediaFrameReader != null)
			{
				MediaFrameReader.FrameArrived -= FrameArrived;
				MediaFrameReader.StopAsync().AsTask().Wait();
				MediaFrameReader.Dispose();
				MediaFrameReader = null;
			}


			if (MediaCapture != null)
			{
				MediaCapture.Dispose();
				MediaCapture = null;
			}


			SourceGroup = sourceGroup;

			var mediaCapture = new MediaCapture();

			var profiles = MediaCapture.FindAllVideoProfiles(sourceGroup.Id);

			foreach (var profile in profiles)
			{
				var infos = profile.FrameSourceInfos.FirstOrDefault();
				var recordMedia = profile.SupportedRecordMediaDescription.FirstOrDefault();
				var keys = infos.DeviceInformation.Properties.Keys;
				var values = infos.DeviceInformation.Properties.Values;

				for (int i = 0; i < keys.Count(); ++i)
				{
					var key = keys.ElementAt(i);
					var value = values.ElementAt(i);
				}
			}

			MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings
			{
				SourceGroup = SourceGroup = sourceGroup,
				SharingMode = exclusive ? MediaCaptureSharingMode.ExclusiveControl : MediaCaptureSharingMode.SharedReadOnly,
				StreamingCaptureMode = StreamingCaptureMode.Video,
				MemoryPreference = MediaCaptureMemoryPreference.Cpu,
			};

			mediaCapture.InitializeAsync(settings).AsTask().Wait();

			

			//    MediaEncodingProfile mediaEncodingProfile = new MediaEncodingProfile();

			//MediaEncodingProfile encodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Qvga);

			//    Windows.UI.Xaml.Controls.CaptureElement
			//var mfExtension = await mediaSink.InitializeAsync(encodingProfile.Audio, encodingProfile.Video);
			//MediaCapture.StartPreviewToCustomSinkAsync(mediaEncodingProfile, mediaExtension);
			//MediaCapture.StartPreviewAsync().AsTask().Wait();

			var frameSources = mediaCapture.FrameSources;
			if (frameSources.Count == 0) return;
			var frameSource = frameSources.First().Value;

			var preferredFormat = frameSource.SupportedFormats.First();

			frameSource.SetFormatAsync(preferredFormat).AsTask().Wait();

			MediaFrameReader = mediaCapture.CreateFrameReaderAsync(frameSource).AsTask().Result;
			MediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;

			MediaFrameReader.FrameArrived += FrameArrived;

			MediaFrameReader.StartAsync().AsTask().Wait();

            MediaCapture = mediaCapture;
        }

		public void SelectDeviceByIndex(int index)
		{
			if (index < 0 || index >= Devices.Count)
				throw new ArgumentOutOfRangeException(nameof(index), "Invalid device index.");

			SelectDevice(Devices[index]);
		}

		public List<string> GetDeviceNames()
		{
			return Devices.Select(d => d.DisplayName).ToList();
		}

		public void CaptureImage()
		{
            CaptureImage(LatestBitmap);
        }

        public void CaptureImage(SoftwareBitmap bitmap)
        {
            SaveBitmap(SoftwareBitmap.Copy(bitmap));
        }

        private async Task SaveBitmap(SoftwareBitmap bitmap)
        {
			if (bitmap == null) return;
            try
            {

                StorageFile file = await KnownFolders.PicturesLibrary.CreateFileAsync("Infrared.jpg", CreationCollisionOption.GenerateUniqueName);

                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
					
                    encoder.SetSoftwareBitmap(bitmap);

                    var propertySet = new BitmapPropertySet();
                    var qualityValue = new BitmapTypedValue(0.9, PropertyType.Single);
                    propertySet.Add("ImageQuality", qualityValue);

                    await encoder.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save image: {ex.Message}");
            }
        }

        public static SoftwareBitmap ConvertToGreenOnly(SoftwareBitmap inputBitmap)
		{
            if (inputBitmap == null)
                throw new ArgumentNullException(nameof(inputBitmap));

            var bitmap = inputBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8
                ? SoftwareBitmap.Convert(inputBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied)
                : SoftwareBitmap.Copy(inputBitmap);

            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            byte[] pixels = new byte[width * height * 4];
            bitmap.CopyToBuffer(pixels.AsBuffer());

            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte g = pixels[i + 1];
                byte a = pixels[i + 3];

                pixels[i + 0] = 0;
                pixels[i + 1] = g;
                pixels[i + 2] = 0;
                pixels[i + 3] = a;
            }

            var result = new SoftwareBitmap(BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);
            result.CopyFromBuffer(pixels.AsBuffer());

            return result;
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

				SoftwareBitmap hi;
				while ((hi = Interlocked.Exchange(ref _backBuffer, null)) != null)
				{

                    if (MappingMode == IRMappingMode.Green)
                        hi = ConvertToGreenOnly(hi);

					var isIlluminated = videoMediaFrame.InfraredMediaFrame.IsIlluminated; // This filter gives a similar result to having the torch enabled or disabled even if we can't control the torch. (It halves framerate tho)
					if (OnFrameReady != null && (FrameFilter == IRFrameFilter.None || (!isIlluminated && FrameFilter == IRFrameFilter.Raw) || (isIlluminated && FrameFilter == IRFrameFilter.Illuminated)))
						OnFrameReady(LatestBitmap = SoftwareBitmap.Copy(hi));
				}

                softwareBitmap?.Dispose();
            }
		}
	}
}
