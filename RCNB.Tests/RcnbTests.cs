using System;
using Xunit;

namespace RCNB.Tests
{
    public class RcnbTests
    {
        [Fact]
        public void EncodeTest()
        {
            var array = new byte[] { 114, 99, 110, 98 };
            string result = RcnbConvert.ToRcnbString(array);

            Assert.Equal("ɌcńƁȓČņÞ", result);
        }
    }
}
