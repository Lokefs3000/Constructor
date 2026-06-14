using Editor.Assets;
using Editor.Storage;
using Editor.UI;
using Editor.UI.Assets;
using Editor.UI.Elements;
using Editor.UI.Elements.Tree;
using Editor.UI.Serialization;
using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Windows
{
    internal sealed class AssetBrowser : UIWindow
    {
        public AssetBrowser(int uniqueWindowId) : base(uniqueWindowId)
        {
            WindowTitle = "Asset browser";


        }
    }
}
