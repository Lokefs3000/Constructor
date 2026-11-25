using Primary.Assets;
using Primary.Assets.Types;
using System.Text;

namespace Editor.Serialization.Types
{
    internal sealed class AssetDefinitionSerializer : ISceneTypeSerializer
    {
        public void Serialize(StringBuilder sb, object? obj)
        {
            IAssetDefinition? val = (IAssetDefinition?)obj;
            if (val == null)
            {
                sb.Append("null");
            }
            else
            {
                sb.Append(val.Id.ToString());
            }
        }
    }

    internal sealed class RenderMeshSerializer : ISceneTypeSerializer
    {
        public void Serialize(StringBuilder sb, object? obj)
        {
            Mesh? val = (Mesh?)obj;
            if (val == null)
            {
                sb.Append("null");
            }
            else
            {
                sb.Append("[\"");
                sb.Append(val.Id);
                sb.Append("\", ");
                sb.Append(val.Model.Id.ToString());
                sb.Append(']');
            }
        }
    }
}
