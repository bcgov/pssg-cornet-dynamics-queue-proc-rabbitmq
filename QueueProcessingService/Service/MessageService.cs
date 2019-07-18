﻿
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
        string clusterName = ConfigurationManager.FetchConfig("CLUSTER_NAME");
        public bool processMessage(object sender, BasicDeliverEventArgs ea)
        {
            byte[] body = ea.Body;

            RabbitMQMessageObj RMQMessage = JsonConvert.DeserializeObject<RabbitMQMessageObj>(System.Text.Encoding.UTF8.GetString(body, 0, body.Length));
            Console.WriteLine(Environment.NewLine); // Flush the Log a bit
            Console.WriteLine("{0}: Received Event: {1}", DateTime.Now, RMQMessage.eventId);
            Console.WriteLine("{0}: Message: {1}", DateTime.Now, JsonConvert.SerializeObject(RMQMessage));
            HttpResponseMessage data = new HttpResponseMessage();
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
                    data = DataClient.PostAsync(MsgUrl, payload).Result;
                    break;
                case "GET":
                    data = DataClient.GetAsync(MsgUrl).Result;
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
                if (MsgVerb == "GET")
                {
                    String msgResStr = data.Content.ReadAsStringAsync().Result;
                    // Only return the results if response URL was set, some requests require no response
                    if (!string.IsNullOrEmpty(MsgResponseUrl))
                    {
                        Console.WriteLine("    Response Data: {0}", msgResStr);
                        Console.WriteLine("    Sending Response data to: {0}", MsgResponseUrl);

                        responseData = DataClient.PostAsync(MsgResponseUrl, JsonConvert.DeserializeObject<JRaw>(msgResStr)).Result;
                        if (responseData.IsSuccessStatusCode)
                        {
                            dynamicsRespStr = responseData.Content.ReadAsStringAsync().Result;
                            Console.WriteLine("    Adpater Response: {0}", dynamicsRespStr);

                            MsgResponse = JsonConvert.DeserializeObject<DynamicsResponse>(dynamicsRespStr);
                            Console.WriteLine("    Dynamics Status Code: {0}", MsgResponse.httpStatusCode);
                            //Handle successful dynamics response
                            if ((int)MsgResponse.httpStatusCode >= 200 && (int)MsgResponse.httpStatusCode <= 299)
                            {
                                Console.WriteLine("    EventId: {0} has succeeded. {1} Dynamics Response: {2}", RMQMessage.eventId, Environment.NewLine, dynamicsRespStr);
                            }
                            else
                            {
                                failure = true;
                                failureLocation = "Dynamics";
                                failureStatusCode = MsgResponse.httpStatusCode;
                            }
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
                else if (MsgVerb == "POST")
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
