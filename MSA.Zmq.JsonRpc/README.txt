
Magicsoft-Asia Systems JSON-RPC ZMQ Service 2012 (Alpha)
===============================================

Parameters:

	--router=<backend-port>:<frontend-port>
	
	This will start service as a router that listen on <backend-port> 
	and <frontend-port>
	
	<backend-port>: the port that every worker will connect to
	
	<frontend-port>: the port that every clients will connect to, 
	it will act as a routing device hence the name router
	

	--worker-group=<router-url>#<worker-ports-list>
	
	This will configure service that will start number of workers 
	based on comma separated value in <worker-ports-list>
	<router-url>: valid zeromq node format: e.q tcp://hostname:port
	
	--worker=<port>
	
	This will start the service as a single worker with specified port
	
	<worker-ports-list>: comma separated value : e.g 3003,3004
	
	--install
	This parameter if combined with --router and --worker, 
	will install the the Windows service with service name:
	
	ZMSA.Router (for router) or ZMSA.Worker (for worker)
	
	--help
	Display this information
	
Usages:
	
	> {0} --router=3000:3001							
	> {0} --router=3000:3001 --install					
	> {0} --worker=3001
	> {0} --worker-group=tcp://localhost:3000#3004,3005
	> {0} --worker-group=tcp://localhost:3000#3004,3005 --install



	
Author: kadekcipta@magicsoft-asia.com	- Feb 2012