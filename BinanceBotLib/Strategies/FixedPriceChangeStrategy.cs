﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BinanceBotLib
{
    public class FixedPriceChangeStrategy : Strategy
    {
        public FixedPriceChangeStrategy(ExchangeType exchangeType) : base(exchangeType)
        {
        }

        public override void Trade()
        {
            Trade(Bot.Settings.TradingDataList);
        }

        public override void Trade(List<TradingData> tradesList)
        {
            // Check USDT and BTC balances
            decimal coins = _client.GetBalance(Bot.Settings.CoinPair.Pair1);
            decimal fiatValue = _client.GetBalance(Bot.Settings.CoinPair.Pair2);

            // Check if user has more USDT or more BTC
            decimal coinsValue = coins * _client.GetPrice(Bot.Settings.CoinPair);

            // cleanup
            tradesList.RemoveAll(trade => trade.BuyOrderID > -1 && trade.SellOrderID > -1);

            // If no buy or sell orders for the required coin pair, then place an order
            TradingData tdSearch = tradesList.Find(x => x.CoinPair.Pair1 == Bot.Settings.CoinPair.Pair1);
            if (tdSearch == null)
            {
                // buy or sell?
                if (fiatValue > coinsValue)
                {
                    // buy
                    PlaceBuyOrder(GetNewTradingData(), Bot.Settings.TradingDataList);
                }
                else
                {
                    // sell
                    TradingData trade0 = GetNewTradingData();
                    trade0.CoinQuantity = Math.Round(coins / Bot.Settings.HydraFactor, 5);
                    tradesList.Add(trade0); // Add because this is the seed
                    PlaceSellOrder(trade0);
                }
            }
            else
            {
                // monitor market price for price changes
                Console.WriteLine();
                OnStarted();
                foreach (TradingData trade in tradesList)
                {
                    trade.MarketPrice = _client.GetPrice(trade.CoinPair);
                    trade.PriceChangePercentage = (trade.MarketPrice - trade.BuyPriceAfterFees) / trade.BuyPriceAfterFees * 100;
                    Console.WriteLine(trade.ToStringPriceCheck());
                    OnTradeListItemHandled(trade);
                    // sell if positive price change
                    if (trade.PriceChangePercentage > Bot.Settings.PriceChangePercentage)
                    {
                        PlaceSellOrder(trade);
                    }
                    Thread.Sleep(200);
                }

                if (tradesList.Last<TradingData>().PriceChangePercentage < Bot.Settings.PriceChangePercentage * -1)
                {
                    // buy more if negative price change
                    PlaceBuyOrder(GetNewTradingData(), Bot.Settings.TradingDataList);
                }
            }

            OnCompleted();
        }

        public TradingData GetNewTradingData()
        {
            return new TradingData() { CoinPair = CoinPairs.GetCoinPair() };
        }
    }
}