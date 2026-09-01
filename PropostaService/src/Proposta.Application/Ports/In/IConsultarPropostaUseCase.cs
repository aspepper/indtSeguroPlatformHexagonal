using Proposta.Application.DTOs;

namespace Proposta.Application.Ports.In;

/// <summary>
/// Driving Port (Porta de Entrada) para o caso de uso de Consulta de Proposta por ID.
/// </summary>
public interface IConsultarPropostaUseCase
{
    Task<PropostaResponseDto?> ExecutarAsync(Guid id, CancellationToken ct = default);
}
