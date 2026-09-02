var builder = WebApplication.CreateBuilder(args);

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Infrastructure (Composition Root: DbContext e Repositórios)
builder.Services.AddDbContext<ContratacaoDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ContratacaoDb");
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IContratacaoRepository, ContratacaoRepository>();

// Registro do cliente HTTP tipado para comunicação remota com o PropostaService (Port/Out -> Adapter)
builder.Services.AddHttpClient<IPropostaServiceClient, PropostaServiceHttpClient>(client =>
{
    var baseUrl = builder.Configuration["PropostaService:BaseUrl"]
        ?? throw new InvalidOperationException("A configuração 'PropostaService:BaseUrl' não foi definida.");
    
    client.BaseAddress = new Uri(baseUrl);
});

// Application Use Cases (Composition Root: Port/In -> Implementação)
builder.Services.AddScoped<IContratarPropostaUseCase, ContratarPropostaUseCase>();

var app = builder.Build();

// Aplicar migrations automaticamente no startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ContratacaoDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Contratação API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Classe parcial pública necessária para testes de integração
public partial class Program { }
