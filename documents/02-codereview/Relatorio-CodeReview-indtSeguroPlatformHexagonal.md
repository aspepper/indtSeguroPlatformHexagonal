# 📑 RELATÓRIO DE AUDITORIA DE CÓDIGO GERADO POR IA

**Projeto:** indtSeguroPlatformHexagonal (PropostaService + ContratacaoService)
**Referência de requisitos:** Teste Técnico INDT — Arquitetura Hexagonal
**Metodologia:** Auditoria estática manual, arquivo a arquivo, seguindo `instrucoes-codereview.md`
**Observação metodológica:** O ambiente de auditoria não possui .NET SDK nem acesso ao NuGet.org, portanto a validação foi 100% estática (leitura de código-fonte, `.csproj`, migrations, Dockerfiles e configs). Não foi possível executar `dotnet build`/`dotnet test`. Recomenda-se rodar `dotnet test` localmente antes do merge para confirmar os 17 testes identificados.

---

## 🎯 1. Resumo Executivo

- **Status da Aprovação:** ✅ **APROVADO COM RESSALVAS**

- **Resumo:** O projeto implementa corretamente os dois microsserviços exigidos (`PropostaService` e `ContratacaoService`) com separação estrita em 4 camadas (`Domain → Application → Infrastructure/Api`), Ports/In e Ports/Out bem definidos, Domain Model rico com invariantes protegidas no construtor/métodos das entidades, EF Core parametrizado (sem SQL cru), `AsNoTracking()` em todas as leituras, `HttpClient` tipado via `IHttpClientFactory`, Docker multi-stage e 17 testes unitários (xUnit + Moq) cobrindo domínio e casos de uso principais. A arquitetura hexagonal é o ponto mais forte da entrega — não há vazamento de EF Core/HttpClient para o Domain ou Application em nenhum dos dois serviços.

  As ressalvas encontradas são de **segurança operacional** (segredos em texto plano nos `appsettings.json`, ausência total de autenticação/autorização, ausência de tratamento de erro global/`ProblemDetails`) e de **robustez** (condição de corrida não tratada na dupla contratação, classificação de erro HTTP por `string.Contains` na mensagem da exceção). Nenhum item é um bloqueador estrutural da Arquitetura Hexagonal, mas todos devem ser endereçados antes de um ambiente de produção real.

---

## 🏛️ 2. Conformidade Arquitetural (Arquitetura Hexagonal)

- **Status:** ✅ **OK**

- **Achados:**

  | Critério | PropostaService | ContratacaoService |
  |---|---|---|
  | `Domain.csproj` sem `PackageReference` | ✅ Confirmado (csproj vazio de dependências) | ✅ Confirmado |
  | Domain sem `using` de EF Core / HttpClient / SDKs de nuvem | ✅ | ✅ |
  | Regras de negócio dentro da entidade (não no UseCase/Controller) | ✅ `PropostaSeguro.AlterarStatus()` valida todas as transições | ✅ `Contratacao` valida `PropostaId != Guid.Empty` no construtor |
  | Ports/In (Driving) e Ports/Out (Driven) separados | ✅ `Ports/In` e `Ports/Out` | ✅ idem, incluindo `IPropostaServiceClient` como Port/Out para comunicação remota |
  | Direção de dependência Infra/Api → Application → Domain | ✅ confirmado via `ProjectReference` de todos os `.csproj` | ✅ |
  | Controllers sem lógica de negócio | ✅ apenas traduzem HTTP ↔ UseCase e capturam `DomainException` | ✅ idem |
  | Injeção de dependência via abstrações (sem `new Repository()` em Application) | ✅ | ✅ |
  | Comunicação entre microsserviços isolada atrás de Port | ✅ `IPropostaServiceClient` isola a chamada HTTP; o UseCase não sabe que é REST | — |

  **Não há violação de isolamento do Core.** As únicas instâncias de `new XxxDbContext(...)` encontradas estão em `PropostaDbContextFactory` / `ContratacaoDbContextFactory`, que são `IDesignTimeDbContextFactory<T>` — um padrão oficial e esperado do EF Core para permitir `dotnet ef migrations add` fora do container de DI da aplicação. **Isso não é uma violação**, mas está listado na seção de Segurança abaixo por conter credenciais em texto plano (ver item 3.3).

  **Observação de nomenclatura:** `Contratacao.Domain.Entities.Contratacao` (entidade com o mesmo nome do namespace raiz) obriga o uso de alias (`using ContratacaoEntity = ...`) em várias classes. Funciona, mas é um code smell leve — considere renomear a entidade para `ContratacaoSeguro` ou `Apolice`, análogo ao `PropostaSeguro` do outro serviço, por consistência e legibilidade.

---

## 🔒 3. Segurança e OWASP Top 10

- **Status:** ⚠️ **RISCO DETECTADO** (nenhum item crítico/bloqueador, mas vários itens do checklist obrigatório estão ausentes)

### 3.1 Concatenação de SQL — ✅ OK
Nenhuma ocorrência de `FromSqlRaw`, `ExecuteSqlRaw` ou concatenação de string SQL. Toda persistência passa pelo EF Core via LINQ (`PropostaRepository`, `ContratacaoRepository`), que parametriza automaticamente as consultas.

### 3.2 Criptografia obsoleta / geração insegura de tokens — ✅ OK (N/A)
Não há uso de `MD5`, `SHA1`, `TripleDES` ou `System.Random` para geração de tokens/senhas no projeto. Como não há autenticação implementada (ver 3.4), este item é atualmente inaplicável — mas será relevante assim que autenticação for adicionada.

### 3.3 Dados Sensíveis Expostos — ❌ **VIOLAÇÃO**
Senhas de banco de dados em **texto plano** em múltiplos arquivos versionados:

```jsonc
// PropostaService/src/Proposta.Api/appsettings.json
"ConnectionStrings": {
  "PropostaDb": "Host=proposta-db;Port=5432;Database=proposta_db;Username=postgres;Password=postgres"
}
```
O mesmo padrão se repete em `appsettings.Development.json` de ambos os serviços e nos `DbContextFactory` (`PropostaDbContextFactory.cs`, `ContratacaoDbContextFactory.cs`), além da senha `postgres` fixa no `docker-compose.yml`.

**Correção recomendada:**
- Em desenvolvimento local: usar `dotnet user-secrets` em vez de `appsettings.Development.json`.
- Em produção/staging: remover a chave `ConnectionStrings` do `appsettings.json` e injetar via variável de ambiente (`ConnectionStrings__PropostaDb`) a partir de um cofre de segredos (Azure Key Vault, AWS Secrets Manager, Docker Secrets ou GitHub Actions Secrets no pipeline).
- O `docker-compose.yml` de desenvolvimento pode manter uma senha fixa **desde que documentado como ambiente local/efêmero**, mas nunca deve ser reaproveitado como imagem de produção.

```csharp
// Refatorado — PropostaDbContextFactory.cs
public class PropostaDbContextFactory : IDesignTimeDbContextFactory<PropostaDbContext>
{
    public PropostaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PROPOSTA_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=proposta_db;Username=postgres;Password=postgres"; // fallback só para dev local

        var optionsBuilder = new DbContextOptionsBuilder<PropostaDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new PropostaDbContext(optionsBuilder.Options);
    }
}
```

### 3.4 Autorização (BOLA) e Autenticação — ❌ **VIOLAÇÃO**
Nenhum dos dois serviços possui `[Authorize]`, middleware de autenticação (JWT/OAuth) ou qualquer verificação de identidade. Todos os endpoints são públicos:
- `GET /api/propostas/{id}` — qualquer chamador pode consultar qualquer proposta por Guid.
- `POST /api/contratacoes` — qualquer chamador pode contratar qualquer proposta aprovada de terceiros, bastando conhecer o `propostaId`.

O teste técnico não exige explicitamente autenticação, e isso é comum ficar fora do escopo de um teste focado em arquitetura — porém o checklist de segurança usado nesta auditoria trata BOLA como item obrigatório, portanto fica registrado como risco a ser mitigado antes de qualquer exposição real.

**Correção recomendada (mínima):** adicionar autenticação via JWT Bearer (`AddAuthentication().AddJwtBearer(...)`) e decorar os controllers com `[Authorize]`; validar no UseCase se o usuário autenticado é o dono do recurso (ex.: comparar um `ClaimTypes.NameIdentifier` com o `segurado` da proposta), e não apenas confiar no ID da rota.

### 3.5 Exibição de Erros — ✅ OK, com ressalva
Os `catch (DomainException ex)` nos controllers retornam apenas `ex.Message`, nunca `StackTrace` ou exceções de infraestrutura brutas — correto. Porém **não existe middleware de tratamento de exceção global** (`UseExceptionHandler` / `ProblemDetails`) para exceções *não* mapeadas (ex.: falha de conexão com o Postgres, `DbUpdateException`, `TaskCanceledException` fora do client HTTP). Em produção, isso resultará em 500 genérico do ASP.NET Core (sem vazamento, mas sem padronização RFC 7807 nem correlação de log). Ver refatoração na Seção 5.

### 3.6 CSRF — ✅ N/A
API pura, sem cookies de sessão nem Razor/MVC Views — `[ValidateAntiForgeryToken]` não se aplica a este modelo de autenticação por token/header.

### 3.7 CORS — ⚠️ Ausente (não é violação, mas incompleto)
Nenhuma política de CORS foi configurada em nenhum dos dois `Program.cs`. Não há `.AllowAnyOrigin()` (portanto não há a violação clássica do checklist), mas também nenhuma política explícita — se um frontend browser precisar consumir a API diretamente, as chamadas falharão por CORS até que uma política restrita a domínios conhecidos seja adicionada.

### 3.8 Rate Limiting — ❌ **AUSENTE**
Nenhum dos endpoints (incluindo `POST /api/propostas` e `POST /api/contratacoes`, que são rotas de mutação sensíveis a abuso) possui `Microsoft.AspNetCore.RateLimiting` configurado.

**Correção recomendada:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("padrao", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
// ...
app.UseRateLimiter();
app.MapControllers().RequireRateLimiting("padrao");
```

---

## ⚡ 4. Performance, Recursos e Boas Práticas C#

- **Status:** ⚠️ **NECESSITA OTIMIZAÇÃO** (pontos pontuais, nada crítico)

### 4.1 `IDisposable` / `using` — ✅ OK
`DbContext` é gerenciado pelo container de DI (`AddDbContext`, ciclo de vida `Scoped`), sem instanciação manual em runtime. `HttpClient` é registrado via `AddHttpClient<IPropostaServiceClient, PropostaServiceHttpClient>`, que usa `IHttpClientFactory` internamente — evita o clássico problema de exaustão de sockets/DNS stale de `new HttpClient()` direto. `JsonDocument` em `PropostaServiceHttpClient` está corretamente em `using var doc = ...`.

### 4.2 Ciclo de vida de DI — ✅ OK
Todos os serviços registrados são `Scoped` (`AddScoped<IPropostaRepository, PropostaRepository>`, UseCases, `AddHttpClient<>` que por padrão registra como `Transient`/gerenciado pela factory). Não foi encontrado nenhum `Singleton` capturando dependência `Scoped` (nenhum `AddSingleton` no projeto, portanto não há Captive Dependency).

### 4.3 Consultas EF Core — ✅ OK
`ObterPorIdAsync`, `ListarAsync` (PropostaRepository) e `ObterPorPropostaIdAsync` (ContratacaoRepository) usam `.AsNoTracking()` corretamente, pois são leituras que não exigem rastreamento de mudanças.

⚠️ Ressalva menor: em `AlterarStatusPropostaUseCase`, a entidade é obtida via `ObterPorIdAsync` (que usa `AsNoTracking`) e depois passada para `_context.Propostas.Update(proposta)` dentro de `AtualizarAsync`. Isso funciona porque a entidade é reanexada (`Update` marca todas as propriedades como modificadas), mas gera um `UPDATE` de todas as colunas em vez de apenas as alteradas. Para uma entidade pequena como esta, o impacto é irrelevante — apenas registre como padrão a observar se a entidade crescer.

### 4.4 Condição de corrida na dupla contratação — ⚠️ **ACHADO RELEVANTE**
`ContratarPropostaUseCase.ExecutarAsync` verifica `ObterPorPropostaIdAsync` e só então insere — clássico padrão *check-then-act*, que não é atômico. A migration corretamente cria um **índice único** em `PropostaId` (`IX_Contratacoes_PropostaId`), o que impede duplicidade real no banco sob concorrência — **isso é o design correto**. Porém, se duas requisições concorrentes passarem ambas pela checagem antes de qualquer uma commitar, a segunda chamada a `AdicionarAsync` lançará uma `DbUpdateException` (violação de índice único) que **não é tratada** em nenhum lugar — nem no UseCase, nem no Controller (`catch (DomainException ex)` não captura `DbUpdateException`). O resultado seria um `500 Internal Server Error` não amigável em vez do esperado `400 Proposta já contratada.`, exatamente o cenário de concorrência que a UNIQUE constraint foi desenhada para proteger.

**Correção recomendada:**
```csharp
// ContratarPropostaUseCase.cs
try
{
    await _contratacaoRepository.AdicionarAsync(contratacao, ct);
}
catch (DbUpdateException) // violação do índice único IX_Contratacoes_PropostaId
{
    throw new DomainException("Proposta já contratada.");
}
```
> Nota: para manter o Domain/Application 100% livres de `Microsoft.EntityFrameworkCore`, esse `catch` deveria residir no `ContratacaoRepository.AdicionarAsync` (Infrastructure), lançando uma exceção de conflito que o UseCase then translates — ver Seção 5 para o exemplo completo.

### 4.5 Classificação de erro por `string.Contains` na mensagem — ⚠️ Code smell
Em ambos os controllers, a decisão entre `404 NotFound` e `400 BadRequest` é feita assim:
```csharp
if (ex.Message.Contains("não encontrada", StringComparison.OrdinalIgnoreCase))
    return NotFound(new { erro = ex.Message });
return BadRequest(new { erro = ex.Message });
```
Isso acopla o comportamento HTTP ao texto exato da mensagem de negócio — qualquer mudança de wording (inclusive tradução/i18n futura) quebra silenciosamente o contrato HTTP. Ver refatoração recomendada na Seção 5 (exceções tipadas: `RecursoNaoEncontradoException : DomainException`).

### 4.6 Pacotes NuGet — ✅ OK
Dependências mínimas e coerentes com a função de cada camada: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `Swashbuckle.AspNetCore`, `Microsoft.Extensions.Http`, `Moq`, `xunit`. Nenhum pacote supérfluo ou de procedência duvidosa. Versões (`8.0.11`, `4.20.72`, `2.9.2`) são recentes e compatíveis com .NET 8.

### 4.7 Estilo / Roslyn — ⚠️ Pontos menores
- Não há `.editorconfig` nem `global.json` no repositório — recomendável para padronizar formatação e fixar a versão exata do SDK entre desenvolvedores/CI.
- Os diretórios `bin/` e `obj/` de todos os projetos foram incluídos no `.zip` enviado (não há `.gitignore` no repositório). Isso não afeta a arquitetura, mas indica ausência de `.gitignore` no controle de versão real — deve ser adicionado antes do primeiro `git init`/push.
- Dockerfiles rodam o processo `dotnet` como usuário `root` dentro do container final (`mcr.microsoft.com/dotnet/aspnet:8.0`), sem `USER` não-root definido. Boa prática de hardening seria adicionar um usuário dedicado:
  ```dockerfile
  FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
  WORKDIR /app
  RUN adduser --disabled-password --home /app appuser && chown -R appuser /app
  USER appuser
  EXPOSE 8080
  ENV ASPNETCORE_URLS=http://+:8080
  COPY --from=build /app/publish .
  ENTRYPOINT ["dotnet", "Proposta.Api.dll"]
  ```
- `docker-compose.yml`: o `contratacao-api` depende de `proposta-api` com `condition: service_started` (não `service_healthy`), e nenhum dos dois serviços de API define `healthcheck`. Como a chamada HTTP entre serviços não tem retry/circuit breaker (ex.: Polly), uma inicialização lenta do `proposta-api` pode causar falhas transitórias nas primeiras chamadas de `POST /api/contratacoes`. Sugestão: adicionar `healthcheck` HTTP nos dois serviços de API e usar `condition: service_healthy`, e/ou adicionar uma política de retry com `Microsoft.Extensions.Http.Polly` no `AddHttpClient`.

---

## 🧪 4.5 Cobertura de Testes e Aderência aos Requisitos do PDF

| Requisito do teste técnico | Status |
|---|---|
| C# / .NET 8+ | ✅ `net8.0` em todos os projetos |
| Arquitetura Hexagonal | ✅ conforme Seção 2 |
| Dois microsserviços (`PropostaService`, `ContratacaoService`) | ✅ |
| `PropostaService`: criar / listar / alterar status | ✅ `POST`, `GET`, `GET/{id}`, `PATCH /status` |
| `ContratacaoService`: contratar somente se Aprovada, armazenar ID + data, comunicar com PropostaService | ✅ implementado em `ContratarPropostaUseCase` + `PropostaServiceHttpClient` |
| Banco relacional | ✅ PostgreSQL (via Npgsql), um banco por serviço (`proposta_db`, `contratacao_db`) — corretamente respeita *database-per-service* |
| Comunicação entre microsserviços | ✅ HTTP REST via `HttpClient` tipado |
| Clean Architecture / DDD | ✅ camadas bem definidas, Aggregate Roots com invariantes |
| Docker (bônus) | ✅ Dockerfile multi-stage para os dois serviços + `docker-compose.yml` orquestrando 4 containers (2 APIs + 2 bancos) |
| Testes automatizados | ✅ 17 testes xUnit/Moq — cobrem 100% das regras de transição de `PropostaSeguro` e os principais fluxos de `CriarPropostaUseCase`, `AlterarStatusPropostaUseCase` e `ContratarPropostaUseCase`. ⚠️ Faltam testes para `ConsultarPropostaUseCase`, `ListarPropostasUseCase` e testes de integração (`WebApplicationFactory`) dos Controllers — o `Program.cs` já expõe `public partial class Program {}`, indicando que a intenção de suportar testes de integração existe, mas não foi concluída. |
| README com instruções de build/execução | ✅ completo, com exemplos de request/response |
| Banco versionado com migrations | ✅ EF Core Migrations (`InitialCreate`) para os dois serviços |
| Diagrama de arquitetura (bônus) | ✅ diagrama Mermaid no README |
| Mensageria (bônus) | ❌ não implementado (comunicação é apenas HTTP síncrona) — aceitável, pois o PDF trata mensageria como bônus opcional |

---

## 🔧 5. Plano de Ação e Refatoração Recomendada

### Prioridade Alta

**1. Tratar concorrência na dupla contratação (Seção 4.4)** — mover o tratamento de `DbUpdateException` para a Infrastructure, mantendo o Domain/Application livres de EF Core:

```csharp
// Contratacao.Infrastructure/Persistence/ContratacaoRepository.cs
using Microsoft.EntityFrameworkCore;
using Contratacao.Application.Ports.Out;
using Contratacao.Domain.Exceptions;
using ContratacaoEntity = Contratacao.Domain.Entities.Contratacao;

public class ContratacaoRepository : IContratacaoRepository
{
    private readonly ContratacaoDbContext _context;

    public ContratacaoRepository(ContratacaoDbContext context) => _context = context;

    public async Task<ContratacaoEntity?> ObterPorPropostaIdAsync(Guid propostaId, CancellationToken ct = default) =>
        await _context.Contratacoes.AsNoTracking().FirstOrDefaultAsync(c => c.PropostaId == propostaId, ct);

    public async Task AdicionarAsync(ContratacaoEntity contratacao, CancellationToken ct = default)
    {
        await _context.Contratacoes.AddAsync(contratacao, ct);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Contratacoes_PropostaId") == true)
        {
            // Violação do índice único: outra requisição concorrente já contratou esta proposta.
            throw new DomainException("Proposta já contratada.");
        }
    }
}
```

**2. Remover segredos em texto plano dos arquivos versionados (Seção 3.3)** — usar `dotnet user-secrets` em dev e variáveis de ambiente/cofre em produção, conforme exemplo já apresentado na Seção 3.3.

**3. Adicionar middleware de tratamento de exceção global com `ProblemDetails`** (Seção 3.5):

```csharp
// Program.cs (ambos os serviços)
builder.Services.AddProblemDetails();
// ...
var app = builder.Build();

app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(feature?.Error, "Erro não tratado ao processar {Path}", context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            erro = "Ocorreu um erro interno inesperado. Tente novamente mais tarde."
        });
    });
});
```

### Prioridade Média

**4. Substituir classificação de erro por `string.Contains` por exceções tipadas (Seção 4.5):**

```csharp
// Proposta.Domain/Exceptions/RecursoNaoEncontradoException.cs
namespace Proposta.Domain.Exceptions;

public class RecursoNaoEncontradoException : DomainException
{
    public RecursoNaoEncontradoException(string mensagem) : base(mensagem) { }
}
```
```csharp
// AlterarStatusPropostaUseCase.cs
var proposta = await _repository.ObterPorIdAsync(id, ct)
    ?? throw new RecursoNaoEncontradoException($"Proposta com Id '{id}' não encontrada.");
```
```csharp
// PropostasController.cs
catch (RecursoNaoEncontradoException ex)
{
    return NotFound(new { erro = ex.Message });
}
catch (DomainException ex)
{
    return BadRequest(new { erro = ex.Message });
}
```

**5. Adicionar autenticação/autorização básica e validação de posse do recurso (BOLA — Seção 3.4).**

**6. Adicionar Rate Limiting nas rotas de mutação (Seção 3.8).**

### Prioridade Baixa (housekeeping)

**7.** Adicionar `.gitignore` (excluindo `bin/`, `obj/`) e `.editorconfig`/`global.json` ao repositório.
**8.** Renomear a entidade `Contratacao.Domain.Entities.Contratacao` para evitar colisão com o namespace raiz (elimina a necessidade do alias `ContratacaoEntity`).
**9.** Adicionar `USER` não-root nos Dockerfiles e `healthcheck` HTTP nos serviços de API no `docker-compose.yml`.
**10.** Completar a suíte de testes: `ConsultarPropostaUseCase`, `ListarPropostasUseCase`, e testes de integração dos Controllers via `WebApplicationFactory<Program>` (a infraestrutura para isso já existe, dado o `public partial class Program {}` exposto).

---

## ✅ Conclusão

A entrega demonstra domínio sólido de Arquitetura Hexagonal e DDD em C#/.NET 8: isolamento real do Domain, Ports/In e Ports/Out corretamente aplicados, Aggregate Roots com invariantes protegidas, e composição de dependências centralizada nos `Program.cs`. É uma base técnica adequada para o teste proposto. As ressalvas de segurança (segredos em texto plano, ausência de auth/rate limiting) e de robustez (condição de corrida não tratada, acoplamento de HTTP status a texto de mensagem) são pontuais, bem localizadas e endereçáveis com as refatorações acima, sem exigir qualquer redesenho estrutural.
