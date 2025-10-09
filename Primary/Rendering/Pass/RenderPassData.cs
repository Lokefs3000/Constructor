using System.Collections.Frozen;

namespace Primary.Rendering.Pass
{
    public sealed class RenderPassData
    {
        private FrozenDictionary<Type, IRenderPassDataObject> _storedData;

        internal RenderPassData(params IRenderPassDataObject[] data)
        {
            Dictionary<Type, IRenderPassDataObject> tempDataDict = new Dictionary<Type, IRenderPassDataObject>();
            for (int i = 0; i < data.Length; i++)
            {
                tempDataDict[data[i].GetType()] = data[i];
            }

            _storedData = tempDataDict.ToFrozenDictionary();
        }

        public T? Get<T>() where T : class, IRenderPassDataObject
        {
            _storedData.TryGetValue(typeof(T), out IRenderPassDataObject? obj);
            return obj as T;
        }
    }
}
