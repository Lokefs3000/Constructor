namespace Editor.DearImGui
{
    internal sealed class PopupManager
    {
        private List<IPopup> _popups;

        internal PopupManager()
        {
            _popups = new List<IPopup>();
        }

        internal void Render()
        {
            for (int i = 0; i < _popups.Count; i++)
            {
                if (!_popups[i].Render())
                {
                    _popups.RemoveAt(i--);
                }
            }
        }

        internal void Open(IPopup popup)
        {
            _popups.Add(popup);
        }
    }

    internal interface IPopup
    {
        public bool Render();
    }
}
