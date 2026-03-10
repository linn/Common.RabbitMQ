# Changelog
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
