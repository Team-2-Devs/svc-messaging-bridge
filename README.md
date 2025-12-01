# svc-messaging-bridge
Microservice that bridges events from Kafka into RabbitMQ. It consumes events from Kafka topics and republishes them (`tu.image.uploaded` and `tu.recognition.completed`) to RabbitMQ. This allows each microservice to interact solely with its own message broker, without needing to manage cross-broker integration or compatibility concerns.

---

See the [full system overview](https://github.com/team-2-devs/infra-core) in the **infra-core** repository.