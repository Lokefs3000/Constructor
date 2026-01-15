using Editor.Shaders.Attributes;
using Primary.Common;

namespace Editor.Shaders.Data
{
    public readonly record struct AttributeData(AttributeSignature Signature, AttributeVarData[]? Data, IndexRange DeclerationRange)
    {
        public T? GetVariable<T>(string name, T? @default = default)
        {
            if (Data == null)
                return @default;

            int idx = Signature.Signature.FindIndex((x) => x.Name == name);
            return (T?)Array.Find(Data, (x) => x.SourceIndex == idx).Value;
        }

        public bool TryGetVariable<T>(string name, out T? result)
        {
            //Unsafe.SkipInit(out result);
            result = default;

            if (Data == null)
                return false;

            int idx = Signature.Signature.FindIndex((x) => x.Name == name);
            idx = Array.FindIndex(Data, (x) => x.SourceIndex == idx);

            if (idx == -1)
                return false;

            result = (T?)Data[idx].Value;
            return true;
        }
    }

    public readonly record struct AttributeVarData(int SourceIndex, object? Value);
}
