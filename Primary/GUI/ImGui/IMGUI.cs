using Primary.Common;

namespace Primary.GUI.ImGui
{
    public static class IMGUI
    {
        private static ImGuiContext? s_context = null;

        internal static ImGuiContext CreateContext()
        {
            ExceptionUtility.Assert(s_context == null);

            s_context = new ImGuiContext();
            return s_context;
        }

        internal static void DestroyContext()
        {
            ExceptionUtility.Assert(s_context != null);

            s_context.Dispose();
            s_context = null;
        }
    }
}
