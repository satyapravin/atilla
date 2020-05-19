using Exchange;
using System;
using System.Collections.Generic;
using System.Configuration;
using Quoter;
using System.IO;
using System.Reflection;

namespace Atilla
{
    class Program
    {
        static log4net.ILog log = log4net.LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)
        {

            try
            {
                var logRepository = log4net.LogManager.GetRepository(Assembly.GetEntryAssembly());
                log4net.Config.XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
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
                IExchangeFactory factory = new ExchangeFactory();
                IExchange exchange = factory.CreateExchange(settings["KEY"], settings["SECRET"], 
                                     bool.Parse(settings["PROD"]), symbols, indices, funding, hours);
                IQuotingServiceFactory qfactory = new QuotingServiceFactory();
                CryptoQuantoCorrStrategy strategy = new CryptoQuantoCorrStrategy(exchange, qfactory, ethbtc, eth, ethfuture, btc, btcfuture);
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
