using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Frends.RabbitMQ.Read.Definitions;
using Frends.RabbitMQ.Read.Helpers;

namespace Frends.RabbitMQ.Read;

/// <summary>
/// RabbitMQ Read task.
/// </summary>
public static class RabbitMQ
{
    /// <summary>
    /// Read message(s) from RabbitMQ queue. Message data is byte[] encoded to base64 and UTF8 strings.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends.RabbitMQ.Read)
    /// </summary>
    /// <param name="input">Connection and read parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, List&lt;Message&gt; MessagesBase64, List&lt;Message&gt; MessageUTF8, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static async Task<Result> Read([PropertyTab] Input input, [PropertyTab] Options options, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connectionHelper = new ConnectionHelper();
            var baseList = new List<Message>();
            var stringList = new List<Message>();

            cancellationToken.ThrowIfCancellationRequested();
            await Helper.OpenConnectionIfClosed(connectionHelper, input);

            while (input.ReadMessageCount-- > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rcvMessage = await connectionHelper.AMQPModel.BasicGetAsync(queue: input.QueueName, autoAck: input.AckType == AckType.AutoAck, cancellationToken: cancellationToken);
                if (rcvMessage != null)
                {
                    baseList.Add(new Message
                    {
                        Data = Convert.ToBase64String(rcvMessage.Body.ToArray()),
                        Headers = Helper.GetResponseHeaderDictionary(rcvMessage.BasicProperties),
                        MessagesCount = rcvMessage.MessageCount,
                        DeliveryTag = rcvMessage.DeliveryTag,
                    });

                    stringList.Add(new Message
                    {
                        Data = Encoding.UTF8.GetString(rcvMessage.Body.ToArray()),
                        Headers = Helper.GetResponseHeaderDictionary(rcvMessage.BasicProperties),
                        MessagesCount = rcvMessage.MessageCount,
                        DeliveryTag = rcvMessage.DeliveryTag,
                    });
                }
                else
                {
                    break;
                }
            }

            // Acking logic:
            // - AutoAck is handled when IChannel.BasicGetAsync() is called with autoAck: true.
            // - NoAck does not send AckMessage.
            // - Other types are handled in AcknowledgeMessage() method
            if (input.AckType is AckType.AutoAck or AckType.NoAck)
                return new Result(true, baseList, stringList);
            foreach (var message in baseList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await AcknowledgeMessage(input.AckType, message.DeliveryTag, connectionHelper, cancellationToken);
            }

            return new Result(true, baseList, stringList);
        }
        catch (Exception e)
        {
            return ErrorHandler.Handle(e, options.ThrowErrorOnFailure, options.ErrorMessageOnFailure);
        }
    }

    private static async Task AcknowledgeMessage(
        AckType ackType,
        ulong deliveryTag,
        ConnectionHelper connectionHelper,
        CancellationToken cancellationToken)
    {
        if (connectionHelper == null || connectionHelper.AMQPModel.IsClosed)
            throw new Exception("No connection to RabbitMQ");

        switch (ackType)
        {
            case AckType.AutoNack:
                await connectionHelper.AMQPModel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
                break;

            case AckType.AutoNackAndRequeue:
                await connectionHelper.AMQPModel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken: cancellationToken);
                break;

            case AckType.AutoReject:
                await connectionHelper.AMQPModel.BasicRejectAsync(deliveryTag, requeue: false, cancellationToken: cancellationToken);
                break;

            case AckType.AutoRejectAndRequeue:
                await connectionHelper.AMQPModel.BasicRejectAsync(deliveryTag, requeue: true, cancellationToken: cancellationToken);
                break;
            case AckType.NoAck:
            case AckType.AutoAck:
            default:
                throw new ArgumentException($"AcknowledgeMessage should not be called with {ackType}.", nameof(ackType));
        }
    }
}
