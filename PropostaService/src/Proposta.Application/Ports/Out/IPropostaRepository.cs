using Proposta.Domain.Entities;

namespace Proposta.Application.Ports.Out;

/// <summary>
/// Driven Port (Porta de Saída) para persistência e consulta de propostas de seguro.
/// A implementação concreta deste contrato é responsabilidade da camada de Infrastructure (ex: EF Core).
/// </summary>
public interface IPropostaRepository
{
    Task<PropostaSeguro?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<PropostaSeguro>> ListarAsync(CancellationToken ct = default);
    Task AdicionarAsync(PropostaSeguro proposta, CancellationToken ct = default);
    Task AtualizarAsync(PropostaSeguro proposta, CancellationToken ct = default);
}
