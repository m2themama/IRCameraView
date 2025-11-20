using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage;
using System.Collections.Generic;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IRCameraView
{
    /// <summary>
    /// The window that displays the camera feed.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private IRCameraController _irController;
        //private bool _isRecording = false;
        private List<AdvancedSetting> _advancedSettings;

        public MainWindow()
        {
            InitializeComponent();

            _irController = new IRCameraController();

            // Populate ComboBox with device names
            DeviceComboBox.ItemsSource = _irController.GetDeviceNames();
            if (DeviceComboBox.Items.Count > 0)
                DeviceComboBox.SelectedIndex = 0; // Optionally select the first device by default

            StartCapture();
        }

        private void StartCapture()
        {
            imageElement.Source = new SoftwareBitmapSource();

            _irController = new IRCameraController();
            _irController.OnFrameReady += IrController_OnFrameArrived;

            InfraredTorchControl torchControl = _irController.Controller.InfraredTorchControl;

            if (torchControl.IsSupported)
            {
                TorchControlGrid.Visibility = Visibility.Visible;
                IRTorchBox.ItemsSource = torchControl.SupportedModes;
            }

            var photoControl = _irController.Controller.AdvancedPhotoControl;
            if (photoControl.Supported)
            {
                PhotoModeGrid.Visibility = Visibility.Visible;
                PhotoModeBox.ItemsSource = photoControl.SupportedModes;
            }
        }

        private void BuildSetting(object control)
        {
            //AdvancedSettingsList.ItemsSource
        }

        private void IrController_OnFrameArrived(SoftwareBitmap bitmap)
        {
            if (imageElement.DispatcherQueue != null) imageElement.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    var imageSource = (SoftwareBitmapSource)imageElement.Source;
                    await imageSource.SetBitmapAsync(bitmap);
                    bitmap.Dispose(); // Important to dispose of.
                }
                catch { }
            });
        }

        private void FrameFilter_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && _irController != null)
            {
                _irController.FrameFilter = (IRFrameFilter)comboBox.SelectedIndex;
            }
        }

        private void DeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceComboBox.SelectedIndex >= 0)
            {
                _irController.SelectDeviceByIndex(DeviceComboBox.SelectedIndex);
                // Optionally, update UI or start preview, etc.
            }
        }

        private async void TakePhoto_Click(object sender, RoutedEventArgs e)
        {
            //_irController.\

            var photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync("IRPhoto.jpg", CreationCollisionOption.GenerateUniqueName);

            var encodingProperties = new ImageEncodingProperties
            {
                Subtype = "Y800"
            };

            using var stream = await photoFile.OpenAsync(FileAccessMode.ReadWrite);
            await _irController.MediaCapture.CapturePhotoToStreamAsync(encodingProperties, stream);
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

                    await _irController.MediaCapture.StartRecordToStorageFileAsync(encodingProfile, videoFile);
                }
                else
                {
                    await _irController.MediaCapture.StopRecordAsync();

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
            if (sender is ComboBox && _irController != null)
                _irController.MappingMode = (IRMappingMode)(sender as ComboBox).SelectedIndex;
        }

        private void PhotoModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _irController.Controller.AdvancedPhotoControl.Configure(new AdvancedPhotoCaptureSettings {
                Mode = (PhotoModeBox.SelectedItem as AdvancedPhotoMode?) ?? AdvancedPhotoMode.Standard
            });
        }

        private void IRTorchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedMode = (IRTorchBox.SelectedItem as InfraredTorchMode?) ?? InfraredTorchMode.AlternatingFrameIllumination;
            _irController.Controller.InfraredTorchControl.CurrentMode = selectedMode;
        }

        class AdvancedSetting
        {
            //public AdvancedSetting(object, ) { }
        }
    }
}
