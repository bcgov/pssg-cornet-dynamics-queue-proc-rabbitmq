using Newtonsoft.Json;
using Objects;
using QueueProcessingService.Util;
using RabbitMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QueueProcessingService.Client
{
    public class QueueClient 
    {
        private int retriesMax = 5;
        private String clusterName;
        private String clientId;
        public QueueClient(String clusterName, String clientId)
        {
            this.clusterName = clusterName;
            this.clientId = clientId;
        }
        public HttpResponseMessage QueueDynamicsNotfication(RabbitMQMessageObj natsMessage)
        {
            //Connect and publish to the queue
            int i = 0;
            while (i < retriesMax)
            {
                try
                {
                  //  using (IStanConnection c = stanConnectionFactory.CreateConnection(clusterName, clientId, stanOptions))
                    {
                  //      c.Publish(ConfigurationManager.FetchConfig("QUEUE_SUBJECT"), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(natsMessage)));
                    }
                    return new HttpResponseMessage();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error in queue connection. Retry {0} of 5, Message: {1} ", (i + 1).ToString(), e.Message);
                }
                i++;
            }
            throw new Exception("Connection to NATS-Streaming has failed.");
        }
    }
}
