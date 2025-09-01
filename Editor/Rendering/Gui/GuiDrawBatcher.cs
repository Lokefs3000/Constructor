using CommunityToolkit.HighPerformance;
using Editor.LegacyGui.Data;
using Editor.LegacyGui.Elements;
using Microsoft.Extensions.ObjectPool;
using System.Runtime.CompilerServices;

namespace Editor.Rendering.Gui
{
    internal class GuiDrawBatcher
    {
        private ObjectPool<UIBatchContainer> _batchPool;
        private List<UIBatchContainer> _activeBatches;

        private GuiFont _defaultFont;

        internal GuiDrawBatcher(GuiFont defaultFont)
        {
            _batchPool = ObjectPool.Create(new UIBatchContainerPolicy());
            _activeBatches = new List<UIBatchContainer>();

            _defaultFont = defaultFont;
        }

        internal void Clear()
        {
            for (int i = 0; i < _activeBatches.Count; i++)
                _batchPool.Return(_activeBatches[i]);

            _activeBatches.Clear();
        }

        internal void Build(DockSpace dockSpace, GuiCommandBuffer commandBuffer, GuiMeshBuilder meshBuilder)
        {
            Span<GuiDraw> draws = commandBuffer.Draws;
            if (draws.IsEmpty)
                return;

            UIBatchContainer batchContainer = _batchPool.Get();
            batchContainer.TargetDockSpace = dockSpace;

            GuiDrawType drawType = GuiDrawType.Unknown;

            int index = 0;
            while (index < draws.Length)
            {
                ref GuiDraw draw = ref draws[index++];
                if (draw.Type == GuiDrawType.Unknown)
                    continue;

                if (draw.Type != drawType)
                {
                    if (drawType == GuiDrawType.Unknown)
                        drawType = draw.Type;

                    batchContainer.AddBatch(new UIBatch
                    {
                        Type = draw.Type switch
                        {
                            GuiDrawType.Unknown => UIBatchType.None,
                            GuiDrawType.SolidRect => UIBatchType.Solid,
                            GuiDrawType.RegularText => UIBatchType.Text,
                            _ => UIBatchType.None
                        },
                        Value = draw.Type switch
                        {
                            GuiDrawType.RegularText => _defaultFont.Graphic,
                            _ => null
                        },
                        IndexOffset = (uint)meshBuilder.IndexCount
                    });
                }
                drawType = draw.Type;

                switch (draw.Type)
                {
                    case GuiDrawType.SolidRect:
                        {
                            GuiDrawSolidRect solidRect = draw.SolidRect;
                            meshBuilder.AppendRect(solidRect.Minimum, solidRect.Maximum, solidRect.Color);
                            break;
                        }
                    case GuiDrawType.RegularText:
                        {
                            GuiDrawRegularText regularText = draw.RegularText;
                            meshBuilder.AppendText(regularText.Cursor, regularText.Text, regularText.Scale, _defaultFont, regularText.Color);
                            break;
                        }
                }
            }

            batchContainer.AddBatch(new UIBatch { Type = UIBatchType.None, IndexOffset = (uint)meshBuilder.IndexCount });
            _activeBatches.Add(batchContainer);
        }

        internal Span<UIBatchContainer> BatchContainers => _activeBatches.AsSpan();

        internal struct UIBatchContainerPolicy : IPooledObjectPolicy<UIBatchContainer>
        {
            public UIBatchContainer Create()
            {
                return new UIBatchContainer();
            }

            public bool Return(UIBatchContainer obj)
            {
                obj.Reset();
                return true;
            }
        }
    }

    internal class UIBatchContainer
    {
        private DockSpace? _targetDockSpace;
        private List<UIBatch> _batches;

        public UIBatchContainer()
        {
            _targetDockSpace = null;
            _batches = new List<UIBatch>();
        }

        internal void Reset()
        {
            _targetDockSpace = null;
            _batches.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddBatch(UIBatch batch) => _batches.Add(batch);

        internal DockSpace? TargetDockSpace { get => _targetDockSpace; set => _targetDockSpace = value; }
        internal Span<UIBatch> Batches => _batches.AsSpan();
    }

    internal record struct UIBatch
    {
        public UIBatchType Type;
        public object? Value;

        public uint IndexOffset;
    }

    internal enum UIBatchType : byte
    {
        None = 0,

        Solid,
        Text
    }
}
