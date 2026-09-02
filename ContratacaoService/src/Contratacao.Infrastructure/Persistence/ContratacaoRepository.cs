using Contratacao.Application.Ports.Out;
using Microsoft.EntityFrameworkCore;
using ContratacaoEntity = Contratacao.Domain.Entities.Contratacao;

namespace Contratacao.Infrastructure.Persistence;

/// <summary>
/// Adaptador de Saída (Driven Adapter) para persistência de contratações via EF Core.
/// </summary>
public class ContratacaoRepository(ContratacaoDbContext context) : IContratacaoRepository
{
    private readonly ContratacaoDbContext _context = context;

    public async Task<ContratacaoEntity?> ObterPorPropostaIdAsync(Guid propostaId, CancellationToken ct = default)
    {
        return await _context.Contratacoes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PropostaId == propostaId, ct);
    }

    public async Task AdicionarAsync(ContratacaoEntity contratacao, CancellationToken ct = default)
    {
        await _context.Contratacoes.AddAsync(contratacao, ct);
        await _context.SaveChangesAsync(ct);
    }
}
