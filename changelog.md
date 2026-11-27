# Changelog

## [6.2.0] - 2026-11-27
### *Changes*
- RabbitChannelConfiguration gains two opt-in init properties; the constructor signature is unchanged, so this is binary and source compatible, and existing callers behave as before.
- `RequestedHeartbeat` (`TimeSpan?`, whole seconds from 1 to 65535) sets the heartbeat the client requests (default: the client's 60s; fractions of a second are dropped). The broker agrees the lower of its own and this (unless either side has heartbeats off), and RabbitMQ.Client 7.2.1 closes the connection after three to four intervals with nothing read, so 15s notices a dead connection in 45-60s rather than 3-4 minutes. Checked against a local broker: closed after 45s.
- `PublisherConfirms` (`bool`) creates the producer channel with publisher confirms and confirmation tracking on, so `RabbitPublisher.PublishAsync` (and `JsonMessagePublisher<T>`) waits for the broker before returning. In RabbitMQ.Client 7.2.1:
  - ack: the publish returns normally.
  - nack: it throws `RabbitMQ.Client.Exceptions.PublishException` (`IsReturn` false).
  - returned message: not relevant, as `RabbitPublisher` publishes with `mandatory: false`, so the broker acks an unroutable message and drops it rather than returning it.
  - no answer: it waits until the caller's `CancellationToken` cancels (`OperationCanceledException`; the message may still have reached the broker) or the connection closes (`AlreadyClosedException`, which the heartbeat bounds). Pass a token with a timeout.
  - Confirms are awaited per publish but publishes on the channel are not serialised behind them, and there is no limit on outstanding confirms (passing `CreateChannelOptions` replaces the client's 128 default rate limiter with none).
  - The client adds an `x-dotnet-pub-seq-no` header to every message published with confirms, which consumers will see in `Message.Headers`.
### *Bug Fix*
- RabbitPublisher copies `Message.Headers` into the published properties instead of passing the caller's dictionary, which with confirms on the client would otherwise write its sequence-number header into (changing the caller's dictionary, or throwing for a read-only one).

## [6.1.0] - 2026-10-02
### *Bug Fix*
- RabbitPublisher now sets ContentType and DeliveryMode from Message (previously ignored). DeliveryMode 2 maps to Persistent, 1 to Transient; null leaves the library default.
### *Changes*
- JsonMessagePublisher<T> gains `ContentType` and `Persistent` init properties. The constructor signature is unchanged, so this is binary and source compatible.
- CI now runs the test project.

## [6.0.1] - 2026-08-25
### *Bug Fix*
- Remove `x-dead-letter-routing-key` from main queue arguments. Previously set to `""`, this caused RabbitMQ to overwrite the original routing key on dead-letter, making messages unroutable when requeued from the DLQ back to the original exchange. The original routing key is now preserved automatically.
- **Deployment note:** Any existing `.q` queue declared with this library must be deleted and recreated for this change to take effect. RabbitMQ will throw 406 PRECONDITION_FAILED if the queue already exists with the old arguments.

## [6.0.0] - 2026-05-28
### *Changes (Breaking)*
- RabbitMessageRouter now takes `IServiceProvider` instead of `IEnumerable<IMessageHandler>` and creates a DI scope per received message. This ensures scoped services (e.g. DbContext) are fresh for each message, preventing stale tracked entities from leaking across messages.
- Consumers must register handlers as Scoped (not Singleton) and pass `IServiceProvider` to the router.
- Added `Microsoft.Extensions.DependencyInjection.Abstractions` dependency.

## [5.2.0] - 2026-05-19
### *Changes*
- Make JsonMessageHandler<T>.HandleAsync(T, headers, ct) public instead of protected, allowing consuming applications to unit test their handler implementations directly with typed payloads rather than having to construct serialised Message objects

## [5.1.0] - 2026-05-18
### *Changes*
- Add JsonMessageHandler<T> abstract base class for type-safe message consumption with automatic JSON deserialization
- Add optional JsonSerializerOptions parameter to JsonMessagePublisher<T>

## [5.0.0] - 2026-05-12
### *Changes (Breaking)*
- Target .NET 10
- Add optional additionalBindings parameter to RabbitChannelConfiguration, allowing a consumer queue to bind to multiple external exchanges with specific routing keys
- Add ExchangeBinding record type
## [4.0.1] - 2026-04-02
### *Changes
- Add optional exchangeType parameter to RabbitChannelConfiguration
## [4.0.0] - 2026-03-10
### *Changes (VERY Breaking)*
- Upgrade to RabbitMQ.Client to v7, target .net9
## [3.2.4] - 2023-12-07
### *Changes
- Extra optional constructor parameter for ChannelConfiguration to allow exchange type and dead letter exchange types to be configurable.
### *Changes
- Extra optional constructor parameter for ChannelConfiguration to allow dlx name to be different to the exchange name. If a null or empty value is passed, the dlx name just assumes the same name as the exchange.
## [3.2.0] - 2023-10-03
### *Changes
- Extra optional constructor parameter for ChannelConfiguration to allow dlq name to be different to the q name. If a null or empty value is passed, the dlq name just assumes the same name as the q.
## [3.1.0] - 2023-08-28
### *Changes
- Extra constructor for ChannelConfiguration to allow exchange to be named and durable
## [3.0.0] - 2022-08-16
### *Changes (VERY Breaking)*
- Use newest Rabbit client. target .net6. Rewrite all the Configuration etc for the .NET native IoC framework.
