using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Primary.IO
{
    public static unsafe class Clipboard
    {
        /// <summary>Not thread-safe</summary>
        public static bool SetText(ReadOnlySpan<char> text)
        {
            if (OperatingSystem.IsWindows())
                return Win32Impl.SetUtf16Text(text);

            return false;
        }

        /// <summary>Not thread-safe</summary>
        public static bool SetText(ReadOnlySpan<byte> text)
        {
            if (OperatingSystem.IsWindows())
                return Win32Impl.SetUtf8Text(text);

            return false;
        }

        /// <summary>Not thread-safe</summary>
        public static string? GetText()
        {
            if (OperatingSystem.IsWindows())
                return Win32Impl.GetUtf16Text();

            return null;
        }

        /// <summary>Not thread-safe</summary>
        public static byte[]? GetBytes()
        {
            if (OperatingSystem.IsWindows())
                return Win32Impl.GetUtf8Text();

            return null;
        }

        /// <summary>Not thread-safe</summary>
        public static bool HasData()
        {
            if (OperatingSystem.IsWindows())
                return Win32Impl.HasData();

            return false;
        }

        private static unsafe class Win32Impl
        {
            internal static bool SetUtf16Text(ReadOnlySpan<char> text)
            {
                if (Windows.OpenClipboard(HWND.NULL).Value == 0)
                    return false;

                try
                {
                    HGLOBAL memory = Windows.GlobalAlloc(GMEM.GMEM_MOVEABLE, (nuint)((text.Length + 1) * sizeof(char)));
                    {
                        char* ptr = (char*)Windows.GlobalLock(memory);

                        text.CopyTo(new Span<char>(ptr, text.Length));
                        ptr[text.Length] = '\0';

                        Windows.GlobalUnlock(memory);
                    }

                    return Windows.SetClipboardData(CF.CF_UNICODETEXT, memory).Value != null;
                }
                finally
                {
                    Windows.CloseClipboard();
                }
            }

            internal static bool SetUtf8Text(ReadOnlySpan<byte> text)
            {
                if (Windows.OpenClipboard(HWND.NULL).Value == 0)
                    return false;

                try
                {
                    HGLOBAL memory = Windows.GlobalAlloc(GMEM.GMEM_MOVEABLE, (nuint)((text.Length + 1) * sizeof(byte)));
                    {
                        byte* ptr = (byte*)Windows.GlobalLock(memory);

                        text.CopyTo(new Span<byte>(ptr, text.Length));
                        ptr[text.Length] = (byte)'\0';

                        Windows.GlobalUnlock(memory);
                    }

                    return Windows.SetClipboardData(CF.CF_TEXT, memory).Value != null;
                }
                finally
                {
                    Windows.CloseClipboard();
                }
            }

            internal static string? GetUtf16Text()
            {
                if (Windows.OpenClipboard(HWND.NULL).Value == 0)
                    return null;

                try
                {
                    string? text = null;

                    HANDLE memory = Windows.GetClipboardData(CF.CF_UNICODETEXT);
                    if (memory.Value == null)
                        return null;

                    {
                        char* ptr = (char*)Windows.GlobalLock(new HGLOBAL(memory));
                        text = new string(ptr, 0, (int)(Windows.GlobalSize(new HGLOBAL(memory)) / sizeof(char)));

                        Windows.GlobalUnlock(new HGLOBAL(memory));
                    }

                    return text;
                }
                finally
                {
                    Windows.CloseClipboard();
                }

                return null;
            }

            internal static byte[]? GetUtf8Text()
            {
                if (Windows.OpenClipboard(HWND.NULL).Value == 0)
                    return null;

                try
                {
                    byte[]? data = null;

                    HANDLE memory = Windows.GetClipboardData(CF.CF_TEXT);
                    if (memory.Value == null)
                        return null;

                    {
                        byte* ptr = (byte*)Windows.GlobalLock(new HGLOBAL(memory));

                        data = new byte[Windows.GlobalSize(new HGLOBAL(memory))];
                        fixed (byte* localPtr = data)
                        {
                            NativeMemory.Copy(ptr, localPtr, (nuint)data.LongLength);
                        }

                        Windows.GlobalUnlock(new HGLOBAL(memory));
                    }

                    return data;
                }
                finally
                {
                    Windows.CloseClipboard();
                }

                return null;
            }

            internal static bool HasData()
            {
                if (Windows.OpenClipboard(HWND.NULL).Value == 0)
                    return false;

                try
                {
                    return Windows.GetClipboardData(CF.CF_TEXT).Value != null;
                }
                finally
                {
                    Windows.CloseClipboard();
                }
            }
        }
    }
}
