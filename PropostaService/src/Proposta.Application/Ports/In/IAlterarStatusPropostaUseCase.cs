using Proposta.Application.DTOs;

namespace Proposta.Application.Ports.In;

/// <summary>
/// Driving Port (Porta de Entrada) para o caso de uso de Alteração de Status da Proposta.
/// </summary>
public interface IAlterarStatusPropostaUseCase
{
    Task<PropostaResponseDto> ExecutarAsync(Guid id, AlterarStatusPropostaDto dto, CancellationToken ct = default);
}
