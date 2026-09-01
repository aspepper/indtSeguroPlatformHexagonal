namespace Contratacao.Application.DTOs;

/// <summary>
/// DTO representando a resposta resumida do PropostaService recebida via cliente de comunicação.
/// </summary>
public record PropostaStatusDto(Guid Id, string Status);
