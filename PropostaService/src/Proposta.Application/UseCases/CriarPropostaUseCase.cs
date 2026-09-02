using Proposta.Application.DTOs;
using Proposta.Application.Mappers;
using Proposta.Application.Ports.In;
using Proposta.Application.Ports.Out;
using Proposta.Domain.Entities;

namespace Proposta.Application.UseCases;

/// <summary>
/// Caso de uso para criação de uma nova proposta de seguro.
/// Na Arquitetura Hexagonal, o UseCase é uma classe de orquestração pura:
/// ele instancia a entidade de domínio (que valida suas próprias invariantes),
/// aciona a porta de saída (IPropostaRepository) para persistência e retorna o DTO de resposta.
/// </summary>
public class CriarPropostaUseCase(IPropostaRepository repository) : ICriarPropostaUseCase
{
    private readonly IPropostaRepository _repository = repository;

    public async Task<PropostaResponseDto> ExecutarAsync(CriarPropostaDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // A validação de negócio (nome, CPF, valor) ocorre no construtor da entidade PropostaSeguro
        var proposta = new PropostaSeguro(dto.NomeSegurado, dto.CpfSegurado, dto.ValorCobertura);

        await _repository.AdicionarAsync(proposta, ct);

        return proposta.ParaDto();
    }
}
