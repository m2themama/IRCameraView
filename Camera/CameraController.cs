using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Devices;
using Windows.Media.Playback;

namespace IRCameraView.Camera
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
		public MediaCapture? MediaCapture { get; private set; }
		public List<MediaFrameSourceGroup> Devices { get; private set; }
		public MediaFrameSourceGroup SourceGroup { get; private set; }

		public IRFrameFilter FrameFilter { get; set; }
		public IRMappingMode MappingMode { get; set; }

		public VideoDeviceController? Controller { get { return MediaCapture != null ? MediaCapture.VideoDeviceController : null; } }

		public delegate void FrameReady(SoftwareBitmap bitmap);
		public event FrameReady OnFrameReady;

		private SoftwareBitmap _backBuffer;
		//private bool _isInitialized = false;

		public CameraController()
		{
			FrameFilter = IRFrameFilter.None;
			MappingMode = IRMappingMode.None;
			MediaCapture = new MediaCapture();
			LoadCameras(MediaFrameSourceKind.Infrared);

			if (Devices.Count == 0)
				throw new Exception("No infrared cameras were found.");
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

		public void SelectDevice(MediaFrameSourceGroup sourceGroup, bool exclusive = true)
		{
			// Clean up previous MediaFrameReader
			if (MediaFrameReader != null)
			{
				MediaFrameReader.FrameArrived -= FrameArrived;
				MediaFrameReader.StopAsync().AsTask().Wait();
				MediaFrameReader.Dispose();
				MediaFrameReader = null;
			}

			// Clean up previous MediaCapture
			if (MediaCapture != null)
			{
				MediaCapture.Dispose();
				MediaCapture = null;
			}

			SourceGroup = sourceGroup;

			MediaCapture = new MediaCapture();

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

			MediaCapture.InitializeAsync(settings).AsTask().Wait();

			_isInitialized = true;

			//    MediaEncodingProfile mediaEncodingProfile = new MediaEncodingProfile();

			//MediaEncodingProfile encodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Qvga);

			//    Windows.UI.Xaml.Controls.CaptureElement
			//var mfExtension = await mediaSink.InitializeAsync(encodingProfile.Audio, encodingProfile.Video);
			//MediaCapture.StartPreviewToCustomSinkAsync(mediaEncodingProfile, mediaExtension);
			//MediaCapture.StartPreviewAsync().AsTask().Wait();

			var frameSources = MediaCapture.FrameSources;
			var frameSource = frameSources.First().Value;

			var preferredFormat = frameSource.SupportedFormats.First();

			frameSource.SetFormatAsync(preferredFormat).AsTask().Wait();

			MediaFrameReader = MediaCapture.CreateFrameReaderAsync(frameSource).AsTask().Result;
			MediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;

			MediaFrameReader.FrameArrived += FrameArrived;

			MediaFrameReader.StartAsync().AsTask().Wait();
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


		public static SoftwareBitmap ConvertToGreenOnly(SoftwareBitmap inputBitmap)
		{
			if (inputBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
			{
				inputBitmap = SoftwareBitmap.Convert(inputBitmap, BitmapPixelFormat.Bgra8);
			}

			int width = inputBitmap.PixelWidth;
			int height = inputBitmap.PixelHeight;
			var buffer = new Windows.Storage.Streams.Buffer((uint)(width * height));
			//Windows.Storage.Streams.Buffer
			inputBitmap.CopyToBuffer(buffer);

			for (int i = 0; i < 90; i++)
			{
				for (global::System.UInt32 j = 0; j < buffer.Length; j++)
				{
					//buffer. = 0;
				}
			}

			inputBitmap.CopyFromBuffer(buffer);

			return inputBitmap;
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
					if (MappingMode == IRMappingMode.Green)
						latestBitmap = ConvertToGreenOnly(latestBitmap);
					var isIlluminated = videoMediaFrame.InfraredMediaFrame.IsIlluminated; // This filter gives a similar result to having the torch enabled or disabled even if we can't control the torch. (It halves framerate tho)
					if (FrameFilter == IRFrameFilter.None || (!isIlluminated && FrameFilter == IRFrameFilter.Raw) || (isIlluminated && FrameFilter == IRFrameFilter.Illuminated))
						OnFrameReady(latestBitmap);
					//latestBitmap.Dispose(); Needs to be done by the event handler.
				}
			}
		}
	}
}
