using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSA.Zmq.JsonRpc;
using MSA.LocalCache.Models;

namespace MSA.Repository.ServiceHandler
{
    [JsonRpcServiceHandler]
    public sealed class CommonLookup: MarshalByRefObject
    {
        private Icd9Manager _icdManager;
        public CommonLookup()
        {
            _icdManager = new Icd9Manager();
        }

        [JsonRpcMethod]
        public IList<Icd9> FindIcd9Codes(string keywords)
        {
            return _icdManager.FindByTitle(keywords);
        }

        [JsonRpcMethod]
        public IList<string> GetSection()
        {
            return _icdManager.GetSection();
        }

        [JsonRpcMethod]
        public IList<string> GetSubSection(string keywords)
        {
            return _icdManager.GetSubSection(keywords);
        }

        [JsonRpcMethod]
        public IList<Icd9> FindICDBySectionAndSubsection(string section, string subsection)
        {
            return _icdManager.FindBySectionAndSubSection(section, subsection);
        }

        [JsonRpcMethod(LogCall=true)]
        public string EchoString(string value)
        {
            return value + " -echoed";
        }

        [JsonRpcMethod(LogCall=true)]
        public double EchoDouble(double value)
        {
            return value;
        }

        [JsonRpcMethod(LogCall = true)]
        public DateTime EchoDate(DateTime value)
        {
            return value;
        }

        [JsonRpcMethod(LogCall = true)]
        public PatientSearchCriteria EchoObject(PatientSearchCriteria criteria)
        {
            return criteria;
        }

        [JsonRpcMethod(LogCall = false, Description="Nested Object Call")]
        public NestedSearchCriteria EchoNestedObject(NestedSearchCriteria criteria)
        {
            return criteria;
        }
    }
}
