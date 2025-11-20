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

        public MainWindow()
        {
            InitializeComponent();

            _irController = new IRController();

            // Populate ComboBox with device names
            DeviceComboBox.ItemsSource = _irController.GetDeviceNames();
            if (DeviceComboBox.Items.Count > 0)
                DeviceComboBox.SelectedIndex = 0; // Optionally select the first device by default

            StartCapture();
        }

        private void StartCapture()
        {
            imageElement.Source = new SoftwareBitmapSource();

            _irController = new IRController();
            _irController.OnFrameReady += IrController_OnFrameArrived;

            var photoControl = _irController.Controller.AdvancedPhotoControl;
            if (photoControl.Supported)
            {
                PhotoModeBox.ItemsSource = photoControl.SupportedModes;
                //foreach (var mode in photoControl.SupportedModes)
                //{
                //    ComboBoxItem item = new ComboBoxItem();
                //    PhotoModeBox.ItemsSource = photoControl.SupportedModes;
                //}
            }
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
            var selectedMode = (PhotoModeBox.SelectedItem as AdvancedPhotoMode?) ?? AdvancedPhotoMode.Standard;
            if (selectedMode != null)
                _irController.Controller.AdvancedPhotoControl.Configure(new AdvancedPhotoCaptureSettings { Mode = selectedMode});
        }
    }
}
