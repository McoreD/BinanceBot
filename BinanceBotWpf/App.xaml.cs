﻿using BinanceBotLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BinanceBotWpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>

    public partial class App : Application
    {
        public static Bot Bot = new Bot();

        public App()
        {
            SettingsManager.LoadUserProfiles();
        }
    }
}