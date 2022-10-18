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
        [InlineData("@", "ŖĈ")]
        [InlineData(
            "北国风光，千里冰封，万里雪飘。望长城内外，惟余莽莽；大河上下，顿失滔滔。山舞银蛇，原驰蜡象，欲与天公试比高。须晴日，看红装素裹，分外妖娆。江山如此多娇，引无数英雄竞折腰。惜秦皇汉武，略输文采；唐宗宋祖，稍逊风骚。一代天骄，成吉思汗，只识弯弓射大雕。俱往矣，数风流人物，还看今朝。",
            "ŅƅȒČǸßŔƇņBŕĆŅƁȓcŇƁRƇŇßrčÑƃȓƇnBRċnþRČNƄȓcnBRċŇƀrƈƝƀȒČŅƄrćņbŘȻȠƃȒĊňƄrȻȠƀrÇńƄȓcńƄŖȻǸƀŖcǹƀȒĈȵƀrĈǹƁŕćŃþȓcnßřĈǹÞŕČŅbȒČnþrƇŇƅŖĆÑƃȓƇnƀRċŃßrĊȠþȒĊńƀŔȼŃƁRƇŇƅȒȻÑƄřcŇƄŘȻňbȒČNƃŖČňþŗƈȠƃȒĊƝÞRćňƄŘĊÑƃȓƇnƄRċƝþŕÇňbȒČŃƃŗƇÑƀŘƈŅþȒƇƞßŔĉȠbrcƝƁȒČŃƅŗƇÑƅrȼŃßȓcÑƅŘćȵßŕCÑƃȓƇnBRċȵƁRčŅþȓcňbŗČǹþŕćÑBȒȻȠßŕÇņbŘȻńƃȒƇǹÞŗƈňÞŘĊňbȒČňƄŖƈÑƀrčƞþȒȻņƅŔċńbŗĊņBȓcňBŔÇȠbrcŅBȓcńƃŘÇȠbŔȻNƁȒƇǸBŖĈņbŘȻǹƃȒƈŅßRĉNbŘƇƝßȒȻNBrƇǹƀŗȼƝßȒȻȠBŘČņbŘȻŅƀȒČǸƄrƈŇƅŖĆňƀȒČNBŔčŃƃŖčǹƀȒĈȵƀrĈÑƁŗčƝƁȒČŃƁŗƇÑÞŖĉńƄȒƇȵƄŖĆňbŖĆňƃȒČƝBrȻņbŘȻƞƃȒČņƅŔċŅÞŔƇNBȒƇňbŗČȵƁRĈńƄȓcņƄrČŇƅŖȼnbȒƇňƅŔÇÑƅrčǹƀȒĈȵƀrĈŃƃrĊȠƄȒƈņþŖĊŅBŕcńþȒƇȠƃRCȠƅŗĆÑƃȓƇnƁRċȵBŔĊǸƃȒȻȵƃŔćŇßŔČNƄȓcƝBrȻŇƄŘȻņƃȒČǹƄRȻňßŗĈƝbȒČŅßRĉŃƀŖĈÑƃȓƇnƁRċńBŖčnÞȓcƞƁRćÑƀŖcņbȓcȠƁŕcȠbrcȠƃȒĊňÞrCǸßŘƈňbȒČňþŖƈƝƃŖÇÑƃȓƇnƀRċƞƀrȼņbȒČȠƃRCnßrCńþȒƇǹßŔƇņbŘȻŅþȒČńƃŖȻNƅŗĊƞƃȒČǸBŗĊņßŘȻƝƀȒČņƁrČƝƁŖĆńƄȓcņƁŔċȠbrcȵBȒĊŃƁŗƇÑbřCǹßȒƈŅþŖCņbŘȻNBȒƇňBŗČÑƀŖcŅƁȒƇńƃrćńƃŘƇÑƄȒƈƝƄŖƈņbŘȻǸƄȒȻňƄŔÇÑßŕćȠßȒĊƞbRćǹƄŕćǹƀȒĈnƀ")]
        public void Test(string s, string rcnb)
        {
            var array = Encoding.UTF8.GetBytes(s);
            Assert.Equal(rcnb, RcnbConvert.ToRcnbString(array));
            Assert.Equal(rcnb, RcnbConvert.ToRcnbString(array.AsSpan()));
            Assert.Equal(rcnb, RcnbConvert.ToRcnbString(array.AsMemory()));
            Span<byte> span = stackalloc byte[array.Length];
            array.CopyTo(span);
            Assert.Equal(rcnb, RcnbConvert.ToRcnbString(span));

            var array2 = new byte[100 + array.Length];
            var memory = array2[100..].AsMemory();
            array.CopyTo(memory);
            Assert.Equal(rcnb, RcnbConvert.ToRcnbString(memory.Span));
            Assert.Equal(rcnb, RcnbConvert.ToRcnbString(memory));

            var decodeResult = RcnbConvert.FromRcnbString(rcnb);
            Assert.Equal(s, Encoding.UTF8.GetString(decodeResult));
        }

        [Fact]
        public void Avx2Test()
        {
            if (!RCNB.Implementations.RcnbAvx2.IsSupported)
            {
                return; // skipped
            }
            var random = new Random();
            var data = new byte[10 * 1024 * 1024 + 33];
            random.NextBytes(data);
            var s1 = RcnbConvert.ToRcnbString(data);
            var s2 = string.Create(data.Length * 2, data, (s, d) =>
            {
                Implementations.RcnbAvx2.EncodeRcnb(d, s);
            });
            Assert.Equal(s1, s2);
        }
    }
}
