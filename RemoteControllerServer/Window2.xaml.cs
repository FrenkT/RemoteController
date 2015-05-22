﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RemoteControllerServer
{
    /// <summary>
    /// Logica di interazione per Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        public Window2()
        {            
            InitializeComponent();

            //String port = TextBox_ShowPort.Text;
            //String pwd = TextBox_ShowPwd.Text;
            //String ipaddress = TextBox_ShowClient.SelectedText;
            TextBox_ShowPort.Text = "ciao";
            

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;
        }
    }
}