namespace IndtSeguro.Contracts.Events;

/// <summary>
/// Eventos de integração entre PropostaService e outros bounded contexts.
/// </summary>

public sealed record PropostaCriadaEvent(
    Guid PropostaId,
    string CpfSegurado,
    decimal ValorCobertura,
    DateTime OcorridoEm);

public sealed record PropostaAprovadaEvent(
    Guid PropostaId,
    DateTime OcorridoEm);

public sealed record PropostaRejeitadaEvent(
    Guid PropostaId,
    DateTime OcorridoEm);
