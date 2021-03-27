using SdlangSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Tests.ShouldPass
{
    public class AstTests
    {
        [Fact]
        public void SimpleTag()
        {
            var node = new SdlReader("this:is a=`simple` 1 line=\"tag\" on 2021/03/27").ToAst();
            Assert.Equal(1, node.Children.Count);
            Assert.True(node.HasChildAt(0));
            Assert.True(node.HasChildCalled("this:is"));

            node = node.Children[0];
            Assert.Equal(2, node.Attributes.Count);
            Assert.Equal(3, node.Values.Count);
            Assert.Equal("is", node.Name);
            Assert.Equal("this", node.Namespace);
            Assert.Equal("this:is", node.QualifiedName);
            Assert.True(node.HasAttributeCalled("a"));
            Assert.Equal("simple", node.GetAttributeString("a"));
            Assert.True(node.HasAttributeCalled("line"));
            Assert.Equal("tag", node.GetAttributeString("line"));
            Assert.True(node.HasValueAt(0) && node.HasValueAt(1) && node.HasValueAt(2));
            Assert.Equal(1, node.GetValueInteger(0));
            Assert.True(node.GetValueBoolean(1));
            Assert.Equal(new DateTimeOffset(2021, 03, 27, 0, 0, 0, TimeSpan.Zero), node.GetValueDateTime(2));

            var asString = node.ToSdlString(false);
            Assert.StartsWith("this:is 1 true 2021/03/27 00:00:00 ", asString);
            Assert.True(asString.EndsWith("a=`simple` line=`tag` \n") || asString.EndsWith("line=`tag` a=`simple` \n"));
        }

        [Fact]
        public void SimpleMatrix()
        {
            var node = new SdlReader(@"
                matrix {
                    1 1 1
                    2 2 2
                    3 3 3
                }
            ").ToAst();

            var matrix = node.Children[0];
            Assert.Equal(3, matrix.Children.Count); // 3 anonymous tags.
            Assert.Equal(18, matrix.GetChildrenCalled("content")
                                   .SelectMany(c => c.Values)
                                   .Select(v => v.Integer)
                                   .Aggregate((a, b) => a + b)
            );

            var asString = node.ToSdlString();
            Assert.Equal("matrix {\n    1 1 1 \n    2 2 2 \n    3 3 3 \n}\n", asString);
        }

        [Fact]
        public void Children()
        {
            var parent = new SdlReader(@"
                parent name=""unga"" {
                    child id=1 name=""ugu""
                    child id=2 name=""gaga""
                    siblings {
                        brother name=""bunga""
                    }
                }
            ").ToAst().Children[0];

            Assert.Equal("unga", parent.GetAttributeString("name"));

            int expectedId = 1;
            foreach(var child in parent.GetChildrenCalled("child"))
                Assert.Equal(expectedId++, child.GetAttributeInteger("id"));

            var brother = parent.GetChildrenCalled("siblings").First().Children[0];
            Assert.Equal("bunga", brother.GetAttributeString("name"));
        }
    }
}
