using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc.Models
{
    public sealed class TaskDescriptor
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public bool Authorize { get; set; }
        public bool LogCall { get; set; }
        public string Description { get; set; }
        public string RequiredRoles { get; set; }
        public DateTime LastInvoked { get; set; }
        public string LastInvokedBy { get; set; }
    }
}
