using System;
using Xunit;
using SdlangSharp;

namespace Tests.ShouldPass
{
    // Tests anything related to singular, specific parse tokens.
    public class SdlReaderBasic
    {
        [Fact]
        public void EndOfFile()
        {
            var parser = new SdlReader("");
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Fact]
        public void IdentifierNoNamespace()
        {
            var parser = new SdlReader("ello");
            parser.Read();
            Assert.Equal(SdlTokenType.Identifier, parser.TokenType);
            Assert.Equal("ello", parser.ValueSpan.ToArray());
            Assert.Equal("ello", parser.TagNameSpan.ToArray());
            Assert.Equal("", parser.TagNamespaceSpan.ToArray());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Fact]
        public void IdentifierNamespace()
        {
            var parser = new SdlReader("el:lo");
            parser.Read();
            Assert.Equal(SdlTokenType.Identifier, parser.TokenType);
            Assert.Equal("el:lo", parser.ValueSpan.ToArray());
            Assert.Equal("el", parser.TagNamespaceSpan.ToArray());
            Assert.Equal("lo", parser.TagNameSpan.ToArray());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Fact]
        public void IdentifierOnlyNamespace()
        {
            var parser = new SdlReader("el:");
            parser.Read();
            Assert.Equal(SdlTokenType.Identifier, parser.TokenType);
            Assert.Equal("el:", parser.ValueSpan.ToString());
            Assert.Equal("el", parser.TagNamespaceSpan.ToString());
            Assert.Equal("", parser.TagNameSpan.ToString());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Theory]
        [InlineData("# This is a comment")]
        [InlineData("-- This is a comment")]
        [InlineData("// This is a comment")]
        public void CommentSingleLineIsolated(string code)
        {
            var parser = new SdlReader(code);
            parser.Read();
            Assert.Equal(SdlTokenType.Comment, parser.TokenType);
            Assert.Equal(" This is a comment", parser.ValueSpan.ToString());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Fact]
        public void CommentMultiLineIsolatedOneLine()
        {
            var parser = new SdlReader("/* This is a comment */");
            parser.Read();
            Assert.Equal(SdlTokenType.Comment, parser.TokenType);
            Assert.Equal(" This is a comment ", parser.ValueSpan.ToString());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Fact]
        public void CommentMultiLine()
        {
            var parser = new SdlReader("/* \nThis\nis\na\ncomment\n */");
            parser.Read();
            Assert.Equal(SdlTokenType.Comment, parser.TokenType);
            Assert.Equal(" \nThis\nis\na\ncomment\n ", parser.ValueSpan.ToString());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Theory]
        [InlineData("\"Sdlang Rocks!\"", SdlTokenType.StringDoubleQuoted)]
        [InlineData("`Sdlang Rocks!`", SdlTokenType.StringBackQuoted)]
        public void StringSimple(string code, SdlTokenType stringType)
        {
            var parser = new SdlReader(code);
            parser.Read();
            Assert.Equal(stringType, parser.TokenType);
            Assert.Equal("Sdlang Rocks!", parser.ValueSpan.ToString());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Fact]
        public void StringMultiLineDoubleQuoted()
        {
            var parser = new SdlReader("\"Hey \\\n   There lol\"");
            parser.Read();
            Assert.Equal(SdlTokenType.StringDoubleQuoted, parser.TokenType);
            Assert.Equal("Hey \\\n   There lol", parser.ValueSpan.ToString()); // ValueSpan doesn't perform any input conversion.
            Assert.Equal("Hey There lol", parser.GetString()); // But GetString does.
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Fact]
        public void StringMultiLineBackQuoted() // Back quotes are WYSIWYG strings.
        {
            var parser = new SdlReader("`Hey \\\n   There lol`");
            parser.Read();
            Assert.Equal(SdlTokenType.StringBackQuoted, parser.TokenType);
            Assert.Equal("Hey \\\n   There lol", parser.ValueSpan.ToString());
            Assert.Equal("Hey \\\n   There lol", parser.GetString());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Fact]
        public void EscapedNewLineWithCRLF()
        {
            var parser = new SdlReader("\\\r\nb");
            parser.Read();
            Assert.Equal(SdlTokenType.Identifier, parser.TokenType);
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Theory]
        [InlineData("{", SdlTokenType.BlockOpen)]
        [InlineData("}", SdlTokenType.BlockClose)]
        [InlineData("=", SdlTokenType.Equals)]
        public void Operators(string code, SdlTokenType expectedType)
        {
            var parser = new SdlReader(code);
            parser.Read();
            Assert.Equal(expectedType, parser.TokenType);
            Assert.Equal(code, parser.ValueSpan.ToString());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Theory]
        [InlineData("true", SdlTokenType.BooleanTrue)]
        [InlineData("false", SdlTokenType.BooleanFalse)]
        [InlineData("on", SdlTokenType.BooleanTrue)]
        [InlineData("off", SdlTokenType.BooleanFalse)]
        [InlineData("null", SdlTokenType.Null)]
        public void Keywords(string code, SdlTokenType expectedType)
        {
            var parser = new SdlReader(code);
            parser.Read();
            Assert.Equal(expectedType, parser.TokenType);
            Assert.Equal(code, parser.ValueSpan.ToString());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Theory]
        [InlineData("[]", "")]
        [InlineData("[lol\nyomama+\n==]", "lol\nyomama+\n==")]
        public void Base64(string code, string value)
        {
            var parser = new SdlReader(code);
            parser.Read();
            Assert.Equal(SdlTokenType.Binary, parser.TokenType);
            Assert.Equal(value, parser.ValueSpan.ToString());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Theory]
        [InlineData("0", SdlTokenType.NumberInt32, "0")]
        [InlineData("-69", SdlTokenType.NumberInt32, "-69")]
        [InlineData("420L", SdlTokenType.NumberInt64, "420")]
        [InlineData("6.9f", SdlTokenType.NumberFloat32, "6.9")]
        [InlineData("69F", SdlTokenType.NumberFloat32, "69")]
        [InlineData("4.20d", SdlTokenType.NumberFloat64, "4.20")]
        [InlineData("420D", SdlTokenType.NumberFloat64, "420")]
        [InlineData("69.420bd", SdlTokenType.NumberFloat128, "69.420")]
        public void Numbers(string code, SdlTokenType expectedType, string expectedValue)
        {
            var parser = new SdlReader(code);
            parser.Read();
            Assert.Equal(expectedType, parser.TokenType);
            Assert.Equal(expectedValue, parser.ValueSpan.ToString());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Fact]
        public void Date()
        {
            var parser = new SdlReader("2021/03/25");
            parser.Read();
            Assert.Equal(SdlTokenType.Date, parser.TokenType);
            Assert.Equal("2021/03/25", parser.ValueSpan.ToString());
            Assert.Equal(new DateTimeOffset(2021, 03, 25, 0, 0, 0, TimeSpan.Zero), parser.DateTimeValue);
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Theory]
        [InlineData("2021/03/25 21:19",                  2021, 03, 25, 21, 19, 0,  0,   0)]
        [InlineData("2021/03/25 21:19:42",               2021, 03, 25, 21, 19, 42, 0,   0)]
        [InlineData("2021/03/25 21:19:42.345",           2021, 03, 25, 21, 19, 42, 345, 0)]
        [InlineData("2021/03/25 21:19:42.345-GMT",       2021, 03, 25, 21, 19, 42, 345, 0)]
        [InlineData("2021/03/25 21:19:42.345-GMT+01",    2021, 03, 25, 21, 19, 42, 345, 1)]
        [InlineData("2021/03/25 21:19:42.345-GMT+01:00", 2021, 03, 25, 21, 19, 42, 345, 1)]
        [InlineData("2021/03/25 21:19:42.345-GMT-01:00", 2021, 03, 25, 21, 19, 42, 345, -1)]
        public void DateTime(string code, int year, int month, int day, int hour, int minute, int second, int ms, int timespanHours)
        {
            var parser = new SdlReader(code);
            parser.Read();
            Assert.Equal(SdlTokenType.DateTime, parser.TokenType);
            Assert.Equal(code, parser.ValueSpan.ToString());
            Assert.Equal(new DateTimeOffset(year, month, day, hour, minute, second, ms, TimeSpan.FromHours(timespanHours)), parser.DateTimeValue);
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Theory]
        [InlineData("12:34:56",             0,   12,  34,   56, 0)]
        [InlineData("10d:12:34:56",         10,  12,  34,   56, 0)]
        [InlineData("10d:12:34:56.789",     10,  12,  34,   56, 789)]
        [InlineData("-10d:12:34:56.789",    -10, -12, -34, -56, -789)]
        public void Time(string code, int days, int hours, int minutes, int seconds, int ms)
        {
            var parser = new SdlReader(code);
            parser.Read();
            Assert.Equal(SdlTokenType.TimeSpan, parser.TokenType);
            Assert.Equal(code, parser.ValueSpan.ToString());
            Assert.Equal(new TimeSpan(days, hours, minutes, seconds, ms), parser.TimeSpanValue);
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Fact]
        public void IDontKnowIfThisIsParsingProperlySoThisTestExist()
        {
            var parser = new SdlReader(
                @"`
Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent congue accumsan enim, eget pretium erat lobortis id. Suspendisse dapibus velit id facilisis tincidunt. Aenean convallis nibh eget elit congue tincidunt. Fusce blandit orci ac ornare facilisis. Vivamus ut felis a metus euismod sodales. Morbi imperdiet quam lectus, a aliquam ipsum commodo faucibus. Nulla viverra dignissim odio, vitae molestie tortor semper ac. Ut at ultrices est, eget rutrum lacus.

Donec est orci, sollicitudin ac viverra sed, fermentum ut erat. Aenean sed enim eu leo dignissim iaculis nec at justo. Phasellus placerat leo sed aliquam hendrerit. Nam varius mi et sem imperdiet, accumsan semper lectus faucibus. Aenean eu ultrices est. Morbi est ligula, porttitor vitae euismod a, malesuada eget felis. Maecenas in consequat ante, vel pretium metus. Integer vitae orci tortor. Mauris non neque in sapien semper sagittis vitae eu augue. Nulla nulla elit, iaculis id gravida nec, fringilla eget lorem. Vivamus pellentesque cursus ex vitae rutrum. Integer tortor nunc, pharetra ut iaculis dignissim, efficitur blandit turpis. Aliquam erat volutpat. Quisque pulvinar velit est, nec scelerisque odio semper ac.

Nulla fermentum magna sit amet hendrerit lacinia. Pellentesque eu cursus enim. Curabitur pharetra mauris vitae semper ullamcorper. Vestibulum nec ligula accumsan, dictum ex sed, ornare mauris. Curabitur justo eros, lacinia eu eleifend quis, finibus mattis elit. Duis ac neque congue, dapibus urna vel, euismod sem. Nulla feugiat dapibus neque, vitae posuere nisl semper quis. Nam tristique consequat erat, quis vestibulum lectus tristique vitae. Interdum et malesuada fames ac ante ipsum primis in faucibus. Nunc congue magna id odio auctor, ac ornare turpis aliquam. Cras eu lectus eu mauris porta venenatis.

Curabitur vitae porta quam. Nunc faucibus volutpat mauris non consectetur. Praesent vel ligula tellus. Duis mi odio, venenatis quis odio consequat, tristique volutpat lectus. In hac habitasse platea dictumst. Vestibulum et diam ullamcorper, porttitor dui id, rutrum felis. Sed condimentum ante eget nunc rutrum tincidunt eget a nisi.

Aliquam tempor, libero non tristique suscipit, orci ipsum tempus sapien, ac maximus ante ligula at mi. Suspendisse commodo semper pharetra. Suspendisse id diam dui. Integer tortor orci, ullamcorper at urna et, aliquet congue mauris. Fusce convallis nec mauris vel dictum. Integer imperdiet sodales risus, in scelerisque urna. Fusce ac libero in ex accumsan accumsan non tincidunt arcu. Curabitur aliquet orci ac est tristique, vel egestas mi malesuada. Nunc lacinia ultricies dolor quis posuere. Donec iaculis pretium est, vel tempus risus rhoncus in. Vestibulum elit erat, porttitor id condimentum in, pretium eget nulla. Nullam ac interdum quam. Praesent accumsan arcu sit amet bibendum cursus.

Nulla facilisi. Pellentesque erat risus, gravida sit amet magna vitae, eleifend vehicula risus. Sed tincidunt ultricies felis, non fringilla urna laoreet sit amet. Integer in ex dapibus, aliquet leo quis, fermentum turpis. Etiam non ornare libero. Mauris in enim sed dui efficitur dignissim. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae;

Maecenas vitae libero non erat tempor ultrices. Donec a tempus mauris. In suscipit lorem sapien, nec ullamcorper ante cursus pulvinar. Nulla ac nisi lorem. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Ut molestie non eros quis ornare. Quisque sollicitudin, dui ut feugiat tincidunt, ipsum risus convallis metus, lobortis rhoncus magna leo eu tellus. Duis at quam turpis. Integer et nisl facilisis, vehicula erat vitae, sagittis nisi.

Quisque in auctor lectus, sit amet porta mauris. Donec ac nulla vitae erat vestibulum tempus a non tortor. Phasellus sapien nisl, condimentum porta cursus vitae, dapibus sit amet nisl. Praesent sed augue magna. Sed urna risus, aliquam sit amet sem quis, elementum tempus diam. Donec molestie nisi eget ultrices hendrerit. Mauris vitae lacus venenatis, consequat augue a, iaculis nisl. Praesent molestie, velit sit amet ornare tincidunt, nisl est vehicula sapien, auctor tempor quam dolor vitae turpis. Quisque fermentum maximus ex, eget gravida velit rhoncus quis. Etiam at lacus eu neque varius bibendum sit amet sed est. Praesent efficitur erat id massa auctor tincidunt. Proin convallis eleifend justo, nec accumsan nibh luctus eu. Maecenas mollis tincidunt enim, eu placerat nunc rutrum in.

Vivamus lacinia ipsum ac venenatis interdum. Nam fringilla a metus ut eleifend. Curabitur dolor lectus, maximus a nulla sit amet, faucibus convallis lectus. Vestibulum consequat augue elit, id egestas sapien eleifend ac. Fusce finibus dolor a risus congue, a tristique augue accumsan. Nam sollicitudin, lectus vel gravida viverra, erat ante rhoncus urna, et ornare eros elit et leo. Sed condimentum, sapien vitae ultrices volutpat, erat arcu venenatis sapien, eu pretium risus velit quis ipsum. Duis porttitor turpis at purus faucibus tempor quis quis nulla. Vivamus aliquam sollicitudin scelerisque. Suspendisse ornare nec leo varius hendrerit. Praesent pellentesque urna velit, at euismod tellus rhoncus sed. Morbi bibendum erat urna. Vivamus mauris enim, euismod ac efficitur sit amet, euismod vitae erat. Sed tincidunt dolor a eros vestibulum tincidunt. Sed lacinia, ante nec efficitur bibendum, lectus augue vestibulum turpis, sed commodo justo justo non mauris. Aenean ornare sapien leo, id congue leo vestibulum eu.

Cras eu mi rutrum, tempus augue euismod, semper lectus. Etiam tempus est vitae porta vestibulum. Mauris sagittis odio nulla, at facilisis lorem malesuada in. Sed porttitor bibendum lobortis. Vivamus rhoncus quis neque id accumsan. Maecenas id massa sit amet odio rutrum sollicitudin. Ut mattis erat id enim tincidunt, non volutpat risus molestie. Aliquam erat volutpat. Interdum et malesuada fames ac ante ipsum primis in faucibus. Donec molestie tellus id mi dignissim, et elementum libero elementum. 
`"
            );
            parser.Read();
            Assert.Equal(SdlTokenType.StringBackQuoted, parser.TokenType);
            Assert.Contains("Cras eu mi rutrum", parser.ValueSpan.ToString());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Theory]
        [InlineData(";")]
        [InlineData("\n")]
        public void EndOfLine(string code)
        {
            var parser = new SdlReader(code);
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfLine, parser.TokenType);
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Fact]
        public void EmptyStringDoesntCrash()
        {
            var parser = new SdlReader("\"\"");
            parser.Read();
            Assert.Equal(SdlTokenType.StringDoubleQuoted, parser.TokenType);
            Assert.Equal("", parser.ValueSpan.ToString());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Fact]
        public void EmptyStringWithEscapedLineBreakDoesntCrash()
        {
            var parser = new SdlReader("\"\\\r\n\"");
            parser.Read();
            Assert.Equal(SdlTokenType.StringDoubleQuoted, parser.TokenType);
            Assert.Equal("\\\r\n", parser.ValueSpan.ToString());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }
    }
}
