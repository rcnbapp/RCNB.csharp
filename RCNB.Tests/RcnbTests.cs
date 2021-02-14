using System;
using System.Text;
using Xunit;

namespace RCNB.Tests
{
    public class RcnbTests
    {
        [Theory]
        [InlineData("rcnb", "ɌcńƁȓČņÞ")]
        [InlineData("我爱你", "ȵÞȒčƝƁŔcǹƄrȼȠþȒĊńƀ")]
        public void EncodeTest(string s, string rcnb)
        {
            var array = Encoding.UTF8.GetBytes(s);
            string result = RcnbConvert.ToRcnbString(array);

            Assert.Equal(rcnb, result);
        }
    }
}
