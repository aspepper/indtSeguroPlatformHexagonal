var builder = WebApplication.CreateBuilder(args);

// Controllers & Swagger (configurando JsonStringEnumConverter para serializar enums como string)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Infrastructure (Composition Root: amarrando DbContext e Repositórios aos Ports/Out)
builder.Services.AddDbContext<PropostaDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PropostaDb");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IPropostaRepository, PropostaRepository>();

// Application Use Cases (Composition Root: amarrando Ports/In às implementações concretas dos Use Cases)
builder.Services.AddScoped<ICriarPropostaUseCase, CriarPropostaUseCase>();
builder.Services.AddScoped<IListarPropostasUseCase, ListarPropostasUseCase>();
builder.Services.AddScoped<IConsultarPropostaUseCase, ConsultarPropostaUseCase>();
builder.Services.AddScoped<IAlterarStatusPropostaUseCase, AlterarStatusPropostaUseCase>();

var app = builder.Build();

// Aplicar migrations automaticamente na inicialização da aplicação
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PropostaDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Proposta API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Classe parcial pública necessária para suporte a testes de integração com WebApplicationFactory
public partial class Program { }
