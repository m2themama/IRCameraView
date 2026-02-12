using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml;
using Microsoft.UI.Xaml;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.Capture.Frames;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Graphics.Imaging;
using System.Threading.Tasks;


#if NETFX_CORE
// UWP code
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
#else
// WinUI 3 code
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
#endif

namespace IRCameraView
{
    public sealed partial class CameraPage : Page
    {
        private CameraController camera;
        static StorageFile videoFile;

        public CameraPage()
        {
            InitializeComponent();
            camera = new CameraController();
            StartCapture();
            ReloadDevices();
        }

        void ReloadDevices()
        {
            // Populate ComboBox with device names
            DeviceComboBox.ItemsSource = camera?.GetDeviceNames();
            if (DeviceComboBox.Items.Count > 0)
                DeviceComboBox.SelectedIndex = 0; // Optionally select the first device by default

            //CameraType.ItemsSource = CameraKind;

            if (camera?.Controller == null) return;

            InfraredTorchControl torchControl = camera.Controller.InfraredTorchControl;

            if (torchControl.IsSupported)
            {
                TorchControlGrid.Visibility = Visibility.Visible;
                IRTorchBox.ItemsSource = torchControl.SupportedModes;
            }

            var photoControl = camera.Controller.AdvancedPhotoControl;
            if (photoControl.Supported)
            {
                PhotoModeGrid.Visibility = Visibility.Visible;
                PhotoModeBox.ItemsSource = photoControl.SupportedModes;
            }
        }

        private void StartCapture()
        {
            imageElement.Source = new SoftwareBitmapSource();

            camera = new CameraController();
            camera.OnFrameReady += OnFrameArrived;
        }
        private void OnFrameArrived(SoftwareBitmap bitmap)
        {
            if (imageElement.DispatcherQueue != null) imageElement.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    var imageSource = (SoftwareBitmapSource)imageElement.Source;
                    await imageSource.SetBitmapAsync(bitmap);
                }
                catch { }
            });
        }

        private void FrameFilter_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && camera != null)
            {
                camera.FrameFilter = (IRFrameFilter)comboBox.SelectedIndex;
            }
        }

        private void DeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (camera == null) return;
            if (DeviceComboBox.SelectedIndex >= 0)
                camera.SelectDeviceByIndex(DeviceComboBox.SelectedIndex);
        }

        private async void TakePhoto_Click(object sender, RoutedEventArgs e)
        {
            camera.CaptureImage();
            FlashScreenAsync();
        }


        private async void TakeVideo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ToggleButton toggleButton)
                {
                    if (toggleButton.IsChecked ?? true)
                    {
                        videoFile = await KnownFolders.VideosLibrary.CreateFileAsync("IRRecording.mp4", CreationCollisionOption.GenerateUniqueName);
                        var encodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);

                        var allVideoProfiles = camera?.Controller?.GetAvailableMediaStreamProperties(MediaStreamType.VideoRecord)?.OfType<VideoEncodingProperties>().ToList();
                        if (allVideoProfiles == null) return;

                        var first = allVideoProfiles[0];

                        encodingProfile.Video.Width = first.Width;
                        encodingProfile.Video.Height = first.Height;

                        encodingProfile.Video.FrameRate.Numerator = first.FrameRate.Numerator;
                        encodingProfile.Video.FrameRate.Denominator = first.FrameRate.Denominator;

                        encodingProfile.Audio = null;

                        await camera?.MediaCapture?.StartRecordToStorageFileAsync(encodingProfile, videoFile);
                    }
                    else
                    {
                        await camera.MediaCapture.StopRecordAsync();

                        var successDialog = new ContentDialog()
                        {
                            Title = "Recording Saved",
                            Content = "Video saved to: " + videoFile.Path,
                            CloseButtonText = "OK",
                            XamlRoot = Content.XamlRoot
                        };
                        await successDialog.ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var successDialog = new ContentDialog()
                {
                    Title = "Failed to record",
                    Content = ex.Message,
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot
                };
                await successDialog.ShowAsync();
            }
        }

        private async Task FlashScreenAsync()
        {
            FlashOverlay.Opacity = 1;
            await Task.Delay(100);

            var fadeDuration = TimeSpan.FromMilliseconds(200);
            var animation = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(fadeDuration)
            };
            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, FlashOverlay);
            Storyboard.SetTargetProperty(animation, "Opacity");
            storyboard.Begin();

            await Task.Delay((int)fadeDuration.TotalMilliseconds);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox && camera != null)
                camera.MappingMode = (IRMappingMode)(sender as ComboBox).SelectedIndex;
        }

        private void PhotoModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            camera.Controller.AdvancedPhotoControl.Configure(new AdvancedPhotoCaptureSettings
            {
                Mode = (PhotoModeBox.SelectedItem as AdvancedPhotoMode?) ?? AdvancedPhotoMode.Standard
            });
        }

        private void IRTorchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedMode = (IRTorchBox.SelectedItem as InfraredTorchMode?) ?? InfraredTorchMode.AlternatingFrameIllumination;
            camera.Controller.InfraredTorchControl.CurrentMode = selectedMode;
        }

        MediaFrameSourceKind GetSelectedCameraKind()
        {
            switch (CameraType.SelectedIndex)
            {
                default:
                case 0: return MediaFrameSourceKind.Infrared;
                case 1: return MediaFrameSourceKind.Color;
                case 2: return MediaFrameSourceKind.Depth;
            }
        }

        private void CameraType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            camera?.LoadCameras(GetSelectedCameraKind());
            ReloadDevices();
        }
    }
}
