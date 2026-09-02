namespace Proposta.Application.DTOs;

/// <summary>
/// DTO de saída representando os dados públicos de uma proposta de seguro.
/// </summary>
public record PropostaResponseDto(
    Guid Id,
    string NomeSegurado,
    string CpfSegurado,
    decimal ValorCobertura,
    StatusProposta Status,
    DateTime DataCriacao,
    DateTime? DataAtualizacao
);
