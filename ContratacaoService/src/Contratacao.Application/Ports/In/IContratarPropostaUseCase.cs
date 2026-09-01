using Contratacao.Application.DTOs;

namespace Contratacao.Application.Ports.In;

/// <summary>
/// Driving Port (Porta de Entrada) para o caso de uso de Contratação de Proposta.
/// </summary>
public interface IContratarPropostaUseCase
{
    Task<ContratacaoResponseDto> ExecutarAsync(ContratarPropostaDto dto, CancellationToken ct = default);
}
