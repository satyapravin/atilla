using Exchange;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FutureMM
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
                var spot = settings["Spot"];
                var future = settings["Future"];
                symbols.Add(spot);
                symbols.Add(future);
                List<int> hours = new List<int>();
                hours.Add(4);
                hours.Add(12);
                hours.Add(20);
                BitmexExchange bitmexExchange = new BitmexExchange(settings["KEY"],
                    settings["SECRET"], bool.Parse(settings["PROD"]), symbols, indices, funding, hours);
                var strategy = new FutureMMStrategy(bitmexExchange, spot, future);
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
