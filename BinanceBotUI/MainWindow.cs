﻿using BinanceBotLib;
using ShareX.HelpersLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BinanceBotUI
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            this.Text = Application.ProductName;

            Bot.LoadSettings();

            foreach (BotMode botMode in Helpers.GetEnums<BotMode>())
            {
                cboBotMode.Items.Add(botMode.GetDescription());
            }
            cboBotMode.SelectedIndex = 1;

            foreach (CoinPair cp in Bot.CoinPairs)
            {
                cboNewDefaultCoinPair.Items.Add(cp);
            }
            cboNewDefaultCoinPair.SelectedIndex = Bot.CoinPairs.FindIndex(x => x.ToString() == Bot.Settings.CoinPair.ToString());

            lblProfitTotal.Text = "Profit made to-date: $" + Bot.Settings.TotalProfit;
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            Bot.Start();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            SettingsWindow frm = new SettingsWindow();
            frm.ShowDialog();
        }

        private void cboNewDefaultCoinPair_SelectedIndexChanged(object sender, EventArgs e)
        {
            Bot.Settings.CoinPair = cboNewDefaultCoinPair.SelectedItem as CoinPair;
        }

        private void cboBotMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            Bot.Settings.BotMode = (BotMode)cboBotMode.SelectedIndex;
        }
    }
}