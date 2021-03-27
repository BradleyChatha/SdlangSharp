using System;
using Xunit;
using SdlangSharp;
using System.Collections.Generic;
using System.Linq;

namespace Tests.ShouldPass
{
    // Tests anything that tests multiple different tokens at the same time.
    public class SdlReaderCompound
    {
        // This is an important test as otherwise tags might accidentally be concatted together during AST construction
        // as new lines/semi-colons seperate tags.
        [Theory]
        [InlineData("#\n",                                  SdlTokenType.Comment)]
        [InlineData("--\n",                                 SdlTokenType.Comment)]
        [InlineData("//\n",                                 SdlTokenType.Comment)]
        [InlineData("a\n",                                  SdlTokenType.Identifier)]
        [InlineData("\"\"\n",                               SdlTokenType.StringDoubleQuoted)]
        [InlineData("\"\\\n\"\n",                           SdlTokenType.StringDoubleQuoted)]
        [InlineData("``\n",                                 SdlTokenType.StringBackQuoted)]
        [InlineData("`\n`\n",                               SdlTokenType.StringBackQuoted)]
        [InlineData("0\n",                                  SdlTokenType.NumberInt32)]
        [InlineData("0l\n",                                 SdlTokenType.NumberInt64)]
        [InlineData("12.f\n",                               SdlTokenType.NumberFloat32)]
        [InlineData("12.bd\n",                              SdlTokenType.NumberFloat128)]
        [InlineData(";\n",                                  SdlTokenType.EndOfLine)]
        [InlineData("1111/11/11\n",                         SdlTokenType.Date)]
        [InlineData("1111/11/11 11:11:11.11-GMT+11:11\n",   SdlTokenType.DateTime)]
        [InlineData("1d:11:11:11.11\n",                     SdlTokenType.TimeSpan)]
        [InlineData("[asbbd\nsaidn]\n",                     SdlTokenType.Binary)]
        public void ValuesWithLeadingNewLineKeepNewLine(string code, SdlTokenType expectedType)
        {
            var parser = new SdlReader(code);
            parser.Read();
            Assert.Equal(expectedType, parser.TokenType);
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfLine, parser.TokenType);
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Fact]
        public void ChainLineBreakDoesntCrash()
        {
            var parser = new SdlReader("\"\\\n\\\n\\\n\\\n\\\n\"");
            parser.Read();
            Assert.Equal(SdlTokenType.StringDoubleQuoted, parser.TokenType);
            Assert.Equal("\\\n\\\n\\\n\\\n\\\n", parser.ValueSpan.ToString());
            Assert.Equal("", parser.GetString());
            parser.Read();
            Assert.Equal(SdlTokenType.EndOfFile, parser.TokenType);
        }

        [Fact]
        public void TagWithValue()
        {
            TokenTypeAssert(
                "some:tag `with a value`",
                SdlTokenType.Identifier,
                SdlTokenType.StringBackQuoted,
                SdlTokenType.EndOfFile
            );
        }

        [Fact]
        public void TagWithValues()
        {
            TokenTypeAssert(
                "some:tag `st\nr` \"str\\\n\" 0 0l 1.f 1.0d 1.0bd true on false off [abc] 11:11:11 1111/11/11 1111/11/11 11:11:11.11",
                SdlTokenType.Identifier,
                SdlTokenType.StringBackQuoted,
                SdlTokenType.StringDoubleQuoted,
                SdlTokenType.NumberInt32,
                SdlTokenType.NumberInt64,
                SdlTokenType.NumberFloat32,
                SdlTokenType.NumberFloat64,
                SdlTokenType.NumberFloat128,
                SdlTokenType.BooleanTrue,
                SdlTokenType.BooleanTrue,
                SdlTokenType.BooleanFalse,
                SdlTokenType.BooleanFalse,
                SdlTokenType.Binary,
                SdlTokenType.TimeSpan,
                SdlTokenType.Date,
                SdlTokenType.DateTime,
                SdlTokenType.EndOfFile
            );
        }

        [Fact]
        public void TagWithAttribute()
        {
            TokenTypeAssert(
                "some:tag with:an=`attribute` lol=true",
                SdlTokenType.Identifier,
                SdlTokenType.Identifier,
                SdlTokenType.Equals,
                SdlTokenType.StringBackQuoted,
                SdlTokenType.Identifier,
                SdlTokenType.Equals,
                SdlTokenType.BooleanTrue,
                SdlTokenType.EndOfFile
            );
        }

        [Fact]
        public void WeirdCommentEdgeCaseWhereSkipLineReadTooManyCharacters()
        {
            // It was due to bad SIMD maths.
            TokenTypeAssert(
                @"
# To retrieve the values from the matrix (as a list of lists)
#
#     List rows = tag.getChild(""matrix"").getChildrenValues(""content"");

a
Lorem",
                SdlTokenType.EndOfLine,
                SdlTokenType.Comment, SdlTokenType.EndOfLine,
                SdlTokenType.Comment, SdlTokenType.EndOfLine,
                SdlTokenType.Comment, SdlTokenType.EndOfLine,
                SdlTokenType.EndOfLine,
                SdlTokenType.Identifier, SdlTokenType.EndOfLine,
                SdlTokenType.Identifier, SdlTokenType.EndOfFile
            );
        }

        [Fact]
        public void OhLawdHeComin() // Actual name: Token test for some of the example file from sdlang-d, will do more as I can be arsed with.
        {
            TokenTypeAssert(
                FULL_EXAMPLE_FILE,

                SdlTokenType.Comment, SdlTokenType.EndOfLine,
                SdlTokenType.Identifier, SdlTokenType.EndOfLine,
                SdlTokenType.EndOfLine,
                SdlTokenType.Comment, SdlTokenType.EndOfLine,
                SdlTokenType.Identifier, SdlTokenType.StringDoubleQuoted, SdlTokenType.EndOfLine,
                SdlTokenType.Identifier, SdlTokenType.StringDoubleQuoted, SdlTokenType.EndOfLine,
                SdlTokenType.Identifier, SdlTokenType.NumberInt32, SdlTokenType.EndOfLine,
                SdlTokenType.EndOfLine,
                SdlTokenType.Comment, SdlTokenType.EndOfLine,
                SdlTokenType.Identifier, SdlTokenType.StringDoubleQuoted, SdlTokenType.StringDoubleQuoted, SdlTokenType.NumberInt32, SdlTokenType.EndOfLine,
                SdlTokenType.EndOfLine,
                SdlTokenType.Comment, SdlTokenType.EndOfLine,
                SdlTokenType.Identifier, SdlTokenType.Identifier, SdlTokenType.Equals, SdlTokenType.StringDoubleQuoted, 
                    SdlTokenType.Identifier, SdlTokenType.Equals, SdlTokenType.StringDoubleQuoted, 
                    SdlTokenType.Identifier, SdlTokenType.Equals, SdlTokenType.NumberInt32,
                    SdlTokenType.EndOfLine,
                SdlTokenType.EndOfLine,
                SdlTokenType.Comment, SdlTokenType.EndOfLine,
                SdlTokenType.Identifier, SdlTokenType.StringDoubleQuoted, SdlTokenType.StringDoubleQuoted,
                    SdlTokenType.Identifier, SdlTokenType.Equals, SdlTokenType.NumberInt32,
                    SdlTokenType.EndOfLine,

                SdlTokenType.EndOfFile
            );
        }

        private void TokenTypeAssert(string code, params SdlTokenType[] expectedTypes)
        {
            var parser = new SdlReader(code);
            var got = new List<SdlTokenType>();
            while(parser.TokenType != SdlTokenType.EndOfFile)
            {
                parser.Read();
                got.Add(parser.TokenType);
            }

            if(!got.SequenceEqual(expectedTypes))
            {
                Assert.True(false, 
                    $"Got:\n{got.Select(v => $"{v}").Aggregate((a,b)=>a+"\n"+b)}" +
                    $"\n\nExpected:\n{expectedTypes.Select(v => $"{v}").Aggregate((a,b) => a + "\n" + b)}"
                );
            }
        }

        public const string FULL_EXAMPLE_FILE = @"# a tag having only a name
my_tag

# three tags acting as name value pairs
first_name ""Akiko""
last_name ""Johnson""
height 68

# a tag with a value list
person ""Akiko"" ""Johnson"" 68

# a tag with attributes
person first_name = ""Akiko"" last_name=""Johnson"" height=68

# a tag with values and attributes
person ""Akiko"" ""Johnson"" height=60
";
    }
}
