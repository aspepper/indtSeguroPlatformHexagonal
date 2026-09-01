using Microsoft.EntityFrameworkCore;
using Proposta.Application.Ports.Out;
using Proposta.Domain.Entities;

namespace Proposta.Infrastructure.Persistence;

/// <summary>
/// Adaptador de Saída (Driven Adapter) que implementa a interface IPropostaRepository (Port/Out).
/// Encapsula todo o acesso ao banco de dados relacional via Entity Framework Core.
/// </summary>
public class PropostaRepository : IPropostaRepository
{
    private readonly PropostaDbContext _context;

    public PropostaRepository(PropostaDbContext context)
    {
        _context = context;
    }

    public async Task<PropostaSeguro?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Propostas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<IEnumerable<PropostaSeguro>> ListarAsync(CancellationToken ct = default)
    {
        return await _context.Propostas
            .AsNoTracking()
            .OrderByDescending(p => p.DataCriacao)
            .ToListAsync(ct);
    }

    public async Task AdicionarAsync(PropostaSeguro proposta, CancellationToken ct = default)
    {
        await _context.Propostas.AddAsync(proposta, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(PropostaSeguro proposta, CancellationToken ct = default)
    {
        _context.Propostas.Update(proposta);
        await _context.SaveChangesAsync(ct);
    }
}
