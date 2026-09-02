using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Proposta.Application.Ports.In;
using Proposta.Application.Ports.Out;
using Proposta.Application.UseCases;
using Proposta.Infrastructure.Messaging;
using Proposta.Infrastructure.Persistence;

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

// MassTransit (RabbitMQ)
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? builder.Configuration["RabbitMq__Host"] ?? "rabbitmq";
        var user = builder.Configuration["RabbitMq:Username"] ?? builder.Configuration["RabbitMq__Username"] ?? "rabbitmq";
        var pass = builder.Configuration["RabbitMq:Password"] ?? builder.Configuration["RabbitMq__Password"] ?? "rabbitmq";

        cfg.Host(host, "/", h =>
        {
            h.Username(user);
            h.Password(pass);
        });
    });
});

// Registrar o adapter que implementa IPropostaEventPublisher
builder.Services.AddScoped<IPropostaEventPublisher, MassTransitPropostaEventPublisher>();

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
