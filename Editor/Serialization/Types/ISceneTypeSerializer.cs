using System.Globalization;
using System.Text;

namespace Editor.Serialization.Types
{
    public interface ISceneTypeSerializer
    {
        public void Serialize(StringBuilder sb, object? obj);
    }

    internal sealed class ToStringSerializer : ISceneTypeSerializer
    {
        public void Serialize(StringBuilder sb, object? obj)
        {
            if (obj == null)
                sb.Append("null");
            else if (obj is IFormattable formattable)
                sb.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
            else
                sb.Append(obj.ToString());
        }
    }
}
