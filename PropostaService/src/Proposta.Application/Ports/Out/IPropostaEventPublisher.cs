using IndtSeguro.Contracts.Events;

namespace Proposta.Application.Ports.Out;

/// <summary>
/// Driven Port (Porta de Saída) responsável pela publicação de eventos de integração do
/// PropostaService para outros Bounded Contexts (ex: ContratacaoService).
///
/// Assim como IPropostaRepository e IPropostaServiceClient, esta interface pertence à camada de
/// Application e é totalmente agnóstica de tecnologia: os Use Cases dependem apenas deste
/// contrato, sem saber se a publicação ocorre via RabbitMQ, Kafka, Azure Service Bus ou qualquer
/// outro broker. A implementação concreta (ex: RabbitMqPropostaEventPublisher usando MassTransit)
/// fica isolada na camada de Infrastructure.
///
/// O método é genérico (PublicarAsync&lt;TEvento&gt;) para que novos eventos de integração possam
/// ser adicionados no futuro (ex: PropostaCanceladaEvent) sem exigir alterações nesta Port nem nos
/// adapters já existentes — apenas um novo record em IntegrationEvents e a chamada no Use Case
/// correspondente.
/// </summary>
public interface IPropostaEventPublisher
{
    /// <summary>
    /// Publica um evento de integração de forma assíncrona.
    /// </summary>
    /// <typeparam name="TEvento">
    /// Tipo do evento de integração (ex: PropostaAprovadaEvent, PropostaRejeitadaEvent).
    /// </typeparam>
    /// <param name="evento">Instância do evento a ser publicada.</param>
    /// <param name="ct">Token de cancelamento da operação.</param>
    Task PublicarAsync<TEvento>(TEvento evento, CancellationToken ct = default) where TEvento : class;
}
