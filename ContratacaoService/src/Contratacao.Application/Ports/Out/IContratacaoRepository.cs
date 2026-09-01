using ContratacaoEntity = Contratacao.Domain.Entities.Contratacao;

namespace Contratacao.Application.Ports.Out;

/// <summary>
/// Driven Port (Porta de Saída) para repositório de contratações.
/// </summary>
public interface IContratacaoRepository
{
    Task<ContratacaoEntity?> ObterPorPropostaIdAsync(Guid propostaId, CancellationToken ct = default);
    Task AdicionarAsync(ContratacaoEntity contratacao, CancellationToken ct = default);
}
