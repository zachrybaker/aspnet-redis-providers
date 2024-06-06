//
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
//

using Microsoft.Web.RedisSessionStateProvider;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Runtime.CompilerServices;
using System.Web.Configuration;
using System.Web.Hosting;
using static Microsoft.Web.Redis.ProviderConfigurationExtension;

namespace Microsoft.Web.Redis
{
    internal class SessionStateProviderConfiguration : IProviderConfiguration
    {
        public TimeSpan RequestTimeout { get; set; }
        public TimeSpan SessionTimeout { get; set; }
        public int Port { get; set; }
        public string Host { get; set; }
        public string AccessKey { get; set; }
        public TimeSpan RetryTimeout { get; set; }
        public bool ThrowOnError { get; set; }
        public bool UseSsl { get; set; }
        public int DatabaseId { get; set; }
        public string ApplicationName { get; set; }
        public int ConnectionTimeoutInMilliSec { get; set; }
        public int OperationTimeoutInMilliSec { get; set; }
        public string ConnectionString { get; set; }
        public ISessionStateSerializer SessionStateSerializer { get; set; } = new DefaultSessionStateSerializer();
        public string SerializationSuffixForKeys { get; set; } = "";

        internal SessionStateProviderConfiguration(NameValueCollection config)
        {
            GetIProviderConfiguration(config, this);

            var assemblyQualifiedClassName = GetStringSettings(config, "redisSerializerType", null);

            if (!string.IsNullOrEmpty(assemblyQualifiedClassName))
            {
                try
                {
                    var serializer = Activator.CreateInstance(Type.GetType(assemblyQualifiedClassName));
                    SessionStateSerializer = (ISessionStateSerializer)serializer;
                    SerializationSuffixForKeys = $"_{SessionStateSerializer.GetType().Name}";
                }
                catch (Exception e)
                {
                    throw new TypeLoadException($"Could not activate Session Serialization Type from assembly qualified class name {assemblyQualifiedClassName}.", e);
                }
            }

            ThrowOnError = GetBoolSettings(config, "throwOnError", true);

            int retryTimeoutInMilliSec = GetIntSettings(config, "retryTimeoutInMilliseconds", 5000);
            RetryTimeout = TimeSpan.FromMilliseconds(retryTimeoutInMilliSec);

            // Get request timeout from config
            HttpRuntimeSection httpRuntimeSection = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
            RequestTimeout = httpRuntimeSection.ExecutionTimeout;

            // Get session timeout from config
            SessionStateSection sessionStateSection = WebConfigurationManager.GetSection("system.web/sessionState") as SessionStateSection;
            SessionTimeout = sessionStateSection.Timeout;

            LogUtility.LogInfo($"Host: {Host}, Port: {Port}, ThrowOnError: {ThrowOnError}, UseSsl: {UseSsl}, RetryTimeout: {RetryTimeout}, DatabaseId: {DatabaseId}, ApplicationName: {ApplicationName}, RequestTimeout: {RequestTimeout}, SessionTimeout: {SessionTimeout}");
        }
    }
}