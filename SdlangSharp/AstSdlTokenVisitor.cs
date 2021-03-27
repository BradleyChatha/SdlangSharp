using System;
using System.Collections.Generic;
using System.Text;

namespace SdlangSharp
{
    public class AstSdlTokenVisitor : ISdlTokenVisitor
    {
        Stack<SdlTag> _parentStack;
        SdlTag _currentNode;

        public void Reset()
        {
            this._parentStack = new Stack<SdlTag>();
            this._parentStack.Push(new SdlTag("root"));
        }

        public void VisitOpenBlock()
        {
            this._parentStack.Push(this._currentNode);
        }

        public void VisitCloseBlock()
        {
            if(this._parentStack.Count == 1)
                throw new SdlException("Stray '}'. We are not inside an open block (started by '{').");
            this._currentNode = null;
            this._parentStack.Pop();
        }

        public void VisitStartTag(ReadOnlySpan<char> qualifiedName)
        {
            this._currentNode = new SdlTag(qualifiedName.ToString());
            this._parentStack.Peek().Children.Add(this._currentNode);
        }

        public void VisitComment(ReadOnlySpan<char> comment)
        {
        }

        public void VisitEndOfFile()
        {
            if(this._parentStack.Count != 1)
                throw new SdlException($"Tag {this._parentStack.Peek().QualifiedName} does not have a closing bracket '}}'");
        }

        public void VisitNewAttribute(ReadOnlySpan<char> qualifiedName, SdlValue value)
        {
            var nameAsString = qualifiedName.ToString();
            if(this._currentNode.HasAttributeCalled(nameAsString))
                throw new SdlException($"Tag {this._currentNode.QualifiedName} already has an attribute called {nameAsString}");
            this._currentNode.Attributes[nameAsString] = new SdlAttribute(nameAsString, value);
        }

        public void VisitNewValue(SdlValue value)
        {
            this._currentNode.Values.Add(value);
        }

        public SdlTag RootNode 
        {
            get
            {
                if(this._parentStack.Count != 1)
                    throw new SdlException("This visitor hasn't parsed anything yet, or the parsing failed as the root node isn't on top.");
                return this._parentStack.Peek();
            }
        }
    }
}
