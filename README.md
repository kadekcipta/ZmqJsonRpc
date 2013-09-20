ZMQ JsonRpc
===========

This is my experimentation and proof of concept using ZMQ as task distribution for JSONRPC 2.0 based service.

#Features
- REQ/REP for client and worker communication
- Attributes based service class and method registration
- Asynchronous and synchronous client
- Method namespacing to group methods or to identify a class
- PUB/SUB for notification 
- ROUTER/DEALER for worker distribution to provide basic scalability
- Windows service that can install multiple service modes and names using single executable (experimental)

#Goal
- Provide simple .NET based system for internal task distribution
- Easy to scale by adding more workers behind router
- Easy to monitor 

#TODO
- Code comments
- More tests
- Improve a lot of things if not all

#Usages

##Class and method registration

<pre>
<code>

    [JsonRpcServiceHandler]
    class SimpleHandler
    {
        [JsonRpcMethod(Authorize = true, LogCall = false, Description = "Just echoing")]
        public string Echo(string message)
        {
            return message;
        }

        [JsonRpcMethod]
        public double AddNumber(double v1, double v2)
        {
            return v1 + v2;
        }
    }

	....

    var worker = Worker.Create("127.0.0.1", 3001);
    worker.AddTaskHandler(new TaskHandlerDescriptor(typeof(SimpleHandler), "namespace:"));
    worker.Start();

</code>
</pre> 

##Client's method call

<pre>
<code>

	// asynchronous call
	var client = MSA.Zmq.JsonRpc.Client.CreateJsonRpcContext("tcp://127.0.0.1:3001");
    client.CallMethodAsync<double>("namespace:AddNumber", (ret) => 
	{
        Console.WriteLine(ret);
    }, 200, 50);

	....

	// synchronous call
	var ret = client.CallMethod<double>("namespace:AddNumber", 200, 50);

</code>
</pre> 

##PUB/SUB

<pre>
<code>

	// Publisher setup
	// It will bind PUB on 3002 and PULL socket on 3003
	var publisher = Publisher.Create("127.0.0.1", 3002, 3003, context);

	....

	// Subscriber
	var subscriber = MSA.Zmq.JsonRpc.Client.CreateSubscriberContext("tcp://127.0.0.1:3002");

    // subscribe for event triggered from another agent
    subscriber.Subscribe("oob:notification", (s) =>
    {
		// react with the event
    });
	
	....

	// Publisher / PUSH
	var client = Client.CreatePushContext("tcp://127.0.0.1:3003");
	pusher.Push("oob:notification", "some data here");

</code>
</pre> 

##Service usages
- Put the required assemblies in the same folder with service executable
- Register the each assembly containing the handler class

<pre>

```
  <zmsa-handlers>
    <handlers>
      <!--<add handlerName="sampleHandler" assemblyName="SameHandlerAssembly" endpointPrefix=""/>-->
    </handlers>
  </zmsa-handlers>
```
</pre>

- Execute the service executable with --help for more information
