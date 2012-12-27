using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Web.Services.Protocols;
using System.Web.Services;


namespace MSA.Subscriber.Tester
{
    [System.Web.Services.WebServiceBindingAttribute(Name = "ServiceSoap", Namespace = "http://tempuri.org/")]
    //[System.Xml.Serialization.XmlIncludeAttribute(typeof(object[]))]
    public partial class SoapClient : SoapHttpClientProtocol
    {
        public SoapClient()
        {
            this.Url = "http://localhost:3887/WebService/AppService.asmx";
        }

        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/ZMQRouter", RequestNamespace = "http://tempuri.org/", ResponseNamespace = "http://tempuri.org/", Use = System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle = System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public string JustTesting(string message)
        {
            var ret = this.Invoke("ZMQRouter", new object[] { message });


            return String.Empty;
        }
    }
}
