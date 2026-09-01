var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Contratacao.Api online");

app.Run();

public partial class Program { }
