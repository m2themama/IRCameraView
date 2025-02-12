using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IRCameraView
{
    /// <summary>
    /// The window that displays the camera feed.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private IRController _irController;

        public MainWindow()
        {
            InitializeComponent();

            StartCapture();
        }

        private void StartCapture()
        {
            imageElement.Source = new SoftwareBitmapSource();

            _irController = new IRController();
            _irController.OnFrameReady += IrController_OnFrameArrived;
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
            if (sender is ComboBox)
            {
                var comboBox = sender as ComboBox;
                _irController.FrameFilter = (IRFrameFilter)comboBox.SelectedIndex;
            }
        }
    }
}
