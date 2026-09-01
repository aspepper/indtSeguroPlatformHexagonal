namespace Proposta.Infrastructure.Persistence;

/// <summary>
/// DbContext do EF Core para o microsserviço de Propostas.
/// Atua como o Adaptador de Persistência na Arquitetura Hexagonal.
/// </summary>
public class PropostaDbContext : DbContext
{
    public PropostaDbContext(DbContextOptions<PropostaDbContext> options) : base(options)
    {
    }

    public DbSet<PropostaSeguro> Propostas => Set<PropostaSeguro>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PropostaSeguro>(builder =>
        {
            builder.ToTable("Propostas");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.NomeSegurado)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.CpfSegurado)
                .IsRequired()
                .HasMaxLength(11);

            builder.Property(p => p.ValorCobertura)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(p => p.Status)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(p => p.DataCriacao)
                .IsRequired();

            builder.Property(p => p.DataAtualizacao)
                .IsRequired(false);
        });
    }
}
