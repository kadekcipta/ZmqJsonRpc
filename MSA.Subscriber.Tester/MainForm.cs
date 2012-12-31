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
using Newtonsoft.Json;

namespace MSA.Subscriber.Tester
{
    public partial class MainForm : Form
    {
        private JsonRpcClient _client;
        private BindingSource _bsICD;
        private SearchCommander _commander;

        public MainForm()
        {
            InitializeComponent();

            _bsICD = new BindingSource();
            dgvIcd.AutoGenerateColumns = false;
            dgvIcd.DataSource = _bsICD;

            //_client = Client.Connect("192.168.1.5", 3001);
            _client = MSA.Subscriber.Tester.Program._client;
            //_client.ServiceError += new ErrorEventHandler(_client_ServiceError);

            //_client
            //.EnqueueMethodCall((c) => 
            //{
            //    _client.CallMethod<string>("SayHello", (s) =>
            //    {
            //        MessageBox.Show(s);
            //        c.Continue();
            //    });
            //})
            //.EnqueueMethodCall((c) => 
            //{
            //    _client.CallMethod<string>("SayHello", (s) =>
            //    {
            //        MessageBox.Show(s + " Again");
            //        c.Continue();
            //    });
            //})
            //.ProcessQueue(() => MessageBox.Show("Completed") );

            _client.CallMethodAsync<string>("patients:SayHello", (s) =>
            {
                MessageBox.Show(s);
            });

            LoadSectionICD();

            _commander = new SearchCommander(txtCommand, (s) => 
            {
                SendRequest(s);
            });

            label1.Text = "";
        }

        void _client_ServiceError(object sender, JsonRpcException ex)
        {
            MessageBox.Show(ex.Message);
        }

        protected override void OnClosed(EventArgs e)
        {
            //_xclient.Dispose();
            _client.Dispose();
            _bsICD.Dispose();
            _commander.Dispose();

            base.OnClosed(e);
        }

        private void SendRequest(string keywords)
        {
            if (!String.IsNullOrEmpty(keywords))
                _client.CallMethodAsync<IList<Icd9>>("FindIcd9Codes", (resp) =>
                {
                    _bsICD.DataSource = resp;
                }, txtCommand.Text);
        }

        private void btnCommand_Click(object sender, EventArgs e)
        {
            SendRequest(txtCommand.Text);
        }

        private void cbSection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(cbSection.DataSource!=null)
            LoadSubSectionICD(cbSection.SelectedValue.ToString());
        }

        private void cbSubsection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(cbSubsection.DataSource!=null && cbSection.DataSource!=null)
                LoadDescriptionICD(cbSection.SelectedValue.ToString(), cbSubsection.SelectedValue.ToString());
        }

        private void cbDescription_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblICDCode.Text = cbDescription.SelectedValue.ToString();
        }


        /// <summary>
        /// Loads the section ICD.
        /// </summary>
        private void LoadSectionICD()
        {
            _client.CallMethodAsync<IList<string>>("GetSection", (respSection) =>
            {
                cbSection.DataSource = respSection;
                if (cbSection.SelectedValue != null)
                {
                    LoadSubSectionICD(cbSection.SelectedValue.ToString());
                }
            });
        }

        /// <summary>
        /// Loads the sub section ICD.
        /// </summary>
        /// <param name="section">The section.</param>
        private void LoadSubSectionICD(string section)
        {
            _client.CallMethodAsync<IList<string>>("GetSubSection", (respSubsection) =>
            {
                cbSubsection.DataSource = respSubsection;
                LoadDescriptionICD(section, cbSubsection.SelectedValue.ToString());
            }, section);
        }

        /// <summary>
        /// Loads the description ICD.
        /// </summary>
        /// <param name="section">The section.</param>
        /// <param name="subsection">The subsection.</param>
        private void LoadDescriptionICD(string section, string subsection)
        {
            _client.CallMethodAsync<IList<Icd9>>("FindICDBySectionAndSubsection", (responDescrption) =>
            {
                cbDescription.DataSource = responDescrption;
                cbDescription.DisplayMember = "Description";
                cbDescription.ValueMember = "Code";
            }, section, subsection);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var f = new ChildForm())
            {
                f.Client = _client;
                f.ShowDialog();
            }
        }
    }
}
