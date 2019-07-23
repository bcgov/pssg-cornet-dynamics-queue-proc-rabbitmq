using System;
using System.Threading;
using QueueProcessingService.Util;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using QueueProcessingService.Service;
using System.Collections.Generic;

namespace QueueProcessingService
{
    public class QueueProcess
    {
        int count = 0;
        // Environment Variable Configuration
        string host = ConfigurationManager.FetchConfig("QUEUE_HOST");
        string port = ConfigurationManager.FetchConfig("QUEUE_PORT");
        string vhost = ConfigurationManager.FetchConfig("QUEUE_VHOST");
        string subject = ConfigurationManager.FetchConfig("QUEUE_SUBJECT");
        string routingKey = ConfigurationManager.FetchConfig("ROUTING_KEY");
        string exchange = ConfigurationManager.FetchConfig("EXCHANGE");
        bool durable = (ConfigurationManager.FetchConfig("DURABLE") == "TRUE");
        string username = ConfigurationManager.FetchConfig("QUEUE_USERNAME");
        string password = ConfigurationManager.FetchConfig("QUEUE_PASSWORD");
        string retryQueue = ConfigurationManager.FetchConfig("RETRY_QUEUE");
        string retryExchange = ConfigurationManager.FetchConfig("RETRY_EXCHANGE");
        int retryCount = int.Parse(ConfigurationManager.FetchConfig("RETRY_NUMBER"));
        //Parking Lot settings
        string parkingLotQueue = ConfigurationManager.FetchConfig("PARKINGLOT_QUEUE");
        string parkingLotExchange = ConfigurationManager.FetchConfig("PARKINGLOT_EXCHANGE");
        string parkingLotRoute = ConfigurationManager.FetchConfig("PARKINGLOT_ROUTE");

        public static int reconnect_attempts = 0;

        public static void Main(string[] args)
        {
            reconnect_attempts = Int32.TryParse(ConfigurationManager.FetchConfig("SERVER_RECONNECT_ATTEMPTS"), out int i) ? i : 0;
        AttemptLabel:
            try
            {
                new QueueProcess().Run(args);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine("Exception: " + ex.Message);
                System.Console.Error.WriteLine(ex);

                if (reconnect_attempts > 0)
                {
                    reconnect_attempts--;
                    goto AttemptLabel;
                }
            }
        }

        public void Run(string[] args)
        {
            ConnectionFactory factory = new ConnectionFactory();
        
            factory.Uri = new System.Uri($"amqp://{username}:{password}@{host}:{port}/{vhost}");
         
            using (IConnection c = factory.CreateConnection()) 
            {              
                receiveAsyncSubscriber(c);
            }
        }

        private void receiveAsyncSubscriber(IConnection c)
        {
            AutoResetEvent ev = new AutoResetEvent(false);
            IModel channel = c.CreateModel();     
            //Create main queue
            channel.ExchangeDeclare(exchange, ExchangeType.Direct, durable);
            channel.QueueDeclare(subject, durable, false, false, new Dictionary<string, object>
                    {
                        {"x-dead-letter-exchange", retryExchange},
                        {"x-dead-letter-routing-key", retryQueue}
                    });
            channel.QueueBind(subject, exchange, routingKey, null);
            //Create the retry queue
            channel.ExchangeDeclare(retryExchange, ExchangeType.Direct);
            channel.QueueDeclare
            (
                retryQueue, true, false, false,
                new Dictionary<string, object>
                {
                        {"x-dead-letter-exchange", exchange},
                        {"x-dead-letter-routing-key", subject},
                        {"x-message-ttl", 30000},
                }
            );
            channel.QueueBind(retryQueue, retryExchange, retryQueue, null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received +=  (ch, ea) => {
                bool result = false;
                using (MessageService messageService = new MessageService())
                {
                    result = messageService.processMessage(ch, ea);
                }
                //Ack or not based on the result from processing the message.   
                byte[] body = ea.Body;
                if (result)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                }
                else if (int.Parse(ea.BasicProperties.Headers["x-death"].ToString()) <= retryCount)
                {
                    //Inc error count
                    channel.BasicAck(ea.DeliveryTag, false);

                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = false;
                    Dictionary<string, object> dictionary = new Dictionary<string, object>();
                    dictionary.Add("x-death", (int.Parse(ea.BasicProperties.Headers["x-death"].ToString()) + 1));
                    dictionary.Add("x-request-id", System.Text.Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["x-request-id"]));
                    dictionary.Add("date", System.Text.Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["date"]));
                    properties.Headers = dictionary;
                    channel.BasicPublish(retryExchange, retryQueue, properties, ea.Body);                 
                    //
                }
                else
                {
                    //Failed five times reject the message
                    channel.BasicReject(ea.DeliveryTag, false);
                    //Add to parking lot queue
                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = false;
                    Dictionary<string, object> dictionary = new Dictionary<string, object>();
                    dictionary.Add("x-death", 0);
                    dictionary.Add("x-request-id", System.Text.Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["x-request-id"]));
                    dictionary.Add("date", System.Text.Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers["date"]));
                    properties.Headers = dictionary;
                    channel.BasicPublish(parkingLotExchange, parkingLotRoute, properties, ea.Body);  
                    //Log final error.
                    Console.WriteLine("Message has failed and has been added to the parking lot.");

                }
            };
            channel.BasicConsume(subject, false, consumer);
            channel.BasicConsume(retryQueue, false, consumer);
            // just wait until we are done.
            ev.WaitOne();
        }
    }

}


