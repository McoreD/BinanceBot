﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BinanceBotLib
{
    public static class Statistics
    {
        public static string GetTotalProfit()
        {
            return Bot.Settings.TotalProfit.ToString();
        }

        public static string GetProfitPerDay()
        {
            double totalDays = (DateTime.Now - Bot.Settings.StartDate).TotalDays;
            return Math.Round(Bot.Settings.TotalProfit / (decimal)totalDays, 2).ToString();
        }

        public static string GetTotalInvestment()
        {
            decimal cost = 0m;

            foreach (TradingData trade in Bot.Settings.TradingDataList)
            {
                cost += trade.CapitalCost;
            }

            return cost.ToString();
        }
    }
}