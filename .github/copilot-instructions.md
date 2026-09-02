# Instruções para o GitHub Copilot — Plataforma de Seguros

## Arquitetura (não negociável)

Este repositório segue **Arquitetura Hexagonal (Ports & Adapters)** com **DDD** em dois
microsserviços autônomos: `PropostaService` e `ContratacaoService`.

Fluxo de dependências: `Domain <- Application <- Infrastructure / Api`

- **`*.Domain`**: entidades ricas, regras de negócio e invariantes. **Zero dependências
  externas** — nunca referenciar EF Core, HttpClient, RabbitMQ, MassTransit ou qualquer
  biblioteca de infraestrutura aqui.
- **`*.Application`**: Use Cases + definição de Ports em `Ports/In` (casos de uso) e
  `Ports/Out` (interfaces para tudo que é externo: repositórios, clientes HTTP,
  publishers/consumers de mensageria). Depende apenas de `Domain` e das próprias
  interfaces de Port.
- **`*.Infrastructure`**: implementações concretas dos Ports/Out (EF Core, HttpClient,
  e agora RabbitMQ/MassTransit).
- **`*.Api`**: Controllers REST + `Program.cs` como Composition Root (é aqui que Ports
  são amarrados às implementações via `builder.Services.AddScoped<...>`).

## Regra de ouro para qualquer nova integração externa

1. Definir a interface em `Application/Ports/Out` (ex: `IPropostaEventPublisher`).
2. Implementar o adapter concreto em `Infrastructure` (ex: `RabbitMqPropostaEventPublisher`).
3. Registrar no `Program.cs` do serviço correspondente.
4. O `Domain` e os `UseCases` nunca devem saber que RabbitMQ existe — eles só conhecem a Port.

## Convenções do projeto

- C# 12 / .NET 8, nullable habilitado.
- Use Cases implementam interfaces `Ports/In` e são registrados como `Scoped`.
- Exceções de negócio: `DomainException` (uma por serviço, em `Domain/Exceptions`).
- Testes ficam em `<Servico>/tests/<Servico>.UnitTests`, um projeto por serviço.
- Serviços já existentes no `docker-compose.yml`: `proposta-db`, `contratacao-db`,
  `proposta-api`, `contratacao-api`, todos na rede `seguro-network`.

## Ao implementar mensageria (RabbitMQ)

- Biblioteca preferida: **MassTransit** com o transporte `RabbitMQ.Client`.
- Eventos de integração ficam num projeto/pasta compartilhada de contratos
  (ex: `Shared.Contracts` ou `Proposta.Application/IntegrationEvents`), nunca dentro do
  `Domain`.
- Nomeação de eventos: `PropostaCriadaEvent`, `PropostaAprovadaEvent`,
  `PropostaRejeitadaEvent` — passado, pois representam algo que já aconteceu.
- `PropostaService` publica eventos após persistir a mudança de status
  (`AlterarStatusPropostaUseCase`). Considerar padrão **Outbox** para consistência entre
  o commit no Postgres e a publicação no broker.
- `ContratacaoService` consome os eventos e mantém uma projeção local (read model) do
  status da proposta, reduzindo a dependência da chamada síncrona atual via
  `IPropostaServiceClient`.
- Adicionar serviço `rabbitmq` (imagem `rabbitmq:3-management-alpine`) ao
  `docker-compose.yml`, na mesma rede `seguro-network`, com variáveis de conexão
  passadas por `RabbitMq__Host`, `RabbitMq__User`, `RabbitMq__Password`.
- Não remover a comunicação HTTP existente sem alinhamento explícito — tratar como
  migração incremental (strangler pattern), não substituição imediata.