﻿using Binance.Net;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using ShareX.HelpersLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace BinanceBotLib
{
    public class Bot
    {
        public static readonly ExchangeType _exchangeType = ExchangeType.BinanceExchange;
        private System.Timers.Timer _marketTimer = new System.Timers.Timer();
        public Strategy Strategy { get; private set; }

        #region IO

        public static readonly string PersonalFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BinanceBot");
        private Settings _settings = null;

        public static string SettingsFilePath
        {
            get
            {
                return Path.Combine(PersonalFolder, "TradingView-Settings.json");
            }
        }

        public static string LogFilePath
        {
            get
            {
                string logsFolder = Path.Combine(PersonalFolder, "Logs");
                string filename = string.Format("TradingView-BinanceBot-Log-{0:yyyy-MM}.log", DateTime.Now);
                return Path.Combine(logsFolder, filename);
            }
        }
        private static Logger logger = new Logger(Bot.LogFilePath);

        public static Settings LoadSettings()
        {
            return Settings.Load(SettingsFilePath);
        }

        public static void SaveSettings(Settings settings)
        {
            if (settings != null)
            {
                settings.Save(SettingsFilePath);
            }
        }

        public static void WriteConsole(string message = "")
        {
            if (_exchangeType != ExchangeType.MockupExchange)
            {
                Console.WriteLine(message);
            }
        }

        public static void WriteLog(string message)
        {
            if (_exchangeType != ExchangeType.MockupExchange)
            {
                Console.WriteLine(message);
                logger.WriteLine(message);
            }
        }

        #endregion IO

        public Bot(Settings settings)
        {
            _settings = settings;
            Init();
        }

        private void Init()
        {
            double timerInterval = _exchangeType == ExchangeType.BinanceExchange ? MathHelpers.Random(60, 120) * 1000 : 1;

            switch (_settings.BotMode)
            {
                case BotMode.FixedProfit:
                    _marketTimer.Interval = timerInterval; // Randomly every 1-2 minutes (60-120)
                    Strategy = new FixedProfitStrategy(_exchangeType, _settings);
                    break;

                case BotMode.FixedPriceChange:
                    _marketTimer.Interval = timerInterval; // Randomly every 1-2 minutes (60-120)
                    Strategy = new FixedPriceChangeStrategy(_exchangeType, _settings);
                    break;

                case BotMode.TradingViewSignal:
                    _marketTimer.Interval = 5000; // Every 5 seconds
                    Strategy = new TradingViewAlertStrategy(_exchangeType, _settings);
                    break;

                default:
                    Console.WriteLine("Unhandled Bot Mode.");
                    Console.ReadLine();
                    return;
            }
        }

        public void Start()
        {
            Init();
#if DEBUG
            Strategy.Activate();
#endif
            _marketTimer.Elapsed += MarketTimer_Tick;
            _marketTimer.Start();
        }

        public void Stop()
        {
            _marketTimer.Stop();
        }

        private void MarketTimer_Tick(object sender, ElapsedEventArgs e)
        {
            if (_exchangeType != ExchangeType.MockupExchange && string.IsNullOrEmpty(_settings.APIKey))
                throw new Exception("Settings reset!");

#if DEBUG
            try
            {
                Strategy.Activate();
            }
            catch (ArgumentOutOfRangeException) // Mockup Exchange Client
            {
                if (_exchangeType == ExchangeType.MockupExchange)
                {
                    string result = $"{_settings.HydraFactor},{_settings.PriceChangePercentageDown},{_settings.PriceChangePercentageUp},{Strategy.Statistics.GetPortfolioValue()}{Strategy.Statistics.GetCoinsBalanceCsv()}";
                    Console.WriteLine(result);
                    Stop();
                }
            }

#endif

#if RELEASE
            try
            {
                NativeMethods.PreventSleep();
                Strategy.Activate();
            }
            catch (Exception ex)
            {
                Bot.WriteLog(ex.Message);
            }
#endif
        }
    }
}