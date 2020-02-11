using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                    eventCollectorToken: ConfigurationManager.FetchConfig("Serilog:Token"),
                    #pragma warning disable CA2000 // Dispose objects before losing scope
                           messageHandler: new HttpClientHandler()
                           {
                               ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
                           }
                    #pragma warning restore CA2000 // Dispose objects before losing scope
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
