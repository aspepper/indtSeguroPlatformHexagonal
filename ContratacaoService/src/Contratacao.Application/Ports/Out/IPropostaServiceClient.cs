using Contratacao.Application.DTOs;

namespace Contratacao.Application.Ports.Out;

/// <summary>
/// Driven Port (Porta de Saída) que define o contrato de comunicação com o PropostaService.
/// Na Arquitetura Hexagonal, a camada de Application interage apenas com esta abstração.
/// A implementação concreta (ex: HttpClient/REST, gRPC ou Mensageria) reside na camada de Infrastructure,
/// garantindo que a aplicação não saiba como a comunicação remota é realizada.
/// </summary>
public interface IPropostaServiceClient
{
    Task<PropostaStatusDto?> ObterStatusPropostaAsync(Guid propostaId, CancellationToken ct = default);
}
