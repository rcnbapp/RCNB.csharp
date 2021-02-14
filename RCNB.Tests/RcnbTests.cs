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
        public void Test(string s, string rcnb)
        {
            var array = Encoding.UTF8.GetBytes(s);
            string encodeResult = RcnbConvert.ToRcnbString(array);
            Assert.Equal(rcnb, encodeResult);

            var decodeResult = RcnbConvert.FromRcnbString(rcnb);
            Assert.Equal(s, Encoding.UTF8.GetString(decodeResult));
        }
    }
}
