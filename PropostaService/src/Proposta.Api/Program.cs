var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Proposta.Api online");

app.Run();

public partial class Program { }
