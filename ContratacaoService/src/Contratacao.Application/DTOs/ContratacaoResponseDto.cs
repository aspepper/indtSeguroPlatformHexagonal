namespace Contratacao.Application.DTOs;

/// <summary>
/// DTO de saída representando a confirmação de uma contratação realizada.
/// </summary>
public record ContratacaoResponseDto(Guid Id, Guid PropostaId, DateTime DataContratacao);
