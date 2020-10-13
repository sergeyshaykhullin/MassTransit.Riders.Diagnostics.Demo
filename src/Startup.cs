using MassTransit.KafkaIntegration;
using Microsoft.AspNetCore.Builder;
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
				builder
					.AddSource(environment.ApplicationName)
					.AddMassTransitInstrumentation()
					.AddAspNetCoreInstrumentation()
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

		public void Configure(IApplicationBuilder app)
		{
			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGet("/send-message", async context =>
				{
					var tracer = context.RequestServices.GetRequiredService<TracerProvider>().GetTracer(environment.ApplicationName);

					using (tracer.StartActiveSpan("Publish dummy message using Bus"))
					{
						var bus = context.RequestServices.GetRequiredService<IBus>();

						await bus.Publish(new Message
						{
							Text = "No one consuming, but recorded"
						});
					}

					using (tracer.StartActiveSpan("Produce message using Kafka", SpanKind.Server))
					{
						var producer = context.RequestServices.GetRequiredService<ITopicProducer<Message>>();

						// This produce is not recorded
						await producer.Produce(new Message
						{
							Text = "Message text"
						});
					}
				});
			});
		}
	}
}