using System.Threading.Tasks;
using MassTransit.KafkaIntegration;

namespace MassTransit.Riders.Diagnostics.Demo
{
	public class MessageConsumer : IConsumer<Message>
	{
		private readonly ITopicProducer<AuditMessage> topicProducer;

		public MessageConsumer(ITopicProducer<AuditMessage> topicProducer)
		{
			this.topicProducer = topicProducer;
		}

		public Task Consume(ConsumeContext<Message> context)
		{
			return topicProducer.Produce(new AuditMessage
			{
				Text = context.Message.Text
			});
		}
	}
}