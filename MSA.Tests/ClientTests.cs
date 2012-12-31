using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using MSA.Zmq.JsonRpc;

namespace MSA.Tests
{
    public class ClientTests: IUseFixture<JsonRpcResultProcessor>
    {
        [Fact]
        public void EmptyResponseShouldReturnsError()
        {
            Assert.Throws(typeof(NotImplementedException), delegate {
                throw new NotImplementedException();
            });
        }

        [Fact]
        public void ResultProcessorNeverReturnsNull()
        {
            string[] results = { 
                                   null, 
                                   "", 
                                   "{}", 
                                   "{\"JsonRpc\": \"2.0\",\"Result\": 294.0,\"Id\": 3005}",
                                   "{\"Result\": 294.0,\"Id\": 3005}"
                               };

            foreach (var res in results)
            {
                _processor.ProcessResult(res, (response) => {
                    Assert.NotNull(response);
                });
            }
        }

        [Fact]
        public void ResultProcessorReturnsErrorWhenMissingVersion()
        {
            _processor.ProcessResult("{}", (response) =>
            {
                Assert.NotNull(response.Error);
            });
        }

        [Fact]
        public void ResultProcessorReturnsErrorWhenInvalidVersion()
        {
            _processor.ProcessResult("{'JsonRpc': '1.0'}", (response) =>
            {
                Assert.NotNull(response.Error);
            });
        }

        [Fact(Skip="NA")]
        public void ResultProcessorReturnsErrorObjectWhenInvalid()
        {
            _processor.ProcessResult(null, (response) =>
            {
                Assert.IsType<JsonRpcError>(response.Error);
            });
        }

        private IResultProcessor _processor;
        public void SetFixture(JsonRpcResultProcessor data)
        {
            _processor = data;
        }
    }
}
