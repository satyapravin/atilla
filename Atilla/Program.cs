﻿using Exchange;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quoter;
using Hedger;
using System.Threading;

namespace Atilla
{
    class Program
    {
        static log4net.ILog log = log4net.LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)
        {

            try
            {
                log4net.Config.XmlConfigurator.Configure();
                var settings = ConfigurationManager.AppSettings;
                var symbols = new HashSet<string>();
                var indices = new HashSet<string>();
                var funding = new HashSet<string>();
                var ethbtc = settings["ETHBTC"];
                var eth = settings["ETH"];
                var ethfuture = settings["ETHFUTURE"];
                var btc = settings["BTC"];
                var btcfuture = settings["BTCFUTURE"];
                symbols.Add(ethbtc);
                symbols.Add(eth);
                symbols.Add(btc);
                symbols.Add(ethfuture);
                symbols.Add(btcfuture);
                funding.Add(eth);
                funding.Add(btc);
                List<int> hours = new List<int>();
                hours.Add(4);
                hours.Add(12);
                hours.Add(20);
                BitmexExchange bitmexExchange = new BitmexExchange(settings["KEY"],
                    settings["SECRET"], bool.Parse(settings["PROD"]), symbols, indices, funding, hours);
                CryptoQuantoCorrStrategy strategy = new CryptoQuantoCorrStrategy(bitmexExchange, ethbtc, eth, ethfuture, btc, btcfuture);
                strategy.Start();
                strategy.Join();
                
            }
            catch (Exception e)
            {
                log.Fatal("Program exited", e);
                throw e;
            }
        }
    }
}
