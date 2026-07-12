using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Styling;
using Primary.Common;

namespace EditorUI.Widgets.Stylists
{
    public abstract class Stylist : StyledObject
    {
        private readonly Widget _parent;
        private bool _isDestroyed;

        protected string? _id;

        private StateFlags _stateFlags;

        public Stylist(Widget parent)
        {
            _parent = parent;
            _isDestroyed = false;

            _id = null;

            _stateFlags = StateFlags.SelfInvalidStyle;
        }

        internal void Destroy()
        {
            if (!_isDestroyed)
            {
                _isDestroyed = true;
                DestroySelf();
            }
        }

        protected virtual void DestroySelf()
        {

        }

        public override void AddStateFlags(StateFlags flags)
        {
            _stateFlags |= flags;
            _parent.AddStateFlags(flags & ~StateFlags.ThisStyle);
        }

        public override void RemoveStateFlags(StateFlags flags)
        {
            _stateFlags &= ~flags;

            if ((flags & _stateFlags).HasFlags(StateFlags.SelfInvalidStyle))
                _parent.InformOfStylistUpdate(this);
        }

        protected internal override void GetUnstyledObjects(ref StyleQueueContext context)
        {
        }

        [StyleUpdateCallback(nameof(Id))]
        private void OnIdChanged()
        {
            _parent.InformOfStylistIdChange(this);
        }

        public override StateFlags StateFlags => _stateFlags;

        protected internal override StyledObject? ParentObject => _parent;

        #region Serializable
        [Styled(nameof(_id), isEditable: true)] public string? Id { get => _id; set => SetEditedField(value); }
        #endregion
    }
}
