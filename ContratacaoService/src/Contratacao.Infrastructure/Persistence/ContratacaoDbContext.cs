using ContratacaoEntity = Contratacao.Domain.Entities.Contratacao;

namespace Contratacao.Infrastructure.Persistence;

/// <summary>
/// DbContext do EF Core para o microsserviço de Contratações.
/// </summary>
public class ContratacaoDbContext : DbContext
{
    public ContratacaoDbContext(DbContextOptions<ContratacaoDbContext> options) : base(options)
    {
    }

    public DbSet<ContratacaoEntity> Contratacoes => Set<ContratacaoEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ContratacaoEntity>(builder =>
        {
            builder.ToTable("Contratacoes");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.PropostaId)
                .IsRequired();

            builder.Property(c => c.DataContratacao)
                .IsRequired();

            // Índice único para garantir a regra de consistência de banco: uma proposta não pode ser contratada mais de uma vez
            builder.HasIndex(c => c.PropostaId)
                .IsUnique();
        });
    }
}
