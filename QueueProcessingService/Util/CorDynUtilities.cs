using Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace cornet_dynamics_adapter.Util
{

    public static class CorDynUtilities
    {
        /// <summary>
        /// Create content used to make PATCH request
        /// </summary>
        /// <param name="inObject">Object to converted into json string</param>
        /// <returns>
        /// Return a String Content with the object as json.
        /// </returns>
        public static StringContent ObjectToContent(Object inObject)
        {
            String jsonRequest = JsonConvert.SerializeObject(inObject);
            return new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        }
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
