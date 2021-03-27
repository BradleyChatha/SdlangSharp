using System;
using System.Collections.Generic;
using System.Text;

namespace SdlangSharp
{
    public class NullSdlTokenVisitor : ISdlTokenVisitor
    {
        public void Reset()
        {
        }

        public void VisitCloseBlock()
        {
        }

        public void VisitComment(ReadOnlySpan<char> comment)
        {
        }

        public void VisitEndOfFile()
        {
        }

        public void VisitNewAttribute(ReadOnlySpan<char> qualifiedName, SdlValue value)
        {
        }

        public void VisitNewValue(SdlValue value)
        {
        }

        public void VisitOpenBlock()
        {
        }

        public void VisitStartTag(ReadOnlySpan<char> qualifiedName)
        {
        }
    }

    public class NullSdlTokenRawVisitor : ISdlTokenRawVisitor
    {
        public void Reset()
        {
        }

        public void VisitCloseBlock()
        {
        }

        public void VisitComment(ReadOnlySpan<char> comment)
        {
        }

        public void VisitEndOfFile()
        {
        }

        public void VisitNewAttribute(SdlTokenType valueType, ReadOnlySpan<char> qualifiedName, ReadOnlySpan<char> value)
        {
        }

        public void VisitNewValue(SdlTokenType valueType, ReadOnlySpan<char> value)
        {
        }

        public void VisitOpenBlock()
        {
        }

        public void VisitStartTag(ReadOnlySpan<char> qualifiedName)
        {
        }
    }
}
