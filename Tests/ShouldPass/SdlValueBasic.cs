using SdlangSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Tests.ShouldPass
{
    public class SdlValueBasic
    {
        [Fact]
        public void Integer()
        {
            var v = new SdlValue(69);
            Assert.Equal(69, v.Integer);
            Assert.True(v != 68);
            Assert.True(v < 70);
            Assert.True(v > 68);
        }

        [Fact]
        public void Floating()
        {
            var v = new SdlValue(69.0f);
            Assert.Equal(69.0d, v.Floating);
            Assert.True(v != 68.0d);
            Assert.True(v < 70.0f);
            Assert.True(v > 68.0f);
        }

        [Fact]
        public void Boolean()
        {
            Assert.True(SdlValue.True);
            Assert.False(SdlValue.False);
            Assert.True(!SdlValue.False);
            Assert.False(SdlValue.True && SdlValue.False);
        }
    }
}
