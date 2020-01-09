using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QueueProcessingService.Util;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace QueueProcessingService
{
    static class DataClient
    {
        private static readonly int timeout = int.Parse(ConfigurationManager.FetchConfig("Request_Timeout").ToString());

        public static async Task<HttpResponseMessage> PostAsync(string uri, JRaw data, bool auth, String username = null, String password = null)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.Timeout = new TimeSpan(0, timeout, 0);
                    if (auth)
                    {
                        httpClient.DefaultRequestHeaders.Authorization = SetAuthentication(username, password);
                    }
                    HttpResponseMessage content = await httpClient.PostAsJsonAsync(uri, data);
                    return await Task.Run(() => content);
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.Message);
                HttpResponseMessage failureResponse = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
                return failureResponse;
            }
        }


        public static async Task<HttpResponseMessage> PutAsync(String endpoint, JRaw data)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.Timeout = new TimeSpan(0, timeout, 0);
                    HttpResponseMessage content = httpClient.PutAsJsonAsync(endpoint, data).Result;
                    return await Task.Run(() => content);
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.Message);
                HttpResponseMessage failureResponse = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
                return failureResponse;
            }

        }

        public static async Task<HttpResponseMessage> DeleteAsync(String endpoint)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.Timeout = new TimeSpan(0, timeout, 0);
                    HttpResponseMessage content = httpClient.DeleteAsync(endpoint).Result;
                    return await Task.Run(() => content);
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.Message);
                HttpResponseMessage failureResponse = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
                return failureResponse;
            }
        }



        public static async Task<HttpResponseMessage> GetAsync(string uri, bool auth, String username = null, String password = null)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.Timeout = new TimeSpan(0, timeout, 0);
                    if (auth)
                    { 
                        httpClient.DefaultRequestHeaders.Authorization = SetAuthentication(username, password);                      
                    }

                    HttpResponseMessage content = await httpClient.GetAsync(uri);
                    return await Task.Run(() => content);
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.Message);
                HttpResponseMessage failureResponse = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
                return failureResponse;
            }
        }
        private static AuthenticationHeaderValue SetAuthentication(String user, String pass)
        {
            byte[] authToken = Encoding.ASCII.GetBytes($"{user}:{pass}");
            return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));
        }
    }
}
