using Editor.Shaders.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders.Data
{
    public readonly record struct AttributeData(AttributeSignature Signature, AttributeVarData[]? Data);
    public readonly record struct AttributeVarData(int SourceIndex, object? Value);
}
