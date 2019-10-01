using Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace QueueProcessingService.Util
{

    public static class QueueProcessorUtilities
    {
        /// <summary>
        /// Based on configured timezone get current datetime as string
        /// </summary>
        /// <returns>
        /// String datetime
        /// </returns>
        public static String GetCurrentDateForTimeZone(String timeZoneId)
        {
            return System.TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)).ToString();
        }
    }
}
