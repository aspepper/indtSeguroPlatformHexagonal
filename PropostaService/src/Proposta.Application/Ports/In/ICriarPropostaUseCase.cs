using Proposta.Application.DTOs;

namespace Proposta.Application.Ports.In;

/// <summary>
/// Driving Port (Porta de Entrada) para o caso de uso de Criação de Proposta de Seguro.
/// </summary>
public interface ICriarPropostaUseCase
{
    Task<PropostaResponseDto> ExecutarAsync(CriarPropostaDto dto, CancellationToken ct = default);
}
