using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MassTransit.Riders.Diagnostics.Demo
{
	public class AuditMessageConsumer : IConsumer<AuditMessage>
	{
		private readonly ILogger<AuditMessage> logger;

		public AuditMessageConsumer(ILogger<AuditMessage> logger)
		{
			this.logger = logger;
		}

		public Task Consume(ConsumeContext<AuditMessage> context)
		{
			logger.LogInformation($"Audit message: {context.Message.Text}");

			return Task.CompletedTask;
		}
	}
}