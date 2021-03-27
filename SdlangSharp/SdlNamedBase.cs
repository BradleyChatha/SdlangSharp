#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace SdlangSharp
{
    public abstract class SdlNamedBase
    {
        private string _qualifiedName;
        private string? _name;
        private string? _namespace;
        private int? _namespaceColonIndex;

        public SdlNamedBase(string qualifiedName)
        {
            this.QualifiedName = qualifiedName;
        }

        public SdlNamedBase(string name, string @namespace)
        {
            this._qualifiedName = name+';'+@namespace;
            this._name = name;
            this._namespace = @namespace;
        }

        public string QualifiedName
        {
            get => _qualifiedName;
            set
            {
                var index = value.IndexOf(':');
                if (index >= 0)
                    this._namespaceColonIndex = index;
                this._qualifiedName = value;
                this._name = null;
                this._namespace = null;
            }
        }

        public string Name
        {
            get
            {
                if(this._name != null)
                    return this._name;
                else if(this._namespaceColonIndex.HasValue)
                {
                    var index = this._namespaceColonIndex.Value + 1;
                    this._name = this.QualifiedName[index..];
                }
                else
                    this._name = this.QualifiedName;
                return this._name;
            }
        }

        public string? Namespace
        {
            get
            {
                if(this._namespace != null)
                    return this._namespace;
                else if(this._namespaceColonIndex.HasValue)
                    this._namespace = this.QualifiedName[0..this._namespaceColonIndex.Value];
                
                return this._namespace;
            }
        }
    }
}
