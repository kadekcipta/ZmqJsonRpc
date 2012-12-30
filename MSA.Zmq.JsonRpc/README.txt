
DESCRIPTION

JSON-RPC ZMQ Service utility (experimental)

Mandatory arguments:

--mode=<WORKER|MULTI-WORKER|PUBLISHER|ROUTER>
           :<host>
           :<port|comma-separated-port-list>
           [:<router-url>]

  Sets the operating mode of the service

  WORKER    Starts single worker and bind to <port>
	
  Usages:

  > {0} --mode=WORKER:localhost:3001

  This command will start single worker and bind to port 3001

  MULTI-WORKER  Starts multiple worker in the same process
	
  Usages:

  > {0} --mode=MULTI-WORKER:localhost:3001,3002:tcp://remotehost:5000

  This command will start workers 
  and each one bind to port 3001 and 3002 and
  connect to router's backend (dealer port) port 5000 running on remotehost.

  PUBLISHER  Starts publisher and pull service (not available yet).

  Usages:

  > {0} --mode=PUBLISHER:localhost:3001,3002
	
  This command will start publisher service 
  on port 3001 and pull service on 3002. 
  Client that need to push message have to 
  connect to pull service on 3002.

  ROUTER  Starts router and dealer service (not available yet).

  Usages:

  > {0} --mode=ROUTER:localhost:3001,3002
	
  This command will start router that bind 
  to front-end port 3001 and backend port 3002.

--install-server=<service-name>
  Installs a service for certain operating mode

  Usages:

  > {0} --mode=WORKER:localhost:3001 --install-service=worker1
  > {0} --mode=MULTI-WORKER:localhost:3001,3002:tcp://myservice:5000 --install-service=workergroup1

--help
  Display this information

AUTHOR
  cipta (kadekcipta@gmail.com) - Feb 2012