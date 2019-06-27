using System;
using System.Threading;
using QueueProcessingService.Util;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using QueueProcessingService.Service;

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
            // "guest"/"guest" by default, limited to localhost connections
        
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
           
            channel.ExchangeDeclare(exchange, ExchangeType.Direct, durable);
            channel.QueueDeclare(subject, durable, false, false, null);
            channel.QueueBind(subject, exchange, routingKey, null);
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received +=  (ch, ea) => {
                bool result = false;
                using (MessageService messageService = new MessageService())
                {
                    result = messageService.processMessage(ch, ea);
                }
                //Ack or not based on the result from processing the message.   
                if (result)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                }
                else
                {
                    channel.BasicReject(ea.DeliveryTag, false);
                }
            };
            channel.BasicConsume(subject, false, consumer);
            // just wait until we are done.
            ev.WaitOne();
        }
    }

}


