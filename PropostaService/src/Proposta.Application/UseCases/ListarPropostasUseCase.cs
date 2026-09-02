using Proposta.Application.DTOs;
using Proposta.Application.Mappers;
using Proposta.Application.Ports.In;
using Proposta.Application.Ports.Out;

namespace Proposta.Application.UseCases;

/// <summary>
/// Caso de uso para listagem de todas as propostas de seguro.
/// </summary>
public class ListarPropostasUseCase(IPropostaRepository repository) : IListarPropostasUseCase
{
    private readonly IPropostaRepository _repository = repository;

    public async Task<IEnumerable<PropostaResponseDto>> ExecutarAsync(CancellationToken ct = default)
    {
        var propostas = await _repository.ListarAsync(ct);
        return propostas.Select(p => p.ParaDto());
    }
}
