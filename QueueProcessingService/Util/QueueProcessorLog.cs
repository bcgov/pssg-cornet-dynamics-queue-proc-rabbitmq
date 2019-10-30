using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QueueProcessingService.Util
{
    public static class QueueProcessorLog
    {
        private readonly static String defaultTimeZone = ConfigurationManager.FetchConfig("DefaultTimeZone");
        public static void LogInfomration(String msg)
        {
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("{0}: {1}", QueueProcessorUtilities.GetCurrentDateForTimeZone(defaultTimeZone), msg);
        }
    }
}
