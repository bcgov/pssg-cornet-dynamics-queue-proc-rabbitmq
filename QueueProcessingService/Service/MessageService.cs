
using QueueProcessingService.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects;
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
            QueueProcessorLog.LogInfomration(String.Format("Received Event: {0}", RMQMessage.eventId));
            QueueProcessorLog.LogInfomration(String.Format("Message: {0}", JsonConvert.SerializeObject(RMQMessage)));
            HttpResponseMessage data;
            HttpResponseMessage responseData = new HttpResponseMessage();
            String requestRespStr;

            string MsgVerb = RMQMessage.verb;
            string MsgUrl = RMQMessage.requestUrl;
            string MsgResponseUrl = RMQMessage.responseUrl;

            JRaw payload = RMQMessage.payload;

            QueueProcessorLog.LogInfomration(String.Format("{0} FOR: {1}", MsgVerb, MsgUrl));

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

            QueueProcessorLog.LogInfomration(String.Format("Recieved Status Code: {0}", data.StatusCode));
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
                        QueueProcessorLog.LogInfomration(String.Format("Initial Response Data: {0}", msgResStr));
                        QueueProcessorLog.LogInfomration(String.Format("Sending Response data to: {0}", MsgResponseUrl));

                        responseData = DataClient.PostAsync(MsgResponseUrl, JsonConvert.DeserializeObject<JRaw>(msgResStr), false).Result;
                        if (responseData.IsSuccessStatusCode)
                        {
                            requestRespStr = responseData.Content.ReadAsStringAsync().Result;
                            QueueProcessorLog.LogInfomration(String.Format("Message Response: {0}", requestRespStr));
                            QueueProcessorLog.LogInfomration(String.Format("Status Code: {0}", responseData.StatusCode));
                        }
                        else
                        {
                            failure = true;
                            failureLocation = "Message Response Request";
                            failureStatusCode = responseData.StatusCode;
                        }
                    }
                    else
                    {
                        QueueProcessorLog.LogInfomration(String.Format("Response Data: {0}", msgResStr));
                        QueueProcessorLog.LogInfomration("No Response URL Set, work is complete ");
                    }
                }
                else if (MsgVerb == "POSTAUTH" || MsgVerb == "POST")
                {
                    QueueProcessorLog.LogInfomration("Data has been posted succesfully to Cornet.");
                    QueueProcessorLog.LogInfomration(JsonConvert.SerializeObject(payload));
                }
            }
            else
            {
                failure = true;
                failureLocation = "Initial Request";
                failureStatusCode = data.StatusCode;
            }

            //Hanlde a failure at any point.
            if (failure)
            {
                QueueProcessorLog.LogInfomration(String.Format("EventId: {0} has failed at {1}. HttpStatusCode: {2}", RMQMessage.eventId, failureLocation, failureStatusCode));
            }
            data.Dispose();
            responseData.Dispose();
            return failure;
        }
        public void Dispose()
        {
            
        }

    }
}
