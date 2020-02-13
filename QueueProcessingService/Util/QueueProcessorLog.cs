using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;


namespace QueueProcessingService.Util
{
    public class QueueProcessorLog
    {
        private readonly Serilog.Core.Logger _logger;
        public QueueProcessorLog()
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.EventCollector(
                    splunkHost: ConfigurationManager.FetchConfig("Serilog:EventCollectorUrl"),
                    sourceType: "manual",
                    eventCollectorToken: ConfigurationManager.FetchConfig("Serilog:Token")
                    )
                .CreateLogger();
            Serilog.Debugging.SelfLog.Enable(Console.Error);
        }
        public void LogInfomration(String msg)
        {
            _logger.Information(msg);
        }
    }
}
