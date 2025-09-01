using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Serialization
{
    public sealed class SDFWriter : IDisposable
    {
        private IBufferWriter<char> _outputBuffer;
        private ArrayPoolBufferWriter<char> _intermediateBuffer;

        private int _recTreeDepth = 0;
        private Stack<VariableType> _recVarStack;

        private int _treeDepth = 0;
        private Stack<VariableType> _varStack;

        internal SDFWriter(IBufferWriter<char> outputBuffer)
        {
            _outputBuffer = outputBuffer;
            _intermediateBuffer = new ArrayPoolBufferWriter<char>(64);

            _recTreeDepth = 0;
            _recVarStack = new Stack<VariableType>();

            _treeDepth = 0;
            _varStack = new Stack<VariableType>();
        }

        public void Dispose()
        {
            _intermediateBuffer.Dispose();
        }

        public void Flush()
        {
            if (_intermediateBuffer.WrittenCount > 0)
            {
                _outputBuffer.Write(_intermediateBuffer.WrittenSpan);
                _intermediateBuffer.Clear();
            }

            _recTreeDepth = _treeDepth;
            _recVarStack.Clear();

            foreach (VariableType vt in _varStack)
                _recVarStack.Push(vt);
        }

        public void Restore()
        {
            _intermediateBuffer.Clear();

            _treeDepth = _recTreeDepth;
            _varStack.Clear();

            foreach (VariableType vt in _recVarStack)
                _varStack.Push(vt);
        }

        private void AddTreePadding()
        {
            if (_varStack.Count == 0)
            {
                for (int i = 0; i < _treeDepth; i++)
                    _intermediateBuffer.Write("    ");
            }
            else
            {
                if (_varStack.Peek() == VariableType.Array)
                {
                    _intermediateBuffer.Write(", ");
                }
            }
        }

        public void BeginObject(string name)
        {
            AddTreePadding();

            _intermediateBuffer.Write(name);
            _intermediateBuffer.Write("{\r\n");

            _treeDepth++;
        }
        public void EndObject()
        {
            if (_treeDepth == 0)
                return;
            _treeDepth--;

            AddTreePadding();

            _intermediateBuffer.Write("}\r\n");
        }

        public void BeginArray()
        {
            if (_varStack.Peek() != VariableType.Property)
                return;

            _treeDepth++;
            _varStack.Push(VariableType.Array);

            _intermediateBuffer.Write('[');
        }
        public void EndArray()
        {
            if (_varStack.Peek() != VariableType.Array)
                return;

            _treeDepth--;
            _varStack.Pop();

            AddTreePadding();

            if (_varStack.Peek() == VariableType.Property)
                _varStack.Pop();

            _intermediateBuffer.Write("]\r\n");
        }

        public void WriteProperty(string name)
        {
            if (_varStack.Count > 0)
                return;

            AddTreePadding();

            _varStack.Push(VariableType.Property);

            _intermediateBuffer.Write(name);
            _intermediateBuffer.Write(" = ");
        }

        private void WriteGeneric(string value)
        {
            if (_varStack.Count == 0)
                return;

            AddTreePadding();

            if (_varStack.Peek() == VariableType.Property)
                _varStack.Pop();

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '"')
                    _intermediateBuffer.Write('\\');
                _intermediateBuffer.Write(c);
            }
        }

        public void Write<T>(T value) where T : struct
        {
            //TODO: some datatype serializer overload
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(string value) => WriteGeneric(@$"""{value}""");
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(float value) => WriteGeneric(value.ToString(CultureInfo.InvariantCulture));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(double value) => WriteGeneric(value.ToString(CultureInfo.InvariantCulture));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(sbyte value) => WriteGeneric(value.ToString(CultureInfo.InvariantCulture));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(short value) => WriteGeneric(value.ToString(CultureInfo.InvariantCulture));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value) => WriteGeneric(value.ToString(CultureInfo.InvariantCulture));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(long value) => WriteGeneric(value.ToString(CultureInfo.InvariantCulture));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value) => WriteGeneric(value.ToString(CultureInfo.InvariantCulture));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ushort value) => WriteGeneric(value.ToString(CultureInfo.InvariantCulture));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value) => WriteGeneric(value.ToString(CultureInfo.InvariantCulture));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ulong value) => WriteGeneric(value.ToString(CultureInfo.InvariantCulture));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(string property, float value)     { WriteProperty(property); WriteGeneric($@"""{value}"""); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(string property, double value)    { WriteProperty(property); WriteGeneric(value.ToString(CultureInfo.InvariantCulture)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(string property, sbyte value)     { WriteProperty(property); WriteGeneric(value.ToString(CultureInfo.InvariantCulture)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(string property, short value)     { WriteProperty(property); WriteGeneric(value.ToString(CultureInfo.InvariantCulture)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(string property, int value)       { WriteProperty(property); WriteGeneric(value.ToString(CultureInfo.InvariantCulture)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(string property, long value)      { WriteProperty(property); WriteGeneric(value.ToString(CultureInfo.InvariantCulture)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(string property, byte value)      { WriteProperty(property); WriteGeneric(value.ToString(CultureInfo.InvariantCulture)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(string property, ushort value)    { WriteProperty(property); WriteGeneric(value.ToString(CultureInfo.InvariantCulture)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(string property, uint value)      { WriteProperty(property); WriteGeneric(value.ToString(CultureInfo.InvariantCulture)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(string property, ulong value) { WriteProperty(property); WriteGeneric(value.ToString(CultureInfo.InvariantCulture)); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(string property, T value) where T : struct { WriteProperty(property); Write(value); }

        private enum ActiveState : byte
        {
            None = 0,
            HasActiveObject,
            HasActiveArray
        }

        private enum VariableType : byte
        {
            Property,
            Array,
        }
    }
}
