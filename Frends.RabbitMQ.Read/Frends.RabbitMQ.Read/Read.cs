using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Frends.RabbitMQ.Read.Definitions;
using Frends.RabbitMQ.Read.Helpers;

namespace Frends.RabbitMQ.Read;

/// <summary>
/// RabbitMQ Read task.
/// </summary>
public class RabbitMQ
{
    /// <summary>
    /// Read message(s) from RabbitMQ queue. Message data is byte[] encoded to base64 and UTF8 strings.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends.RabbitMQ.Read)
    /// </summary>
    /// <param name="connection">Connection parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <returns>Object { bool Success, Object { string Data, Dictionary&lt;string, string&gt; Headers, uint MessagesCount, ulong DeliveryTag } MessagesBase64, Object { string Data, Dictionary&lt;string, string&gt; Headers, uint MessagesCount, ulong DeliveryTag } MessageUTF8 }</returns>
    public static async Task<Result> Read([PropertyTab] Connection connection, [PropertyTab] Options options)
    {
        try
        {
            using var connectionHelper = new ConnectionHelper();
            var baseList = new List<Message>();
            var stringList = new List<Message>();

            await Helper.OpenConnectionIfClosed(connectionHelper, connection);

            while (connection.ReadMessageCount-- > 0)
            {
                var rcvMessage = await connectionHelper.AMQPModel.BasicGetAsync(queue: connection.QueueName, autoAck: connection.AckType == AckType.AutoAck);
                if (rcvMessage != null)
                {
                    baseList.Add(new Message
                    {
                        Data = Convert.ToBase64String(rcvMessage.Body.ToArray()),
                        Headers = Helper.GetResponseHeaderDictionary(rcvMessage.BasicProperties),
                        MessagesCount = rcvMessage.MessageCount,
                        DeliveryTag = rcvMessage.DeliveryTag
                    });

                    stringList.Add(new Message
                    {
                        Data = Encoding.UTF8.GetString(rcvMessage.Body.ToArray()),
                        Headers = Helper.GetResponseHeaderDictionary(rcvMessage.BasicProperties),
                        MessagesCount = rcvMessage.MessageCount,
                        DeliveryTag = rcvMessage.DeliveryTag
                    });
                }
                else
                    break;
            }

            // Acking logic:
            // - AutoAck is handled when IChannel.BasicGetAsync() is called with autoAck: true.
            // - NoAck does not send AckMessage.
            // - Other types are handled in AcknowledgeMessage() method
            if (connection.AckType is AckType.AutoAck or AckType.NoAck)
                return new Result(true, baseList, stringList);
            foreach (var message in baseList)
                await AcknowledgeMessage(connection.AckType, message.DeliveryTag, connectionHelper);

            return new Result(true, baseList, stringList);
        }
        catch (Exception e)
        {
            return ErrorHandler.Handle(e, options.ThrowErrorOnFailure, options.ErrorMessageOnFailure);
        }
    }

    private static async Task AcknowledgeMessage(AckType ackType, ulong deliveryTag,
        ConnectionHelper connectionHelper)
    {
        if (connectionHelper == null || connectionHelper.AMQPModel.IsClosed)
            throw new Exception("No connection to RabbitMQ");

        switch (ackType)
        {
            case AckType.AutoNack:
                await connectionHelper.AMQPModel.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
                break;

            case AckType.AutoNackAndRequeue:
                await connectionHelper.AMQPModel.BasicNackAsync(deliveryTag, multiple: false, requeue: true);
                break;

            case AckType.AutoReject:
                await connectionHelper.AMQPModel.BasicRejectAsync(deliveryTag, requeue: false);
                break;

            case AckType.AutoRejectAndRequeue:
                await connectionHelper.AMQPModel.BasicRejectAsync(deliveryTag, requeue: true);
                break;
            case AckType.NoAck:
            case AckType.AutoAck:
            default:
                throw new ArgumentException($"AcknowledgeMessage should not be called with {ackType}.", nameof(ackType));
        }
    }
}
