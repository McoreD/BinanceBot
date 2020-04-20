﻿using BinanceBotLib;
using ExchangeClientLib;
using ShareX.HelpersLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotOptimiser
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Logger logger = new Logger("BacktestDataLogger.log");

            for (int hydraFactor = 10; hydraFactor <= 20; hydraFactor++)
            {
                for (decimal priceChangePerc = 1.0m; priceChangePerc <= 4.0m; priceChangePerc++)
                {
                    try
                    {
                        Settings settings = new Settings()
                        {
                            CoinPair = new CoinPair("BTC", "USDT", 6),
                            HydraFactor = hydraFactor,
                            PriceChangePercentage = priceChangePerc,
                            BotMode = BotMode.FixedPriceChange
                        };

                        Bot.Start(settings);
                    }
                    catch (Exception ex)
                    {
                        Bot.Stop();
                        logger.WriteLine($"HydraFactor = {hydraFactor} PriceChangePerc = {priceChangePerc} Total Price = {Statistics.GetPortfolioValue()}");
                    }
                }
            }

            Console.ReadLine();
        }
    }
}