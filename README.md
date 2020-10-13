# MassTransit.Riders.Diagnostics.Demo

This is a reproducible demo for https://github.com/MassTransit/MassTransit/issues/2082

- Run `docker-compose up -d`
  - Jaeger - `localhost:16686`
  - AkHQ(Kafka UI) - `localhost:8080`

- Run this project

- HTTP GET `localhost:5000/send-message`