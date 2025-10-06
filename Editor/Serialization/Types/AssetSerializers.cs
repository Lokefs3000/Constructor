using Primary.Assets;
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
            RenderMesh? val = (RenderMesh?)obj;
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
