using CommunityToolkit.HighPerformance;
using ExtConsole.Communication.Messages;
using ExtConsole.Communication.Net;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Encoders;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ExtConsole.Communication.Serialization
{
    public sealed class MessageSerializer
    {
        private MessageWriter _writer;

        private Dictionary<IDataClient, SendBufferData> _clientBuffers;
        private List<byte> _sendBuffer;

        private Lock _sendLock;

        internal MessageSerializer()
        {
            _writer = new MessageWriter();

            _clientBuffers = new Dictionary<IDataClient, SendBufferData>();
            _sendBuffer = new List<byte>(1024);

            _sendLock = new Lock();
        }

        internal int SendProcessedMessages()
        {
            int traffic = 0;
            lock (_sendLock)
            {
                foreach (IDataClient client in _clientBuffers.Keys)
                {
                    ref SendBufferData bufferData = ref CollectionsMarshal.GetValueRefOrNullRef(_clientBuffers, client);
                    Debug.Assert(!Unsafe.IsNullRef(ref bufferData));

                    if (bufferData.Offset == 0)
                        continue;
                    if (!client.IsConnected)
                        continue;

                    int worstCaseOutputSize = LZ4Codec.MaximumOutputSize(1021);
                    using RentedArray<byte> tempBuffer = RentedArray<byte>.Rent(worstCaseOutputSize + 1);

                    _sendBuffer.Clear();

                    unsafe
                    {
                        fixed (byte* ptr1 = bufferData.Buffer)
                        fixed (byte* ptr2 = tempBuffer.Span)
                        {
                            int lengthLeft = bufferData.Offset;

                            do
                            {
                                int offset = bufferData.Offset - lengthLeft;
                                int size = Math.Min(lengthLeft, 1021);

                                int writtenSize = LZ4Codec.Encode(ptr1 + offset, size, ptr2 + Unsafe.SizeOf<CompressionMetadata>(), worstCaseOutputSize);

                                if (writtenSize > size)
                                {
                                    Unsafe.WriteUnaligned(ref tempBuffer.Span.DangerousGetReference(), new CompressionMetadata(false, (ushort)size));
                                    bufferData.Buffer.AsSpan(offset, size).CopyTo(tempBuffer.Span.Slice(Unsafe.SizeOf<CompressionMetadata>()));
                                    _sendBuffer.AddRange(tempBuffer.Span.Slice(0, size + Unsafe.SizeOf<CompressionMetadata>()));
                                }
                                else
                                {
                                    Unsafe.WriteUnaligned(ref tempBuffer.Span.DangerousGetReference(), new CompressionMetadata(true, (ushort)writtenSize));
                                    _sendBuffer.AddRange(tempBuffer.Span.Slice(0, writtenSize + Unsafe.SizeOf<CompressionMetadata>()));
                                }
                            } while ((lengthLeft -= 1021) > 0);
                        }
                    }

                    if (_sendBuffer.Count > 0)
                    {
                        try
                        {
                            client.SendRaw(_sendBuffer.AsSpan());
                            traffic += _sendBuffer.Count;
                        }
                        catch (SocketException ex)
                        {
                            CommLog.Message.Error(ex, "Failed to send message bytes due to socket related issue");
                        }
                    }
                }

                _clientBuffers.Clear();
            }

            return traffic;
        }

        internal void Send<T>(IDataClient client, T message) where T : IMessage
        {
            _writer.ResetForNewWrite();
            message.Serialize(_writer);

            if (_writer.Bytes.IsEmpty)
                return;

            lock (_sendLock)
            {
                ref SendBufferData bufferData = ref CollectionsMarshal.GetValueRefOrAddDefault(_clientBuffers, client, out bool exists);
                if (!exists)
                    bufferData = new SendBufferData { Buffer = new byte[1024], Offset = 0 };

                if (bufferData.Offset + Unsafe.SizeOf<MessageHeader>() + _writer.Bytes.Length >= bufferData.Buffer.Length)
                {
                    Array.Resize(ref bufferData.Buffer, bufferData.Buffer.Length * 2);
                }

                MessageHeader header = new MessageHeader
                {
                    Header = MessageHeader.ConHeader,
                    MessageSize = (ushort)_writer.Bytes.Length,
                    Id = message.Id
                };

                Unsafe.WriteUnaligned(ref bufferData.Buffer[bufferData.Offset], header);
                bufferData.Offset += Unsafe.SizeOf<MessageHeader>();

                _writer.Bytes.CopyTo(bufferData.Buffer.AsSpan(bufferData.Offset));
                bufferData.Offset += _writer.Bytes.Length;
            }
        }

        private struct SendBufferData
        {
            public byte[] Buffer;
            public int Offset;
        }
    }
}
