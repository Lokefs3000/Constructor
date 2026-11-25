using System;
using System.Collections.Generic;
using System.Text;
using PrimaryEditor.Inspector.Widgets;

namespace PrimaryEditor.Inspector.Pooling.Dedicated
{
    public interface IInspectorWidgetPool : IDisposable
    {
        public InspectorWidget GetWidget();
        public void ReturnWidget(InspectorWidget widget);
    }
}
