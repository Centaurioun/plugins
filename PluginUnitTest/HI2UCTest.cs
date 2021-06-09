using System;
using System.Globalization;
using System.Windows.Forms;
using Nikse.SubtitleEdit.PluginLogic;
using Nikse.SubtitleEdit.PluginLogic.Strategies;
using Xunit;

namespace PluginUnitTest
{
    public class Tests
    {
        [Fact]
        public void LowerCaseStrategyTest()
        {
            var c = new LowerCaseStrategy(CultureInfo.CurrentCulture);
            Assert.Equal("foobar", c.Execute("FOOBAR"));
        }
        
        [Fact]
        public void NoneCaseStrategyTest()
        {
            var c = new NoneCaseStrategy();
            Assert.Equal("FOOBAR", c.Execute("FOOBAR"));
            Assert.Equal("foobar", c.Execute("foobar"));
        }
        
        [Fact]
        public void ConvertTest()
        {
            var c = new HI2UC();

            c.DoAction(new Form(), default, default, default, default,
                default, default);
        }
    }
}