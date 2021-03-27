using SdlangSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Tests.ShouldPass
{
    enum VisitType
    {
        ERROR,
        OpenBlock,
        CloseBlock,
        Comment,
        StartTag,
        NewValue,
        NewAttribute,
        EndOfFile
    }

    class AssertingVisistor : ISdlTokenVisitor
    {
        readonly Queue<VisitType> _expected;

        public AssertingVisistor(Queue<VisitType> expected)
        {
            this._expected = expected;
        }

        void AssertIsExpected(VisitType type)
        {
            Assert.NotEmpty(this._expected);
            Assert.Equal(this._expected.Dequeue(), type);
        }

        public void AssertEmpty()
        {
            Assert.Empty(this._expected);
        }

        public void VisitCloseBlock() => AssertIsExpected(VisitType.CloseBlock);
        public void VisitComment(ReadOnlySpan<char> comment) => AssertIsExpected(VisitType.Comment);
        public void VisitEndOfFile() => AssertIsExpected(VisitType.EndOfFile);
        public void VisitNewAttribute(ReadOnlySpan<char> qualifiedName, SdlValue value) => AssertIsExpected(VisitType.NewAttribute);
        public void VisitNewValue(SdlValue value) => AssertIsExpected(VisitType.NewValue);
        public void VisitOpenBlock() => AssertIsExpected(VisitType.OpenBlock);
        public void VisitStartTag(ReadOnlySpan<char> qualifiedName) => AssertIsExpected(VisitType.StartTag);

        public void Reset()
        {
        }
    }

    public class SdlTokenPusherTests
    {
        [Fact]
        public void SingleTag()
        {
            AssertCorrectVisit("some:tag", new[]{ VisitType.StartTag, VisitType.EndOfFile });
        }

        [Theory]
        [InlineData("tag:one ; tag:two")]
        [InlineData("tag:one\ntag:two")]
        public void MultipleTags(string code)
        {
            AssertCorrectVisit(code, new[]{ VisitType.StartTag, VisitType.StartTag, VisitType.EndOfFile });
        }

        [Theory]
        [InlineData("\"value\"")]
        [InlineData("`value`")]
        [InlineData("69")]
        [InlineData("420.0f")]
        [InlineData("true")]
        [InlineData("1111/11/11")]
        [InlineData("1111/11/11 11:11:11-GMT+01")]
        [InlineData("1d:11:11:11")]
        public void AnonymousTagValue(string code)
        {
            AssertCorrectVisit(code, new[]{ VisitType.StartTag, VisitType.NewValue, VisitType.EndOfFile });
        }

        [Fact]
        public void AnonymousTagAttribute()
        {
            AssertCorrectVisit("some:attrib=on", new[]{ VisitType.StartTag, VisitType.NewAttribute, VisitType.EndOfFile });
        }

        [Fact]
        public void ComplexSingleLineTag()
        {
            AssertCorrectVisit("tag with:attribute=`lol` 69", new[]
            {
                VisitType.StartTag, VisitType.NewAttribute, VisitType.NewValue, VisitType.EndOfFile
            });
        }

        [Fact]
        public void ComplexTagWithChildren()
        {
            AssertCorrectVisit("tag with=`children` on {\nchild with:value=420\nchild with:value=off\n}", new[]
            {
                VisitType.StartTag, VisitType.NewAttribute, VisitType.NewValue, 
                VisitType.OpenBlock,
                    VisitType.StartTag, VisitType.NewAttribute,
                    VisitType.StartTag, VisitType.NewAttribute,
                VisitType.CloseBlock,
                VisitType.EndOfFile
            });
        }

        [Theory]
        [InlineData("# comment\ntag\n# comment\ntag")]
        [InlineData("-- comment\ntag\n-- comment\ntag")]
        [InlineData("// comment\ntag\n// comment\ntag")]
        [InlineData("/* comment*/\ntag\n/* comment*/\ntag")]
        public void Comments(string code)
        {
            AssertCorrectVisit(code, new[]
            {
                VisitType.Comment, VisitType.StartTag, VisitType.Comment, VisitType.StartTag, VisitType.EndOfFile
            });
        }

        [Fact]
        public void InlineComment()
        {
            AssertCorrectVisit("tag /**/ 41", new[]
            {
                VisitType.StartTag, VisitType.Comment, VisitType.NewValue, VisitType.EndOfFile
            });
        }

        [Fact]
        public void CanParseExampleFile()
        {
            AssertCorrectVisit(SdlReaderCompound.FULL_EXAMPLE_FILE, new[]
            {
                VisitType.Comment,
                VisitType.StartTag,
                VisitType.Comment,
                VisitType.StartTag, VisitType.NewValue,
                VisitType.StartTag, VisitType.NewValue,
                VisitType.StartTag, VisitType.NewValue,
                VisitType.Comment,
                VisitType.StartTag, VisitType.NewValue, VisitType.NewValue, VisitType.NewValue,
                VisitType.Comment,
                VisitType.StartTag, VisitType.NewAttribute, VisitType.NewAttribute, VisitType.NewAttribute,
                VisitType.Comment,
                VisitType.StartTag, VisitType.NewValue, VisitType.NewValue, VisitType.NewAttribute,
                VisitType.EndOfFile
            });
        }

        void AssertCorrectVisit(string code, IEnumerable<VisitType> expectedTypes)
        {
            var visitTypes = new Queue<VisitType>(expectedTypes);
            var reader = new SdlReader(code);
            var visitor = new AssertingVisistor(visitTypes);
            SdlTokenPusher.ParseAndVisit(reader, new[]{ visitor });
            visitor.AssertEmpty();
        }
    }
}
