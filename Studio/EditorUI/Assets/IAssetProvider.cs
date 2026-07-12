using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace EditorUI.Assets
{
    public interface IAssetProvider<T>
    {
        [MemberNotNull(nameof(Value))]
        public bool IsReadyToUse { get; }

        public T? Value { get; }
    }
}
