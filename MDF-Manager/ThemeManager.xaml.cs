using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MDF_Manager
{
    /// <summary>Interaction logic for ThemeManager.xaml</summary>
    public partial class ThemeManager : Window
    {
        private Color _Background = new() { R = 226, G = 226, B = 226, A = 255 };
        private Color _Foreground = new() { R = 255, G = 255, B = 255, A = 255 };
        private Color _Windows = new() { R = 255, G = 255, B = 255, A = 255 };
        private Color _Button = new() { R = 226, G = 226, B = 226, A = 255 };
        private Color _Text = new() { R = 0, G = 0, B = 0, A = 255 };

        public SolidColorBrush BackgroundColor
        { get { return (SolidColorBrush)HelperFunctions.GetBrushFromColor(_Background); } set { _Background = value.Color; UpdateRects(); } }

        public SolidColorBrush ForegroundColor
        { get { return (SolidColorBrush)HelperFunctions.GetBrushFromColor(_Foreground); } set { _Foreground = value.Color; UpdateRects(); } }

        public SolidColorBrush WindowsColor
        { get { return (SolidColorBrush)HelperFunctions.GetBrushFromColor(_Windows); } set { _Windows = value.Color; UpdateRects(); } }

        public SolidColorBrush ButtonColor
        { get { return (SolidColorBrush)HelperFunctions.GetBrushFromColor(_Button); } set { _Button = value.Color; UpdateRects(); } }

        public SolidColorBrush TextColor
        { get { return (SolidColorBrush)HelperFunctions.GetBrushFromColor(_Text); } set { _Text = value.Color; UpdateRects(); } }

        public ThemeManager()
        {
            InitializeComponent();
            UpdateRects();
        }

        public void UpdateRects()
        {
            Background.Fill = BackgroundColor;
            Foreground.Fill = ForegroundColor;
            Windows.Fill = WindowsColor;
            Buttons.Fill = ButtonColor;
            Text.Fill = TextColor;
        }

        private void SetBackground(object sender, MouseButtonEventArgs e)
        {
            var cc = new ColorCanvas();
            cc.colorCanvas.SelectedColor = _Background;
            if (cc.ShowDialog() == true)
            {
                _Background = (Color)cc.colorCanvas.SelectedColor;
            }
            UpdateRects();
        }

        private void SetForeground(object sender, MouseButtonEventArgs e)
        {
            var cc = new ColorCanvas();
            cc.colorCanvas.SelectedColor = _Foreground;
            if (cc.ShowDialog() == true)
            {
                _Foreground = (Color)cc.colorCanvas.SelectedColor;
            }
            UpdateRects();
        }

        private void SetWindows(object sender, MouseButtonEventArgs e)
        {
            var cc = new ColorCanvas();
            cc.colorCanvas.SelectedColor = _Windows;
            if (cc.ShowDialog() == true)
            {
                _Windows = (Color)cc.colorCanvas.SelectedColor;
            }
            UpdateRects();
        }

        private void SetButton(object sender, MouseButtonEventArgs e)
        {
            var cc = new ColorCanvas();
            cc.colorCanvas.SelectedColor = _Button;
            if (cc.ShowDialog() == true)
            {
                _Button = (Color)cc.colorCanvas.SelectedColor;
            }
            UpdateRects();
        }

        private void SetText(object sender, MouseButtonEventArgs e)
        {
            var cc = new ColorCanvas();
            cc.colorCanvas.SelectedColor = _Text;
            if (cc.ShowDialog() == true)
            {
                _Text = (Color)cc.colorCanvas.SelectedColor;
            }
            UpdateRects();
        }

        private void Confirm(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}