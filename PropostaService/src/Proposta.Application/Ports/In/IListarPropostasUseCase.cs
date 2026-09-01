using Proposta.Application.DTOs;

namespace Proposta.Application.Ports.In;

/// <summary>
/// Driving Port (Porta de Entrada) para o caso de uso de Listagem de Propostas.
/// </summary>
public interface IListarPropostasUseCase
{
    Task<IEnumerable<PropostaResponseDto>> ExecutarAsync(CancellationToken ct = default);
}
