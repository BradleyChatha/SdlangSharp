using System;
using System.Collections.Generic;
using System.Text;

namespace SdlangSharp
{
    public static class SdlHelper
    {
        public static SdlTag ToAst(this SdlReader reader)
        {
            var visitor = new AstSdlTokenVisitor();
            SdlTokenPusher.ParseAndVisit(reader, visitor);
            return visitor.RootNode;
        }

        public static string ToSdlString(this SdlTag tag, bool isRoot = true)
        {
            var builder = new StringBuilder();
            SdlAstToTextConverter.WriteInto(builder, tag, isRoot);
            return builder.ToString();
        }
    }
}
