using System.Collections.Generic;
using System.Windows.Media;

namespace MDF_Manager.Classes
{
    public class Defs
    {
        private Color _Background = new() { R = 226, G = 226, B = 226, A = 255 };
        private Color _Foreground = new() { R = 255, G = 255, B = 255, A = 255 };
        private Color _Windows = new() { R = 255, G = 255, B = 255, A = 255 };
        private Color _Button = new() { R = 226, G = 226, B = 226, A = 255 };
        private Color _Text = new() { R = 0, G = 0, B = 0, A = 255 };
        public List<string> lastOpenFiles { get; set; }
        public string lastOpenComp { get; set; }
        public string lastOpenLib { get; set; }
        public Color background
        {
            get
            {
                return _Background;
            }

            set
            {
                _Background = value;
            }
        }
        public Color foreground
        {
            get
            {
                return _Foreground;
            }

            set
            {
                _Foreground = value;
            }
        }
        public Color windows
        {
            get
            {
                return _Windows;
            }

            set
            {
                _Windows = value;
            }
        }
        public Color buttons
        {
            get
            {
                return _Button;
            }

            set
            {
                _Button = value;
            }
        }
        public Color text
        {
            get
            {
                return _Text;
            }

            set
            {
                _Text = value;
            }
        }

        public Defs()
        {
            lastOpenFiles = new List<string>();
            lastOpenLib = "";
            lastOpenComp = "";
        }
    }
}