using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc.BuiltinsTasks
{
    /// <summary>
    /// This task handler will be much of self reflection on the worker itself
    /// </summary>
    [JsonRpcServiceHandler]
    public class TaskLister
    {
        public TaskLister(Worker worker)
        {
            Worker = worker;
        }

        public Worker Worker { get; private set; }

        [JsonRpcMethod]
        public IList<Models.TaskDescriptor> GetAvailableTasks()
        {
            return Worker.GetAvailableTasks();
        }
    }
}
