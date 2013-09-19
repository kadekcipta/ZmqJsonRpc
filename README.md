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

#TODO
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

##Service usages
- TBD