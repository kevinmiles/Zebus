﻿using System.IO;
using Abc.Zebus.Serialization.Protobuf;
using Abc.Zebus.Testing.Extensions;
using Abc.Zebus.Testing.Measurements;
using Abc.Zebus.Tests.Messages;
using Abc.Zebus.Transport;
using NUnit.Framework;
using ProtoBuf;

namespace Abc.Zebus.Tests.Transport
{
    [TestFixture]
    public class TransportMessageReaderTests
    {
        [Test]
        public void should_read_message()
        {
            var transportMessage = TestDataBuilder.CreateTransportMessage<FakeCommand>();

            var outputStream = new CodedOutputStream();
            outputStream.WriteTransportMessage(transportMessage);

            var inputStream = new CodedInputStream(outputStream.Buffer, 0, outputStream.Position);
            var deserialized = inputStream.ReadTransportMessage();

            deserialized.Id.ShouldEqual(transportMessage.Id);
            deserialized.MessageTypeId.ShouldEqual(transportMessage.MessageTypeId);
            deserialized.GetContentBytes().ShouldEqual(transportMessage.GetContentBytes());
            deserialized.Originator.ShouldEqualDeeply(transportMessage.Originator);
            deserialized.Environment.ShouldEqual(transportMessage.Environment);
            deserialized.WasPersisted.ShouldEqual(transportMessage.WasPersisted);
        }

        [Test]
        public void should_read_message_from_protobuf()
        {
            var transportMessage = TestDataBuilder.CreateTransportMessage<FakeCommand>();

            var stream = new MemoryStream();
            Serializer.Serialize(stream, transportMessage);

            var inputStream = new CodedInputStream(stream.GetBuffer(), 0, (int)stream.Length);
            var deserialized = inputStream.ReadTransportMessage();

            deserialized.Id.ShouldEqual(transportMessage.Id);
            deserialized.MessageTypeId.ShouldEqual(transportMessage.MessageTypeId);
            deserialized.GetContentBytes().ShouldEqual(transportMessage.GetContentBytes());
            deserialized.Originator.ShouldEqualDeeply(transportMessage.Originator);
            deserialized.Environment.ShouldEqual(transportMessage.Environment);
            deserialized.WasPersisted.ShouldEqual(transportMessage.WasPersisted);
        }

        [Test, Ignore("Manual test")]
        public void MeasureReadPerformance()
        {
            var transportMessage = TestDataBuilder.CreateTransportMessage<FakeCommand>();

            var outputStream = new CodedOutputStream();
            outputStream.WriteTransportMessage(transportMessage);

            var inputStream = new CodedInputStream(outputStream.Buffer, 0, outputStream.Position);
            inputStream.ReadTransportMessage();

            const int count = 1000 * 1000 * 1000;
            using (Measure.Throughput(count))
            {
                for (var i = 0; i < count; i++)
                {
                    inputStream.ReadTransportMessage();
                }
            }
        }
    }
}