﻿using NodaTime;
using NUnit.Framework;
using QuantConnect.Brokerages.Binance;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Logging;
using QuantConnect.Securities;
using System;

namespace QuantConnect.Tests.Brokerages.Binance
{
    [TestFixture]
    public partial class BinanceBrokerageTests
    {
        public TestCaseData[] ValidHistory
        {
            get
            {
                return new[]
                {
                    // valid 
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Minute, Time.OneHour, false),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Hour, Time.OneDay, false),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Daily, TimeSpan.FromDays(15), false),
                };
            }
        }

        public TestCaseData[] NoHistory
        {
            get
            {
                return new[]
                {
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Tick, TimeSpan.FromSeconds(15)),
                    new TestCaseData(Symbol.Create("ETHUSDT", SecurityType.Crypto, Market.Binance), Resolution.Second, Time.OneMinute),
                };
            }
        }

        public TestCaseData[] InvalidHistory
        {
            get
            {
                return new[]
                {
                    // invalid period, no error, empty result
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(-15), true),

                    // invalid symbol, throws "System.ArgumentException : Unknown symbol: XYZ"
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.Crypto, Market.Binance), Resolution.Daily, TimeSpan.FromDays(15), true),

                    // invalid security type, throws "System.ArgumentException : Invalid security type: Equity"
                    new TestCaseData(Symbols.AAPL, Resolution.Daily, TimeSpan.FromDays(15), true),
                };
            }
        }

        [Test]
        [TestCaseSource("ValidHistory")]
        [TestCaseSource("InvalidHistory")]
        public void GetsHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool throwsException)
        {
            TestDelegate test = () =>
            {
                var brokerage = (BinanceBrokerage)Brokerage;

                var historyProvider = new BrokerageHistoryProvider();
                historyProvider.SetBrokerage(brokerage);
                historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false));

                var now = DateTime.UtcNow;

                var requests = new[]
                {
                    new HistoryRequest(now.Add(-period),
                                       now,
                                       typeof(TradeBar),
                                       symbol,
                                       resolution,
                                       SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                                       DateTimeZone.Utc,
                                       Resolution.Minute,
                                       false,
                                       false,
                                       DataNormalizationMode.Adjusted,
                                       TickType.Quote)
                };

                var history = historyProvider.GetHistory(requests, TimeZones.Utc);

                foreach (var slice in history)
                {
                    if (resolution == Resolution.Tick)
                    {
                        foreach (var tick in slice.Ticks[symbol])
                        {
                            Log.Trace("{0}: {1} - {2} / {3}", tick.Time.ToStringInvariant("yyyy-MM-dd HH:mm:ss.fff"), tick.Symbol, tick.BidPrice, tick.AskPrice);
                        }
                    }
                    else
                    {
                        var bar = slice.Bars[symbol];

                        Log.Trace("{0}: {1} - O={2}, H={3}, L={4}, C={5}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close);
                    }
                }

                Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);
            };

            if (throwsException)
            {
                Assert.Throws<ArgumentException>(test);
            }
            else
            {
                Assert.DoesNotThrow(test);
            }
        }

        [Test]
        [TestCaseSource("NoHistory")]
        public void GetEmptyHistory(Symbol symbol, Resolution resolution, TimeSpan period)
        {
            TestDelegate test = () =>
            {
                var brokerage = (BinanceBrokerage)Brokerage;

                var historyProvider = new BrokerageHistoryProvider();
                historyProvider.SetBrokerage(brokerage);
                historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null,false));

                var now = DateTime.UtcNow;

                var requests = new[]
                {
                    new HistoryRequest(now.Add(-period),
                                       now,
                                       typeof(TradeBar),
                                       symbol,
                                       resolution,
                                       SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                                       DateTimeZone.Utc,
                                       Resolution.Minute,
                                       false,
                                       false,
                                       DataNormalizationMode.Adjusted,
                                       TickType.Quote)
                };

                var history = historyProvider.GetHistory(requests, TimeZones.Utc);

                Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);
                Assert.AreEqual(0, historyProvider.DataPointCount);
            };

            Assert.DoesNotThrow(test);
        }
    }
}
