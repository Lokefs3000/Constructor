using System.IO.Pipes;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using Primary.Threading;
using VoxelizationDemo.Core;

// WHY ARE THERE 2 DIFFERENT 'ThreadState' ENUMS IN 'System.Runtime' and 'System.Diagnostics' ??!?!!?!
using Process = System.Diagnostics.Process;

namespace VoxelizationDemo.Editor.Runner
{
    internal sealed class RuntimeManager : IDisposable
    {
        private readonly Process _thisProcess;

        private Thread? _runtimeThread;

        private RuntimeLoadContext? _alc;
        private AnonymousPipeServerStream? _inStream;
        private AnonymousPipeServerStream? _outStream;

        private bool _disposedValue;

        internal RuntimeManager()
        {
            _thisProcess = Process.GetCurrentProcess();

            _runtimeThread = null;

            _alc = null;
            _inStream = null;
            _outStream = null;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    StopRuntime();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void StartRuntime()
        {
            VoxelRuntime runtime = VoxelRuntime.Instance;
            if (runtime.State == RuntimeState.Running)
                return;

            runtime.ChangeRuntimeState(RuntimeState.Running);

            Guard.IsNull(_runtimeThread);
            Guard.IsNull(_alc);
            Guard.IsNull(_inStream);
            Guard.IsNull(_outStream);

            _inStream = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
            _outStream = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);

            string[] args = [
                $"--runtime", _thisProcess.Id.ToString(),
                "--runtime-pipe-in", _outStream.GetClientHandleAsString(),
                "--runtime-pipe-out", _inStream.GetClientHandleAsString()
                ];

            _alc = new RuntimeLoadContext(typeof(RuntimeManager).Assembly.Location);

            Assembly asm = _alc.LoadFromAssemblyPath(typeof(RuntimeManager).Assembly.Location);
            Type programType = asm.GetType(typeof(Program).FullName!)!;
            MethodInfo main = programType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic)!;
            Action<string[]> invoke = main.CreateDelegate<Action<string[]>>();

            _runtimeThread = new Thread((x) =>
            {
                try
                {
                    ((KeyValuePair<Action<string[]>, string[]>)x!).Key(((KeyValuePair<Action<string[]>, string[]>)x!).Value);
                }
                finally
                {
                    ThreadHelper.ExecuteOnMainThread(StopRuntime);
                }
            });

            _runtimeThread.Start(new KeyValuePair<Action<string[]>, string[]>(invoke, args));

            int readValue = 0;
            int bytesRead = 0;

            // This is a possible infinite loop
            while (true)
            {
                int amountRead = _inStream.Read(MemoryMarshal.Cast<int, byte>(new Span<int>(ref readValue))[bytesRead..]);
                bytesRead += amountRead;

                if (bytesRead >= Unsafe.SizeOf<int>())
                {
                    if (readValue == _thisProcess.Id)
                    {
                        // Success!

                        _runtimeThread.Name = "Runtime-Main";
                        break;
                    }
                    else
                    {
                        AppLog.Editor.Error("Incorrect response from process {process} (expected: {expected})", readValue, _thisProcess.Id);
                        StopRuntime();
                    }
                }
                else if (_runtimeThread.ThreadState != ThreadState.Running)
                {
                    AppLog.Editor.Error("Failed to initialize runtime");
                    StopRuntime();
                    break;
                }
            }
        }

        internal void StopRuntime()
        {
            VoxelRuntime runtime = VoxelRuntime.Instance;
            if (runtime.State == RuntimeState.Editor)
                return;

            runtime.ChangeRuntimeState(RuntimeState.Editor);

            if (_alc != null)
            {
                _alc.Unload();
                _alc = null;
            }

            _inStream?.Dispose();
            _inStream = null;

            _outStream?.Dispose();
            _outStream = null;
        }
    }
}
