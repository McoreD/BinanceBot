﻿using BinanceBotLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BinanceBotWpf
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            foreach (UserData user in SettingsManager.UserProfiles.Users)
            {
                lbUsers.Items.Add(user);
            }

            lbUsers.SelectionChanged += LbUsers_SelectionChanged;
            lbUsers.SelectedIndex = 0;
        }

        private void LbUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserData user = lbUsers.SelectedItem as UserData;
            pgUser.SelectedObject = user;
            pgConfig.SelectedObject = user.Config;
        }
    }
}