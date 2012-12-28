using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MSA.Tests
{
    public class ClientTests
    {
        [Fact]
        public void EmptyResponseShouldReturnsError()
        {
            Assert.Throws(typeof(NotImplementedException), delegate {
                throw new NotImplementedException();
            });
        }
    }
}
