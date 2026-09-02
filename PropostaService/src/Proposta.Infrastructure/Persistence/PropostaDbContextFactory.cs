using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Proposta.Infrastructure.Persistence;

/// <summary>
/// Factory de design-time para o PropostaDbContext.
/// Permite que ferramentas de CLI do Entity Framework (ex: dotnet ef migrations add)
/// instanciem o DbContext sem depender do container de DI da API em tempo de design.
/// </summary>
public class PropostaDbContextFactory : IDesignTimeDbContextFactory<PropostaDbContext>
{
    public PropostaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PropostaDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=proposta_db;Username=postgres;Password=postgres");

        return new PropostaDbContext(optionsBuilder.Options);
    }
}
