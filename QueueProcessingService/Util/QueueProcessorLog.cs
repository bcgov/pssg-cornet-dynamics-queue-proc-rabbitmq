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
                .WriteTo.EventCollector(ConfigurationManager.FetchConfig("Serilog:EventCollectorUrl"), ConfigurationManager.FetchConfig("Serilog:Token"))
                .CreateLogger();
        }
        public void LogInfomration(String msg)
        {
            _logger.Information(msg);
        }
    }
}
