namespace Proposta.Application.IntegrationEvents;

/// <summary>
/// Eventos de integração publicados pelo PropostaService sempre que uma transição de status
/// relevante para outros Bounded Contexts (ex: ContratacaoService) é persistida com sucesso.
///
/// São contratos de comunicação entre serviços — por isso vivem em Application, não em Domain.
/// O Domain conhece apenas o "AlterarStatus"; quem decide publicar o evento de integração é o
/// Use Case, após a persistência ser confirmada pelo repositório.
///
/// Nomenclatura no passado (Criada/Aprovada/Rejeitada): representam algo que já aconteceu.
/// </summary>

/// <summary>
/// Publicado quando uma nova proposta de seguro é criada e entra em análise.
/// </summary>
/// <param name="PropostaId">Identificador da proposta.</param>
/// <param name="CpfSegurado">CPF do segurado (11 dígitos, sem máscara).</param>
/// <param name="ValorCobertura">Valor da cobertura solicitada.</param>
/// <param name="OcorridoEm">Data/hora UTC em que o evento ocorreu.</param>
public sealed record PropostaCriadaEvent(
    Guid PropostaId,
    string CpfSegurado,
    decimal ValorCobertura,
    DateTime OcorridoEm);

/// <summary>
/// Publicado quando uma proposta transiciona para o status Aprovada.
/// É o evento que o ContratacaoService consome para liberar a contratação da apólice.
/// </summary>
/// <param name="PropostaId">Identificador da proposta aprovada.</param>
/// <param name="OcorridoEm">Data/hora UTC em que a aprovação ocorreu.</param>
public sealed record PropostaAprovadaEvent(
    Guid PropostaId,
    DateTime OcorridoEm);

/// <summary>
/// Publicado quando uma proposta transiciona para o status Rejeitada.
/// </summary>
/// <param name="PropostaId">Identificador da proposta rejeitada.</param>
/// <param name="OcorridoEm">Data/hora UTC em que a rejeição ocorreu.</param>
public sealed record PropostaRejeitadaEvent(
    Guid PropostaId,
    DateTime OcorridoEm);
