using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using MSA.Zmq.JsonRpc;
using MSA.LocalCache.Models;

namespace MSA.Subscriber.Tester
{
    public partial class ChildForm : Form
    {
        public ChildForm()
        {
            InitializeComponent();
        }

        public JsonRpcClient Client { get; set; }

        private void button1_Click(object sender, EventArgs e)
        {
            var s = Client.CallMethod<DateTime>("EchoDate", DateTime.Now);
            MessageBox.Show(s.ToString());

            var d = Client.CallMethod<double>("EchoDouble", 12.6);
            MessageBox.Show(d.ToString());

            var o = Client.CallMethod<PatientSearchCriteria>("EchoObject", new PatientSearchCriteria() { BirthDate = DateTime.Now, NameLike = "Kadek" });
            MessageBox.Show(o.ToString());

            var n = new NestedSearchCriteria();
            n.Criteria = new PatientSearchCriteria() { BirthDate = null, NameLike = "" };
            var no = Client.CallMethod<NestedSearchCriteria>("EchoNestedObject", n);
            MessageBox.Show(no.ToString());


            //Client.CallMethodAsync<string>("SayHello", (s) =>
            //{
            //    MessageBox.Show(s);
            //});
        }
    }
}
