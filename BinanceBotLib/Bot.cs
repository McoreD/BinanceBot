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
    public static class Bot
    {
        #region IO

        public static readonly string PersonalFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BinanceBot");
        public static Settings Settings { get; private set; }
        public static string SettingsFilePath
        {
            get
            {
                return Path.Combine(PersonalFolder, "Settings.json");
            }
        }

        public static string LogFilePath
        {
            get
            {
                string logsFolder = Path.Combine(PersonalFolder, "Logs");
                string filename = string.Format("BinanceBot-Log-{0:yyyy-MM}.log", DateTime.Now);
                return Path.Combine(logsFolder, filename);
            }
        }
        private static Logger logger = new Logger(Bot.LogFilePath);

        public static void LoadSettings()
        {
            Settings = Settings.Load(SettingsFilePath);
        }

        public static void SaveSettings()
        {
            if (Settings != null)
            {
                Settings.Save(SettingsFilePath);
            }
        }

        public static void WriteLog(string message)
        {
            Console.WriteLine(message);
            logger.WriteLine(message);
        }

        #endregion IO

        public delegate void ProgressEventHandler();
        public delegate void TradingEventHandler(TradingData tradingData);

        public static event ProgressEventHandler Started, Completed;
        public static event TradingEventHandler PriceChecked, OrderSucceeded;

        public static List<CoinPair> CoinPairs = new List<CoinPair>()
        {
            new CoinPair("BTC", "USDT"),
            new CoinPair("BCH", "USDT")
        };

        private static bool _init = false;

        private static readonly ExchangeType _exchange = ExchangeType.BinanceExchange;
        private static ExchangeClient _client = null;
        private static System.Timers.Timer _marketTimer = new System.Timers.Timer();

        private static void Init()
        {
            switch (_exchange)
            {
                case ExchangeType.BinanceExchange:
                    _client = new BinanceExchangeClient(Bot.Settings.APIKey, Bot.Settings.SecretKey);
                    break;
                case ExchangeType.SimulatedExchange:
                    _client = new MockupExchangeClient();
                    break;
            }
        }

        public static void Start()
        {
            if (!_init) Init();

#if DEBUG
            SwingTrade();
#endif

            _marketTimer.Interval = MathHelpers.Random(60, 120) * 1000; // Randomly every 1-2 minutes (60-120)
            _marketTimer.Elapsed += MarketTimer_Tick;
            _marketTimer.Start();
        }

        public static void Stop()
        {
            _marketTimer.Stop();
        }

        private static void MarketTimer_Tick(object sender, ElapsedEventArgs e)
        {
            if (string.IsNullOrEmpty(Bot.Settings.APIKey))
                throw new Exception("Settings reset.");

            switch (Bot.Settings.BotMode)
            {
                case BotMode.DayTrade:
                    DayTrade();
                    break;
                case BotMode.SwingTrade:
                    SwingTrade();
                    break;
                default:
                    Console.WriteLine("Unhandled Bot Mode.");
                    Console.ReadLine();
                    return;
            }

            Bot.SaveSettings();
        }

        public static TradingData GetNew()
        {
            return new TradingData() { CoinPair = Bot.Settings.CoinPair };
        }

        public static void DayTrade()
        {
            using (var client = new BinanceClient())
            {
                var queryBuyOrder = client.GetOrder(Bot.Settings.CoinPair.ToString(), orderId: Bot.Settings.LastBuyOrderID); ;
                var querySellOrder = client.GetOrder(Bot.Settings.CoinPair.ToString(), orderId: Bot.Settings.LastSellOrderID);

                if (queryBuyOrder.Data != null)
                {
                    switch (queryBuyOrder.Data.Status)
                    {
                        case OrderStatus.Filled:
                            SellOrderDayTrade();
                            break;
                        case OrderStatus.Canceled:
                            WriteLog($"Buy order {Bot.Settings.LastBuyOrderID} has been cancelled by the user.");
                            BuyOrderDayTrade();
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
                            BuyOrderDayTrade();
                            break;
                        case OrderStatus.Canceled:
                            Bot.WriteLog($"Sell order {Bot.Settings.LastSellOrderID} has been cancelled by the user.");
                            SellOrderDayTrade();
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
                    BuyOrderDayTrade();
                }
                else if (querySellOrder.Data == null)
                {
                    Console.WriteLine("Could not find any previous sell orders.");
                    SellOrderDayTrade();
                }
            }
        }

        private static void BuyOrderDayTrade()
        {
            using (var client = new BinanceClient())
            {
                var accountInfo = client.GetAccountInfo();
                decimal coinsUSDT = accountInfo.Data.Balances.Single(s => s.Asset == Bot.Settings.CoinPair.Pair2).Free;

                decimal myCapitalCost = Bot.Settings.InvestmentMax == 0 ? coinsUSDT : Math.Min(Bot.Settings.InvestmentMax, coinsUSDT);
                Bot.WriteLog("USDT balance to trade = " + myCapitalCost.ToString());

                decimal fees = client.GetTradeFee().Data.Single(s => s.Symbol == Bot.Settings.CoinPair.ToString()).MakerFee;
                decimal myInvestment = myCapitalCost / (1 + fees);

                decimal myRevenue = myCapitalCost + Bot.Settings.DailyProfitTarget;
                Bot.WriteLog($"Receive target = {myRevenue}");

                // New method from: https://docs.google.com/spreadsheets/d/1be6zYuzKyJMZ4Yn_pUmIt-YRTON3YJKbq_lenL2Kldc/edit?usp=sharing
                decimal marketBuyPrice = client.GetPrice(Bot.Settings.CoinPair.ToString()).Data.Price;
                Bot.WriteLog($"Market price = {marketBuyPrice}");

                decimal marketSellPrice = myRevenue * (1 + fees) / (myInvestment / marketBuyPrice);
                decimal priceDiff = marketSellPrice - marketBuyPrice;

                Bot.Settings.BuyPrice = Math.Round(marketBuyPrice - priceDiff / 2, 2);
                Bot.Settings.CoinQuantity = Math.Round(myInvestment / Bot.Settings.BuyPrice, 6);

                Bot.WriteLog($"Buying {Bot.Settings.CoinQuantity} {Bot.Settings.CoinPair.Pair1} for {Bot.Settings.BuyPrice}");
                var buyOrder = client.PlaceOrder(Bot.Settings.CoinPair.ToString(), OrderSide.Buy, OrderType.Limit, quantity: Bot.Settings.CoinQuantity, price: Bot.Settings.BuyPrice, timeInForce: TimeInForce.GoodTillCancel);

                if (buyOrder.Success)
                {
                    // Save Sell Price
                    Bot.Settings.SellPrice = Math.Round(myRevenue * (1 + fees) / Bot.Settings.CoinQuantity, 2);
                    Bot.WriteLog("Target sell price = " + Bot.Settings.SellPrice);

                    decimal priceChange = Math.Round(priceDiff / Bot.Settings.BuyPrice * 100, 2);
                    Console.WriteLine($"Price change = {priceChange}%");
                    Bot.Settings.LastBuyOrderID = buyOrder.Data.OrderId;
                    Bot.WriteLog("Order ID: " + buyOrder.Data.OrderId);
                    Bot.SaveSettings();
                    Console.WriteLine();
                }
            }
        }

        private static void SellOrderDayTrade()
        {
            using (var client = new BinanceClient())
            {
                var accountInfo = client.GetAccountInfo();
                decimal coinsQuantity = accountInfo.Data.Balances.Single(s => s.Asset == Bot.Settings.CoinPair.Pair1).Free;

                // if user has crypto rather than USDT for capital, then calculate SellPrice and CoinQuanitity
                if (Bot.Settings.SellPrice == 0 || Bot.Settings.CoinQuantity == 0)
                {
                    decimal marketBuyPrice = client.GetPrice(Bot.Settings.CoinPair.ToString()).Data.Price;
                    decimal myCapitalCost = Bot.Settings.InvestmentMax == 0 ? marketBuyPrice * coinsQuantity : Math.Min(Bot.Settings.InvestmentMax, marketBuyPrice * coinsQuantity);

                    decimal fees = client.GetTradeFee().Data.Single(s => s.Symbol == Bot.Settings.CoinPair.ToString()).MakerFee;
                    decimal myInvestment = myCapitalCost / (1 + fees);

                    decimal myRevenue = myCapitalCost + Bot.Settings.DailyProfitTarget;

                    decimal marketSellPrice = myRevenue * (1 + fees) / (myInvestment / marketBuyPrice);
                    decimal priceDiff = marketSellPrice - marketBuyPrice;

                    Bot.Settings.BuyPrice = Math.Round(marketBuyPrice - priceDiff / 2, 2);
                    Bot.Settings.CoinQuantity = Math.Round(myInvestment / Bot.Settings.BuyPrice, 6);
                    Bot.Settings.SellPrice = Math.Round(myRevenue * (1 + fees) / Bot.Settings.CoinQuantity, 2);
                }

                if (Bot.Settings.SellPrice > 0 && Bot.Settings.CoinQuantity > 0 && coinsQuantity > Bot.Settings.CoinQuantity)
                {
                    var sellOrder = client.PlaceOrder(Bot.Settings.CoinPair.ToString(), OrderSide.Sell, OrderType.Limit, quantity: Bot.Settings.CoinQuantity, price: Bot.Settings.SellPrice, timeInForce: TimeInForce.GoodTillCancel);

                    if (sellOrder.Success)
                    {
                        Bot.WriteLog($"Sold {Bot.Settings.CoinQuantity} {Bot.Settings.CoinPair.Pair1} for {Bot.Settings.SellPrice}");
                        Bot.Settings.LastSellOrderID = sellOrder.Data.OrderId;
                        Bot.WriteLog("Order ID: " + sellOrder.Data.OrderId);
                        Bot.Settings.TotalProfit += Bot.Settings.DailyProfitTarget;
                        Bot.SaveSettings();
                        Console.WriteLine();
                    }
                }
            }
        }

        public static void SwingTrade()
        {
            // Check USDT and BTC balances

            decimal coins = _client.GetBalance(Bot.Settings.CoinPair.Pair1);
            decimal fiatValue = _client.GetBalance(Bot.Settings.CoinPair.Pair2);

            // Check if user has more USDT or more BTC
            decimal coinsValue = coins * _client.GetPrice(Bot.Settings.CoinPair);

            // cleanup
            Bot.Settings.TradingDataList.RemoveAll(trade => trade.BuyOrderID > -1 && trade.SellOrderID > -1);

            // If no buy or sell orders for the required coin pair, then place an order
            TradingData tdSearch = Bot.Settings.TradingDataList.Find(x => x.CoinPair.Pair1 == Bot.Settings.CoinPair.Pair1);
            if (tdSearch == null)
            {
                // buy or sell?
                if (fiatValue > coinsValue)
                {
                    // buy
                    BuyOrderSwingTrade();
                }
                else
                {
                    // sell
                    TradingData trade0 = GetNew();
                    trade0.CoinQuantity = Math.Round(coins / Bot.Settings.HydraFactor, 5);
                    Bot.Settings.TradingDataList.Add(trade0);
                    SellOrderSwingTrade(trade0);
                }
            }
            else
            {
                // monitor market price for price changes
                Console.WriteLine();
                OnStarted();
                foreach (TradingData trade in Bot.Settings.TradingDataList)
                {
                    trade.MarketPrice = _client.GetPrice(trade.CoinPair);
                    trade.PriceChangePercentage = Math.Round((trade.MarketPrice - trade.BuyPriceAfterFees) / trade.BuyPriceAfterFees * 100, 2);
                    Console.WriteLine(trade.ToStringPriceCheck());
                    OnPriceChecked(trade);
                    // sell if positive price change
                    if (trade.PriceChangePercentage > Bot.Settings.PriceChangePercentage)
                    {
                        SellOrderSwingTrade(trade);
                    }
                    Thread.Sleep(200);
                }

                if (Bot.Settings.TradingDataList.Last<TradingData>().PriceChangePercentage < Bot.Settings.PriceChangePercentage * -1)
                {
                    // buy more if negative price change
                    BuyOrderSwingTrade();
                }
            }

            OnCompleted();
        }

        private static void BuyOrderSwingTrade()
        {
            TradingData trade = GetNew();
            decimal coinsUSDT = _client.GetBalance(trade.CoinPair.Pair2);

            trade.CapitalCost = Math.Round(coinsUSDT / Bot.Settings.HydraFactor, 2);
            if (trade.CapitalCost > Bot.Settings.InvestmentMin)
            {
                trade.MarketPrice = Math.Round(_client.GetPrice(trade.CoinPair) * (1 - Math.Abs(Bot.Settings.BuyBelowPerc) / 100), 2);

                Console.WriteLine();

                decimal fees = _client.GetTradeFee(trade.CoinPair);
                decimal myInvestment = trade.CapitalCost / (1 + fees);
                trade.CoinQuantity = Math.Round(myInvestment / trade.MarketPrice, 5);

                var buyOrder = _client.PlaceBuyOrder(trade);
                if (buyOrder.Success)
                {
                    trade.BuyPriceAfterFees = Math.Round(trade.CapitalCost / trade.CoinQuantity, 2);
                    trade.BuyOrderID = buyOrder.Data.OrderId;
                    trade.ID = Bot.Settings.TradingDataList.Count;
                    Bot.Settings.TradingDataList.Add(trade);
                    Bot.WriteLog(trade.ToStringBought());
                    OnOrderSucceeded(trade);
                }
                else
                {
                    Bot.WriteLog(buyOrder.Error.Message.ToString());
                }
            }
            else
            {
                Console.WriteLine($"Capital cost is too low to buy more.");
            }
        }

        private static void SellOrderSwingTrade(TradingData trade)
        {
            trade.MarketPrice = Math.Round(_client.GetPrice(trade.CoinPair) * (1 + Math.Abs(Bot.Settings.SellAbovePerc) / 100), 2);

            if (trade.MarketPrice > trade.BuyPriceAfterFees)
            {
                trade.CapitalCost = trade.CoinQuantity * trade.MarketPrice;

                if (trade.CapitalCost > Bot.Settings.InvestmentMin)
                {
                    decimal fees = _client.GetTradeFee(trade.CoinPair);
                    decimal myInvestment = trade.CapitalCost / (1 + fees);

                    var sellOrder = _client.PlaceSellOrder(trade);
                    if (sellOrder.Success)
                    {
                        trade.SellPriceAfterFees = Math.Round(myInvestment / trade.CoinQuantity, 2);
                        trade.SellOrderID = sellOrder.Data.OrderId;
                        Bot.WriteLog(trade.ToStringSold());
                        if (trade.BuyPriceAfterFees > 0) Bot.Settings.TotalProfit += trade.Profit;
                        OnOrderSucceeded(trade);
                    }
                }
            }
        }

        #region Event Handlers

        private static void OnStarted()
        {
            Started?.Invoke();
        }

        private static void OnPriceChecked(TradingData data)
        {
            PriceChecked?.Invoke(data);
        }

        private static void OnOrderSucceeded(TradingData data)
        {
            OrderSucceeded?.Invoke(data);
        }

        private static void OnCompleted()
        {
            Completed?.Invoke();
        }

        #endregion Event Handlers
    }
}