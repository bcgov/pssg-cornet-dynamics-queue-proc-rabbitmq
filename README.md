#  RabbitMQ Queue Processor
	A listener for the RabbitMQ Queue. 
## About 
	This app will take and process messages from the RabbitMQ queue.

## Technologies

	.NET Core 2.1 
	RabbitMQ

## Development Project Setup 

	Required: 
		Visual Studio 2019 
		Docker

## Usage
	In Visual Studio run in IIS Express

	Run the rabbitmq instance in docker. The managment port must also be mapped. 	
```
docker run -d --hostname my-rabbit --name some-rabbit -p 5672:5672 -p 15672:15672 rabbitmq:3-management	
```	