using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage;
using System.Collections.Generic;

using IRCameraView.Camera;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IRCameraView
{
    /// <summary>
    /// The window that displays the camera feed.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private CameraController camera;
        //private bool _isRecording = false;
        private List<AdvancedSetting> _advancedSettings;
        //public MediaFrameSourceKind CameraKind { get; set; } = MediaFrameSourceKind.Infrared;

        public MainWindow()
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(Body);

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

        private void BuildSetting(object control)
        {
            //AdvancedSettingsList.ItemsSource
        }

        private void OnFrameArrived(SoftwareBitmap bitmap)
        {
            if (imageElement.DispatcherQueue != null) imageElement.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    var imageSource = (SoftwareBitmapSource)imageElement.Source;
                    await imageSource.SetBitmapAsync(bitmap);
                    bitmap.Dispose();
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
            if (DeviceComboBox.SelectedIndex >= 0)
            {
                camera.SelectDeviceByIndex(DeviceComboBox.SelectedIndex);
                // Optionally, update UI or start preview, etc.
            }
        }

        private async void TakePhoto_Click(object sender, RoutedEventArgs e)
        {
            var photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync("IRPhoto.jpg", CreationCollisionOption.GenerateUniqueName);

            var encodingProperties = new ImageEncodingProperties
            {
                
            };

            using var stream = await photoFile.OpenAsync(FileAccessMode.ReadWrite);
            await camera.MediaCapture?.CapturePhotoToStreamAsync(encodingProperties, stream);
        }

        static StorageFile videoFile;

        private async void TakeVideo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton)
            {
                if (toggleButton.IsChecked ?? true)
                {
                    videoFile = await KnownFolders.VideosLibrary.CreateFileAsync("IRRecording.mp4", CreationCollisionOption.GenerateUniqueName);
                    var encodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);

                    await camera.MediaCapture.StartRecordToStorageFileAsync(encodingProfile, videoFile);
                }
                else
                {
                    await camera.MediaCapture.StopRecordAsync();

                    var successDialog = new ContentDialog()
                    {
                        Title = "Recording Saved",
                        Content = "Video saved to: " + videoFile.Path,
                        CloseButtonText = "OK"
                    };
                    await successDialog.ShowAsync();
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox && camera != null)
                camera.MappingMode = (IRMappingMode)(sender as ComboBox).SelectedIndex;
        }

        private void PhotoModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            camera.Controller.AdvancedPhotoControl.Configure(new AdvancedPhotoCaptureSettings {
                Mode = (PhotoModeBox.SelectedItem as AdvancedPhotoMode?) ?? AdvancedPhotoMode.Standard
            });
        }

        private void IRTorchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedMode = (IRTorchBox.SelectedItem as InfraredTorchMode?) ?? InfraredTorchMode.AlternatingFrameIllumination;
            camera.Controller.InfraredTorchControl.CurrentMode = selectedMode;
        }

        class AdvancedSetting
        {
            //public AdvancedSetting(object, ) { }
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
