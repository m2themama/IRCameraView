using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace IRCameraView
{
    /// <summary>
    /// The window that displays the camera feed.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            CameraFrame.Navigate(typeof(CameraPage));

            var currentPage = CameraFrame.Content as CameraPage;
            if (currentPage != null)
            {
                SetTitleBar(currentPage.GetTitleBar());
            }

            //SetTitleBar();
        }
    }
}
