﻿using System.Windows;

namespace MDF_Manager
{
    /// <summary>Interaction logic for Window1.xaml</summary>
    public partial class ColorCanvas : Window
    {
        public ColorCanvas()
        {
            InitializeComponent();
        }

        public void Confirm(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ApplyColor(object sender, RoutedEventArgs e)
        {
            try
            {
                colorCanvas.HexadecimalString = ColorEntry.Text;
            }
            catch (System.IO.InvalidDataException)
            {
                MessageBox.Show("Invalid color!");
            }
        }
    }
}