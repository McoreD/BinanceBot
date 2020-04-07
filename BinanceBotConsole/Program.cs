﻿using Binance.Net;
using Binance.Net.Objects;
using BinanceBotLib;
using ShareX.HelpersLib;
using System;
using System.Collections.Generic;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using CryptoExchange.Net.Objects;

namespace BinanceBotConsole
{
    internal class Program

    {
        private static BotMode _BotMode = BotMode.DayTrade;

        private static void Main(string[] args)
        {
            Bot.LoadSettings();

            // Error handling
            if (string.IsNullOrEmpty(Bot.Settings.APIKey))
            {
                Console.Write("Enter Binance API Key: ");
                Bot.Settings.APIKey = Console.ReadLine();

                Console.Write("Enter Binance Secret Key: ");
                Bot.Settings.SecretKey = Console.ReadLine();

                Bot.SaveSettings(); // Save API Key and Secret Key
            }

            // Choose Bot mode
            foreach (BotMode bm in Enum.GetValues(typeof(BotMode)))
            {
                Console.WriteLine($"{bm.GetIndex()} for {bm.GetDescription()}");
            }
            Console.Write("Choose Bot mode: ");

            int intMode;
            int.TryParse(Console.ReadLine(), out intMode);
            _BotMode = (BotMode)intMode;

            if (Bot.Settings.DailyProfitTarget <= 0)
            {
                Console.WriteLine("Daily Profit Target must be greater than zero!");
                Console.ReadLine();
                return;
            }

            BinanceClient.SetDefaultOptions(new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(Bot.Settings.APIKey, Bot.Settings.SecretKey),
                LogVerbosity = LogVerbosity.Error,
                LogWriters = new List<TextWriter> { Console.Out }
            });

            Random rnd = new Random();
            Timer marketTickTimer = new Timer();
            marketTickTimer.Interval = rnd.Next(60, 120) * 1000; // Randomly every 1-2 minutes (60-120)
            marketTickTimer.Elapsed += MarketTickTimer_Tick;
            marketTickTimer.Start();
            Console.WriteLine("Bot initiated...");

            Console.ReadLine();

            Bot.SaveSettings();
        }

        private static void MarketTickTimer_Tick(object sender, ElapsedEventArgs e)
        {
            Bot.LoadSettings(); // Re-read settings

            switch (_BotMode)
            {
                case BotMode.DayTrade:
                    DayTrade();
                    break;
                case BotMode.SwingTrade:
                    TradingHelper.SwingTrade();
                    break;
                default:
                    Console.WriteLine("Unhandled Bot Mode.");
                    Console.ReadLine();
                    return;
            }

            Bot.SaveSettings();
        }

        public static void DayTrade()
        {
            using (var client = new BinanceClient())
            {
                var queryBuyOrder = client.GetOrder(CoinPairs.BTCUSDT, orderId: Bot.Settings.LastBuyOrderID);
                var querySellOrder = client.GetOrder(CoinPairs.BTCUSDT, orderId: Bot.Settings.LastSellOrderID);

                if (queryBuyOrder.Data != null)
                {
                    switch (queryBuyOrder.Data.Status)
                    {
                        case OrderStatus.Filled:
                            TradingHelper.SellOrderDayTrade();
                            break;
                        case OrderStatus.Canceled:
                            Bot.WriteLog($"Buy order {Bot.Settings.LastBuyOrderID} has been cancelled by the user.");
                            TradingHelper.BuyOrderDayTrade();
                            break;
                        case OrderStatus.New:
                            Console.WriteLine($"Waiting {DateTime.UtcNow - queryBuyOrder.Data.Time} for the {Bot.Settings.BuyPrice} buy order to fill...");
                            break;
                        default:
                            Console.WriteLine("Unhandled buy order outcome. Reload application...");
                            break;
                    }
                }
                else if (querySellOrder.Data != null)
                {
                    switch (querySellOrder.Data.Status)
                    {
                        case OrderStatus.Filled:
                            TradingHelper.BuyOrderDayTrade();
                            break;
                        case OrderStatus.Canceled:
                            Bot.WriteLog($"Sell order {Bot.Settings.LastSellOrderID} has been cancelled by the user.");
                            TradingHelper.SellOrderDayTrade();
                            break;
                        case OrderStatus.New:
                            Console.WriteLine($"Waiting {DateTime.UtcNow - querySellOrder.Data.Time} for the {Bot.Settings.SellPrice} sell order to fill...");
                            break;
                        default:
                            Console.WriteLine("Unhandled sell order outcome. Reload application...");
                            break;
                    }
                }
                else if (queryBuyOrder.Data == null)
                {
                    Console.WriteLine("Could not find any previous buy orders.");
                    TradingHelper.BuyOrderDayTrade();
                }
                else if (querySellOrder.Data == null)
                {
                    Console.WriteLine("Could not find any previous sell orders.");
                    TradingHelper.SellOrderDayTrade();
                }
            }
        }
    }
}