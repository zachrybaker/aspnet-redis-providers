//
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
//

using Microsoft.Web.Redis;
using Microsoft.Web.Redis.FunctionalTests;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.SessionState;
using Xunit;

namespace Microsoft.Web.RedisSessionStateProvider.Functional.Tests
{
    public class ISessionDataSerializerFunctionalTests
    {
        [Fact]
        public void TestCustomISessionDataSerializer()
        {
            var serializer = new SessionStateSerializerExample();
            var session = new SessionStateItemCollection();
            session["key1"] = "value1";
            session["key2"] = "value2";
            var serialized = serializer.Serialize(session);
            var deserialized = serializer.Deserialize(serialized);
            Assert.Equal(2, deserialized.Count);
            Assert.Equal("value1", deserialized["key1"]);
            Assert.Equal("value2", deserialized["key2"]);
        }

        [Fact]
        public async Task TestCustomISessionDataSerializerInProvider()
        {
            using (RedisServer redisServer = new RedisServer())
            {
                RedisSessionStateConnectionWrapper.sharedConnection = null;
                string sessionId = Guid.NewGuid().ToString();

                var ssp = new Redis.RedisSessionStateProvider();
                var config = new NameValueCollection()
                {
                    { "sessionSerializationNamespaceAndType", typeof(SessionStateSerializerExample).FullName },
                    { "sessionSerializationTypeAssembly", typeof(SessionStateSerializerExample).Assembly.FullName },
                    { "ssl", "false" },
                };
                ssp.Initialize("ssp", config);

                var session = new SessionStateItemCollection();
                session["key1"] = "value1";
                session["key2"] = "value2";
                SessionStateStoreData storeData = new SessionStateStoreData(session, null, 10);

                await ssp.SetAndReleaseItemExclusiveAsync(null, sessionId, storeData, null, true, CancellationToken.None);
                var result = await ssp.GetItemAsync(null, sessionId, CancellationToken.None);

                SessionStateStoreData deserialized = result.Item;
                Assert.Equal(2, deserialized.Items.Count);
                Assert.Equal("value1", deserialized.Items["key1"]);
                Assert.Equal("value2", deserialized.Items["key2"]);

                RedisSessionStateConnectionWrapper.sharedConnection = null;
            }
        }
    }
}
internal class SessionStateSerializerExample : ISessionStateSerializer
{
    public byte[] Serialize(SessionStateItemCollection data)
    {
        // example custom serialization 
        var serializedData = new List<byte>();
        foreach (var key in data.Keys)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key.ToString());
            serializedData.AddRange(BitConverter.GetBytes(keyBytes.Length));
            serializedData.AddRange(keyBytes);

            var value = data[(string)key];
            var valueBytes = Encoding.UTF8.GetBytes(value.ToString());
            serializedData.AddRange(BitConverter.GetBytes(valueBytes.Length));
            serializedData.AddRange(valueBytes);
        }
        return serializedData.ToArray();
    }

    public SessionStateItemCollection Deserialize(byte[] data)
    {
        // example custom deserialization
        var deserializedData = new SessionStateItemCollection();
        int index = 0;
        while (index < data.Length)
        {
            var keyLength = BitConverter.ToInt32(data, index);
            index += sizeof(int);
            var key = Encoding.UTF8.GetString(data, index, keyLength);
            index += keyLength;

            var valueLength = BitConverter.ToInt32(data, index);
            index += sizeof(int);
            var value = Encoding.UTF8.GetString(data, index, valueLength);
            index += valueLength;

            deserializedData[key] = value;
        }
        return deserializedData;
    }
}