using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Primary.Common;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;

namespace Primary.Console
{
    public sealed class ConsoleManager
    {
        internal ConsoleManager()
        {

        }

        private void RegisterObject<T>() where T : class
        {
            Type type = typeof(T);

            string localNamespace = type.GetCustomAttribute<ConsoleNameAttribute>()?.FullName ?? string.Empty;

        }

        private void UnregisterObject<T>() where T : class
        {

        }

        public static void Register<T>(T obj) where T : class
        {

        }

        public static void Unregister<T>(T obj) where T : class
        {

        }
    }
}
