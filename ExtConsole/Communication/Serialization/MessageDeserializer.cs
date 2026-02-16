using CommunityToolkit.HighPerformance;
using ExtConsole.Communication.Messages;
using K4os.Compression.LZ4;
using Primary.Common;
using Primary.Pooling;
using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ExtConsole.Communication.Serialization
{
    public sealed class MessageDeserializer
    {
        private MessageHeader? _currentHeader;

        private List<byte> _recievedBuffer;

        private byte[] _byteBuffer;
        private int _readDataSize;

        private byte[] _decompressionBuffer;

        private ObjectPool<MessageReader> _readerPool;
        private ConcurrentQueue<QueuedMessage> _queuedMessages;

        private Lock _readerPoolLock;

        internal MessageDeserializer()
        {
            _currentHeader = null;

            _recievedBuffer = new List<byte>();

            _byteBuffer = Array.Empty<byte>();
            _readDataSize = 0;

            _decompressionBuffer = new byte[LZ4Codec.MaximumOutputSize(1021)];

            _readerPool = new ObjectPool<MessageReader>(new MessageReader.Policy());
            _queuedMessages = new ConcurrentQueue<QueuedMessage>();

            _readerPoolLock = new Lock();
        }

        internal void ProcessBytes(ReadOnlySpan<byte> bytes)
        {
            _recievedBuffer.AddRange(bytes);
        }

        private void ProcessRecievedBytes()
        {
            ReadOnlySpan<byte> bytes = _recievedBuffer.AsSpan();
            while (bytes.Length > 0)
            {
                CompressionMetadata metadata = Unsafe.ReadUnaligned<CompressionMetadata>(ref bytes.DangerousGetReference());
                CommLog.Message.Information("{md}", metadata);

                ReadOnlySpan<byte> rawBytes = ReadOnlySpan<byte>.Empty;
                if (metadata.IsCompressed)
                {
                    unsafe
                    {
                        fixed (byte* ptr1 = bytes)
                        fixed (byte* ptr2 = _decompressionBuffer)
                        {
                            int writtenSize = LZ4Codec.Decode(ptr1 + Unsafe.SizeOf<CompressionMetadata>(), metadata.DataLength, ptr2, _decompressionBuffer.Length);
                            Debug.Assert(writtenSize != -1);

                            if (writtenSize == -1)
                            {
                                CommLog.Message.Error("Malformed compressed packet!");

                                bytes = bytes.Slice(Unsafe.SizeOf<CompressionMetadata>() + metadata.DataLength);
                                continue;
                            }

                            rawBytes = _decompressionBuffer.AsSpan(0, writtenSize);
                        }
                    }
                }
                else
                    rawBytes = bytes.Slice(Unsafe.SizeOf<CompressionMetadata>(), metadata.DataLength);

                ProcessRawBytes(rawBytes);
                bytes = bytes.Slice(Unsafe.SizeOf<CompressionMetadata>() + metadata.DataLength);
            }
        }

        private void ProcessRawBytes(ReadOnlySpan<byte> bytes)
        {
            if (_currentHeader.HasValue)
            {
                MessageHeader header = _currentHeader.Value;
                if (_readDataSize + bytes.Length >= header.MessageSize)
                {
                    bytes.Slice(0, Math.Min(_byteBuffer.Length - _readDataSize, bytes.Length)).CopyTo(_byteBuffer.AsSpan(_readDataSize));
                    SendAssembledMessage(header.Id, _byteBuffer.AsSpan(0, header.MessageSize));

                    bytes = bytes.Slice(header.MessageSize - _readDataSize);

                    _currentHeader = null;
                    _readDataSize = 0;
                }
                else
                {
                    bytes.CopyTo(_byteBuffer.AsSpan(_readDataSize));
                    _readDataSize += bytes.Length;

                    return;
                }
            }

            while (!bytes.IsEmpty)
            {
                if (bytes.Length < Unsafe.SizeOf<MessageHeader>())
                {
                    CommLog.Message.Warning("Not enough data provided for message header: {b}", FileUtility.FormatSize(bytes.Length));
                    return;
                }

                MessageHeader header = Unsafe.ReadUnaligned<MessageHeader>(ref bytes.DangerousGetReference());
                if (header.Header != MessageHeader.ConHeader)
                {
                    CommLog.Message.Error("Invalid header present in stream: {h:x8} (utf8: {s})", header.Header, Encoding.UTF8.GetString(MemoryMarshal.Cast<uint, byte>(new ReadOnlySpan<uint>(ref header.Header))));
                    return;
                }

                if (bytes.Length - Unsafe.SizeOf<MessageHeader>() >= header.MessageSize)
                {
                    _currentHeader = null;

                    SendAssembledMessage(header.Id, bytes.Slice(Unsafe.SizeOf<MessageHeader>(), header.MessageSize));
                    bytes = bytes.Slice(Unsafe.SizeOf<MessageHeader>() + header.MessageSize);
                }
                else
                {
                    _currentHeader = header;

                    int roundedNextSize = (int)BitOperations.RoundUpToPowerOf2(header.MessageSize);
                    if (_byteBuffer.Length < roundedNextSize)
                    {
                        _byteBuffer = new byte[roundedNextSize];
                    }

                    bytes.Slice(Unsafe.SizeOf<MessageHeader>()).CopyTo(_byteBuffer);
                    _readDataSize = bytes.Length - Unsafe.SizeOf<MessageHeader>();

                    break; //No more data left
                }
            }
        }

        private void SendAssembledMessage(MessageId id, ReadOnlySpan<byte> bytes)
        {
            lock (_readerPoolLock)
            {
                MessageReader reader = _readerPool.Get();
                reader.RentArray(bytes);

                _queuedMessages.Enqueue(new QueuedMessage(id, reader));
            }
        }

        internal void HandleRecievedData()
        {
            if (_recievedBuffer.Count > 0)
            {
                ProcessRecievedBytes();
                _recievedBuffer.Clear();
            }
        }

        internal bool TryDequeueQueuedMessage([NotNullWhen(true)] out QueuedMessage reader) => _queuedMessages.TryDequeue(out reader);

        internal void ReturnMessageReader(MessageReader reader)
        {
            lock (_readerPoolLock)
            {
                _readerPool.Return(reader);
            }
        }
    }
}
