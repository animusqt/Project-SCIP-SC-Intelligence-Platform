using System.Windows;

namespace ScipDashboard
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        public void SetStatus(string status)
        {
            StatusText.Text = status;
        }
    }
}
