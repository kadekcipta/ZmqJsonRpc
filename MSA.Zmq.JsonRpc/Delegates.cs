using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc
{
    public delegate void StartedEventHandler(object sender, EventArgs e);
    public delegate void StoppedEventHandler(object sender, EventArgs e);
}
