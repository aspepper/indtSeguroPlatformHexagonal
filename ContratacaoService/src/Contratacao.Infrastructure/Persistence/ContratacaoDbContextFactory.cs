using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Contratacao.Infrastructure.Persistence;

/// <summary>
/// Factory de design-time para o ContratacaoDbContext.
/// </summary>
public class ContratacaoDbContextFactory : IDesignTimeDbContextFactory<ContratacaoDbContext>
{
    public ContratacaoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContratacaoDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=contratacao_db;Username=postgres;Password=postgres");

        return new ContratacaoDbContext(optionsBuilder.Options);
    }
}
