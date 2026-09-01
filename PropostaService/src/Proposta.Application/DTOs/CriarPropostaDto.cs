namespace Proposta.Application.DTOs;

/// <summary>
/// DTO de entrada para solicitação de criação de proposta de seguro.
/// </summary>
public record CriarPropostaDto(string NomeSegurado, string CpfSegurado, decimal ValorCobertura);
