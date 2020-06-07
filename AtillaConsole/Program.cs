using System;
using System.IO;
using AtillaCore;
using BitmexCore;
using BitmexCore.Models;
using BitmexWebSocket;
using ExchangeCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog.Extensions.Logging;

namespace AtillaConsole
{
    class Program
    {
        [Obsolete]
        static void Main(string[] args)
        {
            try
            {
                ILoggerFactory loggerFactory = new LoggerFactory();
                loggerFactory.AddNLog();
                
                AtillaConfig config = JsonConvert.DeserializeObject<AtillaConfig>(File.ReadAllText("atillaConfig.json"));
                
                var webservice = new WebSocketService(new BitmexAuthorization
                {
                    BitmexEnvironment = config.Bitmex.IsProd ? BitmexEnvironment.Prod : BitmexEnvironment.Test,
                    Key = config.Bitmex.Key,
                    Secret = config.Bitmex.Secret
                }, null, loggerFactory);

                var strategy = new AtillaCore.CorrQuantStrategyService(config, 
                    new CustomWebSocketFactory(), null, loggerFactory);
                strategy.Start();

                string str = string.Empty;

                while(!str.Equals("quit"))
                    str = Console.ReadLine();
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e.Message);
            }            
        }
    }
}
