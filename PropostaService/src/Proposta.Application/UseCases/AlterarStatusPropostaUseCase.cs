using Proposta.Application.DTOs;
using Proposta.Application.Mappers;
using Proposta.Application.Ports.In;
using Proposta.Application.Ports.Out;

namespace Proposta.Application.UseCases;

/// <summary>
/// Caso de uso para alteração de status de uma proposta de seguro.
/// Orquestra a busca da proposta, invoca o método de domínio AlterarStatus
/// (que valida as regras de transição de estado) e persiste as alterações via repositório.
/// </summary>
public class AlterarStatusPropostaUseCase(IPropostaRepository repository) : IAlterarStatusPropostaUseCase
{
    private readonly IPropostaRepository _repository = repository;

    public async Task<PropostaResponseDto> ExecutarAsync(Guid id, AlterarStatusPropostaDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var proposta = await _repository.ObterPorIdAsync(id, ct)
            ?? throw new DomainException($"Proposta com Id '{id}' não encontrada.");

        // A transição de estado e suas validações são executadas dentro da entidade PropostaSeguro
        proposta.AlterarStatus(dto.NovoStatus);

        await _repository.AtualizarAsync(proposta, ct);

        return proposta.ParaDto();
    }
}
