using Proposta.Application.DTOs;
using Proposta.Application.Mappers;
using Proposta.Application.Ports.In;
using Proposta.Application.Ports.Out;

namespace Proposta.Application.UseCases;

/// <summary>
/// Caso de uso para consulta de uma proposta de seguro por seu identificador único.
/// </summary>
public class ConsultarPropostaUseCase : IConsultarPropostaUseCase
{
    private readonly IPropostaRepository _repository;

    public ConsultarPropostaUseCase(IPropostaRepository repository)
    {
        _repository = repository;
    }

    public async Task<PropostaResponseDto?> ExecutarAsync(Guid id, CancellationToken ct = default)
    {
        var proposta = await _repository.ObterPorIdAsync(id, ct);
        return proposta?.ParaDto();
    }
}
