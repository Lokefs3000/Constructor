using Primary.Rendering.Assets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.Interaction.Draw
{
    public sealed class SelectionDrawList
    {
        private List<SelectionDrawCmd> _cmds;

        internal SelectionDrawList()
        {
            _cmds = new List<SelectionDrawCmd>();
        }

        internal void Clear()
        {
            _cmds.Clear();
        }

        public void AddMesh(RawRenderMesh mesh, Matrix4x4 model)
        {
            _cmds.Add(new SelectionDrawCmd(mesh, model));
        }
    }

    public readonly record struct SelectionDrawCmd(RawRenderMesh RenderMesh, Matrix4x4 Model);
}
