using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Common
{
    public static class FileUtility
    {
        public static FileStream TryWaitOpen(string fullPath, FileMode mode, FileAccess access, FileShare share, int maxTries = 10, int timeoutMs = 250)
        {
            for (int i = 0; i < maxTries; i++)
            {
                FileStream? fs = null;
                try
                {
                    fs = File.Open(fullPath, mode, access, share);
                    return fs;
                }
                catch (Exception)
                {
                    fs?.Dispose();
                    Thread.Sleep(timeoutMs);
                }
            }

            return File.Open(fullPath, mode, access, share);
        }

        public static string FormatSize(long size, string? format = "G", IFormatProvider? provider = null)
        {
            if (size < 1025)
                return $"{size.ToString(format, provider)}b";

            double dbSize = size / 1024.0;
            for (int i = 0; i < s_fileSizes.Length; i++)
            {
                if (dbSize <= 1024.0)
                    return $"{dbSize.ToString(format, provider)}{s_fileSizes[i]}";
                dbSize /= 1024.0;
            }

            return $"{dbSize.ToString(format, provider)}pb";
        }

        private static readonly string[] s_fileSizes = ["kb", "mb", "gb", "tb", "pb"];
    }
}
