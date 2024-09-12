using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Frends.RabbitMQ.Publish.Definitions;
using RabbitMQ.Client;
using System.Runtime.Caching;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

namespace Frends.RabbitMQ.Publish;

/// <summary>
/// RabbitMQ publish task.
/// </summary>
public class RabbitMQ
{
    internal static readonly ObjectCache RabbitMQConnectionCache = MemoryCache.Default;

    [ExcludeFromCodeCoverage]
    private static void RemovedCallback(CacheEntryRemovedArguments arg)
    {
        if (arg.RemovedReason != CacheEntryRemovedReason.Removed)
        {
            if (arg.CacheItem.Value is IDisposable item)
                item.Dispose();
        }
    }

    /// <summary>
    /// Publish message to RabbitMQ queue in UTF8 or byte array format.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends.RabbitMQ.Publish)
    /// </summary>
    /// <param name="input">Input parameters</param>
    /// <param name="connection">Connection parameters.</param>
    /// <param name="cancellationToken">CancellationToken given by Frends to terminate the Task.</param>
    /// <returns>Object { string DataFormat, string DataString, byte[] DataByteArray, Dictionary&lt;string, string&gt; Headers }</returns>
    public static Result Publish([PropertyTab] Input input, [PropertyTab] Connection connection, CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory();

        switch (connection.AuthenticationMethod)
        {
            case AuthenticationMethod.URI:
                factory.Uri = new Uri(connection.Host);
                break;
            case AuthenticationMethod.Host:
                if (!string.IsNullOrWhiteSpace(connection.Username) || !string.IsNullOrWhiteSpace(connection.Password))
                {
                    factory.UserName = connection.Username;
                    factory.Password = connection.Password;
                }
                factory.HostName = connection.Host;

                if (connection.Port != 0) factory.Port = connection.Port;

                break;
        }

        if (connection.Timeout != 0)
            factory.RequestedConnectionTimeout = TimeSpan.FromSeconds(connection.Timeout);

        var channel = GetRabbitMQChannel(connection, factory, cancellationToken);

        var dataType = input.InputType.Equals(InputType.ByteArray) ? "ByteArray" : "String";
        var data = input.InputType.Equals(InputType.ByteArray) ? input.DataByteArray : Encoding.UTF8.GetBytes(input.DataString);

        if (data.Length == 0)
            throw new ArgumentException("Publish: Message data is missing.");

        if (connection.Create)
        {
            // Create args dictionary for quorum queue arguments
            var args = new Dictionary<string, object>
            {
                { "x-queue-type", "quorum" }
            };

            channel.QueueDeclare(queue: connection.QueueName,
                durable: connection.Durable,
                exclusive: false,
                autoDelete: connection.AutoDelete,
                arguments: connection.Quorum ? args : null);

            if (!string.IsNullOrEmpty(connection.ExchangeName))
            {
                channel.QueueBind(queue: connection.QueueName,
                    exchange: connection.ExchangeName,
                    routingKey: connection.RoutingKey,
                    arguments: null);
            }
        }

        var basicProperties = channel.CreateBasicProperties();
        basicProperties.Persistent = connection.Durable;
        AddHeadersToBasicProperties(basicProperties, input.Headers);

        var headers = new Dictionary<string, string>();

        if (basicProperties.Headers != null)
            foreach (var head in basicProperties.Headers)
                headers.Add(head.Key.ToString(), head.Value.ToString());

        channel.BasicPublish(exchange: connection.ExchangeName,
            routingKey: connection.RoutingKey,
            basicProperties: basicProperties,
            body: data);

        return new Result(dataType,
            !string.IsNullOrEmpty(input.DataString) ? input.DataString : Encoding.UTF8.GetString(input.DataByteArray),
            input.DataByteArray ?? Encoding.UTF8.GetBytes(input.DataString),
            headers);
    }

    private static void AddHeadersToBasicProperties(IBasicProperties basicProperties, Header[] headers)
    {
        if (headers == null) return;

        var messageHeaders = new Dictionary<string, object>();

        headers.ToList().ForEach(header =>
        {
            switch (header.Name.ToUpper())
            {
                case "APPID":
                case "HEADER_APPID":
                case "HEADER.APPID":
                    basicProperties.AppId = header.Value;
                    break;

                case "CLUSTERID":
                case "HEADER_CLUSTERID":
                case "HEADER.CLUSTERID":
                    basicProperties.ClusterId = header.Value;
                    break;

                case "CONTENTENCODING":
                case "HEADER_CONTENTENCODING":
                case "HEADER.CONTENTENCODING":
                    basicProperties.ContentEncoding = header.Value;
                    break;

                case "CONTENTTYPE":
                case "HEADER_CONTENTTYPE":
                case "HEADER.CONTENTTYPE":
                    basicProperties.ContentType = header.Value;
                    break;

                case "CORRELATIONID":
                case "HEADER_CORRELATIONID":
                case "HEADER.CORRELATIONID":
                    basicProperties.CorrelationId = header.Value;
                    break;

                case "EXPIRATION":
                case "HEADER_EXPIRATION":
                case "HEADER.EXPIRATION":
                    basicProperties.Expiration = header.Value;
                    break;

                case "MESSAGEID":
                case "HEADER_MESSAGEID":
                case "HEADER.MESSAGEID":
                    basicProperties.MessageId = header.Value;
                    break;

                default:
                    messageHeaders.Add(header.Name, header.Value);
                    break;
            }
        });

        if (messageHeaders.Any())
            basicProperties.Headers = messageHeaders;
    }

    private static IModel GetRabbitMQChannel(Connection connection, ConnectionFactory factory, CancellationToken cancellationToken)
    {
        var conn = GetRabbitMQConnection(connection, factory, cancellationToken);

        var retryCount = 0;
        while (retryCount < 5)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var channel = new RabbitMQChannel() { AMQPModel = conn.CreateModel() };
                return channel.AMQPModel;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The connection cannot support any more channels."))
                {
                    conn = GetRabbitMQConnection(connection, factory, cancellationToken, true);
                    continue;
                }
                // Log the exception here
                // If the maximum number of retries has been reached, rethrow the exception
                if (++retryCount >= 5)
                    throw new Exception($"Getting Exception : {ex.Message} after {retryCount} retries.", ex);

                // Wait for a certain period of time before retrying
                Thread.Sleep(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
            }
        }

        return null;
    }

    private static IConnection GetRabbitMQConnection(Connection connection, ConnectionFactory factory, CancellationToken cancellationToken, bool forceCreate = false)
    {
        var cacheKey = GetCacheKey(connection);

        if (!forceCreate)
        {
            var key = GetCacheKeyFromMemoryCache(cacheKey);
            if (key != null)
            {
                if (RabbitMQConnectionCache.Get(GetCacheKeyFromMemoryCache(cacheKey)) is RabbitMQConnection conn && conn.AMQPConnection.IsOpen)
                    return conn.AMQPConnection;
            }

        }

        var retryCount = 0;
        while (retryCount < 5)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var rabbitMQConnection = new RabbitMQConnection { AMQPConnection = factory.CreateConnection() };
                RabbitMQConnectionCache.Add($"{Guid.NewGuid()}_{cacheKey}", rabbitMQConnection, new CacheItemPolicy() { RemovedCallback = RemovedCallback, SlidingExpiration = TimeSpan.FromSeconds(connection.ConnectionExpirationSeconds) });
                return rabbitMQConnection.AMQPConnection;
            }
            catch (Exception ex)
            {
                // Log the exception here
                // If the maximum number of retries has been reached, rethrow the exception
                if (++retryCount >= 5)
                    throw new Exception($"Operation failed: {ex.Message} after {retryCount} retries.", ex);

                // Wait for a certain period of time before retrying
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        return null;
    }

    private static string GetCacheKey(Connection connection)
    {
        var key = $"{connection.Host}:";
        if (connection.AuthenticationMethod == AuthenticationMethod.Host)
        {
            key += $"{connection.Username}:{connection.Password}:{connection.Port}:";
        }

        key += $"{connection.QueueName}:{connection.ExchangeName}:{connection.RoutingKey}:" +
            $"{connection.Create}:{connection.AutoDelete}:{connection.Durable}:{connection.Quorum}:{connection.Timeout}";

        return key;
    }

    private static string GetCacheKeyFromMemoryCache(string cacheKey)
    {
        try
        {
            return RabbitMQConnectionCache.ToList().Where(e => e.Key.Split("_")[0] == cacheKey).Select(e => e.Key).FirstOrDefault();
        }
        catch { return null; }
    }
}