using Proposta.Application.DTOs;
using Proposta.Domain.Entities;

namespace Proposta.Application.Mappers;

/// <summary>
/// Mapper para conversão entre a Entidade do Domínio (PropostaSeguro) e o DTO de Saída (PropostaResponseDto).
/// </summary>
public static class PropostaMapper
{
    public static PropostaResponseDto ParaDto(this PropostaSeguro proposta)
    {
        ArgumentNullException.ThrowIfNull(proposta);

        return new PropostaResponseDto(
            proposta.Id,
            proposta.NomeSegurado,
            proposta.CpfSegurado,
            proposta.ValorCobertura,
            proposta.Status,
            proposta.DataCriacao,
            proposta.DataAtualizacao
        );
    }
}
