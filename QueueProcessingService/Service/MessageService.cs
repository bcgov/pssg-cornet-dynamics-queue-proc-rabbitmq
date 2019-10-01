﻿
using cornet_dynamics_adapter.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects;
using QueueProcessingService.Util;
using RabbitMQ.Client.Events;
using System;
using System.Net;
using System.Net.Http;

namespace QueueProcessingService.Service
{
    public class MessageService : IDisposable
    {
        public bool processMessage(object sender, BasicDeliverEventArgs ea)
        {
            byte[] body = ea.Body;

            RabbitMQMessageObj RMQMessage = JsonConvert.DeserializeObject<RabbitMQMessageObj>(System.Text.Encoding.UTF8.GetString(body, 0, body.Length));
            Console.WriteLine(Environment.NewLine); // Flush the Log a bit
            Console.WriteLine("{0}: Received Event: {1}", CorDynUtilities.GetCurrentDateForTimeZone(ConfigurationManager.FetchConfig("DefaultTimeZone")), RMQMessage.eventId);
            Console.WriteLine("{0}: Message: {1}", CorDynUtilities.GetCurrentDateForTimeZone(ConfigurationManager.FetchConfig("DefaultTimeZone")), JsonConvert.SerializeObject(RMQMessage));
            HttpResponseMessage data;
            HttpResponseMessage responseData = new HttpResponseMessage();
            String dynamicsRespStr;
            DynamicsResponse MsgResponse;

            string MsgVerb = RMQMessage.verb;
            string MsgUrl = RMQMessage.requestUrl;
            string MsgResponseUrl = RMQMessage.responseUrl;

            JRaw payload = RMQMessage.payload;

            Console.WriteLine("    {0} FOR: {1}", MsgVerb, MsgUrl);

            switch (MsgVerb)
            {
                case "POST":
                    data = DataClient.PostAsync(MsgUrl, payload, false).Result;
                    break;
                case "GET":
                    data = DataClient.GetAsync(MsgUrl, false).Result;
                    break;
                case "POSTAUTH":
                    data = DataClient.PostAsync(MsgUrl, payload, true).Result;
                    break;
                case "GETAUTH":
                    data = DataClient.GetAsync(MsgUrl, true).Result;
                    break;
                case "PUT":
                    data = DataClient.PutAsync(MsgUrl, payload).Result;
                    break;
                case "DELETE":
                    data = DataClient.DeleteAsync(MsgUrl, payload).Result;
                    break;
                default:
                    throw new Exception("Invalid VERB, message not processed");
            }

            Console.WriteLine("     Recieved Status Code: {0}", data.StatusCode);
            bool failure = false;
            String failureLocation = "";
            HttpStatusCode failureStatusCode = data.StatusCode;
            //Handle adapter response
            if (data.IsSuccessStatusCode)
            {
                if (MsgVerb == "GETAUTH" || MsgVerb == "GET")
                {
                    String msgResStr = data.Content.ReadAsStringAsync().Result;
                    // Only return the results if response URL was set, some requests require no response
                    if (!string.IsNullOrEmpty(MsgResponseUrl))
                    {
                        Console.WriteLine("    Response Data: {0}", msgResStr);
                        Console.WriteLine("    Sending Response data to: {0}", MsgResponseUrl);

                        responseData = DataClient.PostAsync(MsgResponseUrl, JsonConvert.DeserializeObject<JRaw>(msgResStr), false).Result;
                        if (responseData.IsSuccessStatusCode)
                        {
                            dynamicsRespStr = responseData.Content.ReadAsStringAsync().Result;
                            Console.WriteLine("    Adapter Response: {0}", dynamicsRespStr);
                            Console.WriteLine("    Status Code: {0}", responseData.StatusCode);
                        }
                        else
                        {
                            failure = true;
                            failureLocation = "Adapter";
                            failureStatusCode = responseData.StatusCode;
                        }
                    }
                    else
                    {
                        Console.WriteLine("    Response Data: {0}", msgResStr);
                        Console.WriteLine("    No Response URL Set, work is complete ");
                    }
                }
                else if (MsgVerb == "POSTAUTH" || MsgVerb == "POST")
                {
                    Console.WriteLine("    Data has been posted succesfully to Cornet.");
                    Console.WriteLine(JsonConvert.SerializeObject(payload));
                }
            }
            else
            {
                failure = true;
                failureLocation = "Cornet";
                failureStatusCode = data.StatusCode;
            }

            //Hanlde a failure at any point.
            if (failure)
            {
                Console.WriteLine("    EventId: {0} has failed at {1}. HttpStatusCode: {2}", RMQMessage.eventId, failureLocation, failureStatusCode);
            }
            data.Dispose();
            responseData.Dispose();
            return !failure;
        }
        public void Dispose()
        {
            
        }

    }
}
