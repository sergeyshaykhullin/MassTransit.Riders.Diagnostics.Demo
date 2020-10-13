using MassTransit.KafkaIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

namespace MassTransit.Riders.Diagnostics.Demo
{
	public class Startup
	{
		private readonly IHostEnvironment environment;

		public Startup(IHostEnvironment environment)
		{
			this.environment = environment;
		}

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddOpenTelemetryTracing(builder =>
			{
				builder.AddMassTransitInstrumentation()
					.AddJaegerExporter(options => options.ServiceName = environment.ApplicationName);
			});

			services.AddMassTransit(bus =>
			{
				bus.UsingInMemory();

				bus.AddRider(rider =>
				{
					rider.AddProducer<Message>("messages");
					rider.AddProducer<AuditMessage>("audit.messages");

					rider.AddConsumer<MessageConsumer>();
					rider.AddConsumer<AuditMessageConsumer>();

					rider.UsingKafka((context, kafka) =>
					{
						kafka.Host("localhost:9092");

						kafka.TopicEndpoint<Message>("messages", "messages.group", endpoint =>
						{
							endpoint.CheckpointMessageCount = 1;
							endpoint.ConfigureConsumer<MessageConsumer>(context);
						});

						kafka.TopicEndpoint<AuditMessage>("audit.messages", "audit.messages.group", endpoint =>
						{
							endpoint.CheckpointMessageCount = 1;
							endpoint.ConfigureConsumer<AuditMessageConsumer>(context);
						});
					});
				});
			}).AddMassTransitHostedService();
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGet("/send-message", async context =>
				{
					var producer = context.RequestServices.GetRequiredService<ITopicProducer<Message>>();

					// This produce is not recorded
					await producer.Produce(new Message
					{
						Text = "Message text"
					});
				});
			});
		}
	}
}