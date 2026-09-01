# Plano Técnico — Teste INDT (Arquitetura Hexagonal)

**Projeto:** `seguro-platform-hexagonal`
**Objetivo:** Dois microsserviços .NET 8 (PropostaService e ContratacaoService) seguindo Arquitetura Hexagonal (Ports & Adapters), DDD, Clean Code, SOLID, com testes unitários e Docker.

Este documento está dividido em **etapas pequenas e independentes**. Cole cada etapa, uma de cada vez, em sequência, no Claude Code / Codex / Antigravity. Revise o resultado de cada etapa antes de avançar para a próxima — isso evita que erros se acumulem e facilita seu entendimento do código para explicar em entrevista depois.

---

## Convenções gerais (cole isso uma vez, no início da conversa com o agente, como contexto fixo)

```
Este projeto segue Arquitetura Hexagonal (Ports & Adapters) com DDD e Clean Code, em C# / .NET 8.

Estrutura de camadas por serviço:
- {Nome}.Domain        -> Entidades, Value Objects, Enums, Exceções de domínio. ZERO dependências externas.
- {Nome}.Application    -> Ports/In (interfaces de casos de uso), Ports/Out (interfaces de repositório/serviços externos),
                            UseCases (implementação dos Ports/In), DTOs, Mappers.
- {Nome}.Infrastructure -> Adapters de saída: implementação concreta dos Ports/Out (EF Core, HttpClient, etc).
- {Nome}.Api            -> Adapter de entrada: Controllers REST + Program.cs (Composition Root, onde os Ports
                            são amarrados às implementações via Dependency Injection).

Regras não negociáveis:
1. Domain nunca referencia Application, Infrastructure ou Api.
2. Application nunca referencia Infrastructure ou Api diretamente — só interfaces (Ports).
3. Toda regra de negócio (validações, transições de estado) vive dentro das entidades do Domain, nunca nos Use Cases
   e nunca nos Controllers.
4. Use Cases apenas orquestram: buscam dados via Port de saída, chamam métodos da entidade, persistem o resultado.
5. Controllers não têm lógica de negócio — só traduzem HTTP em chamadas aos Use Cases (Ports de entrada) e tratam
   exceções de domínio convertendo para o status HTTP correto.
6. Toda exceção de regra de negócio deve ser uma DomainException, capturada na camada Api e traduzida para 400/404.
7. Nomes de classes, métodos e comentários em português, seguindo o padrão do restante do projeto.
```

---

## Etapa 1 — Scaffolding da solução

```
Crie a estrutura de solução .NET 8 para dois microsserviços seguindo Arquitetura Hexagonal.

Estrutura de pastas a criar:

seguro-platform-hexagonal/
  SeguroPlatform.sln
  PropostaService/
    src/
      Proposta.Domain/Proposta.Domain.csproj          (classlib, net8.0)
      Proposta.Application/Proposta.Application.csproj (classlib, net8.0, referencia Proposta.Domain)
      Proposta.Infrastructure/Proposta.Infrastructure.csproj (classlib, net8.0, referencia Proposta.Domain e Proposta.Application)
      Proposta.Api/Proposta.Api.csproj                 (webapi, net8.0, referencia Application e Infrastructure)
    tests/
      Proposta.UnitTests/Proposta.UnitTests.csproj     (xunit, net8.0, referencia Domain e Application)
  ContratacaoService/
    src/
      Contratacao.Domain/Contratacao.Domain.csproj
      Contratacao.Application/Contratacao.Application.csproj
      Contratacao.Infrastructure/Contratacao.Infrastructure.csproj
      Contratacao.Api/Contratacao.Api.csproj
    tests/
      Contratacao.UnitTests/Contratacao.UnitTests.csproj

Pacotes NuGet a adicionar:
- Proposta.Infrastructure e Contratacao.Infrastructure: Microsoft.EntityFrameworkCore.Design, Npgsql.EntityFrameworkCore.PostgreSQL
- Proposta.Api e Contratacao.Api: Swashbuckle.AspNetCore
- Proposta.UnitTests e Contratacao.UnitTests: xunit, xunit.runner.visualstudio, Moq, Microsoft.NET.Test.Sdk
- Contratacao.Application: nenhum pacote externo (mantém puro)
- Contratacao.Infrastructure: adicionar também Microsoft.Extensions.Http (para o HttpClient nomeado)

Adicione todos os projetos à solution (SeguroPlatform.sln) e configure as referências de projeto (ProjectReference)
respeitando a direção de dependência: Domain <- Application <- Infrastructure/Api.

Ao final, rode `dotnet build` na raiz para confirmar que tudo compila (mesmo vazio) e me mostre a saída.
```

---

## Etapa 2 — Domain do PropostaService

```
Dentro de Proposta.Domain, crie:

1. Enums/StatusProposta.cs — enum com EmAnalise, Aprovada, Rejeitada.

2. Exceptions/DomainException.cs — exceção customizada simples (Exception com construtor de mensagem).

3. Entities/PropostaSeguro.cs — Aggregate Root com:
   - Propriedades com setter privado: Id (Guid), NomeSegurado (string), CpfSegurado (string),
     ValorCobertura (decimal), Status (StatusProposta), DataCriacao (DateTime), DataAtualizacao (DateTime?)
   - Construtor privado sem parâmetros (para o EF Core materializar a entidade)
   - Construtor público (nomeSegurado, cpfSegurado, valorCobertura) que:
     - Valida nome não vazio (senão lança DomainException)
     - Valida CPF com exatamente 11 dígitos (senão lança DomainException)
     - Valida valorCobertura > 0 (senão lança DomainException)
     - Gera novo Guid, seta Status = EmAnalise, DataCriacao = DateTime.UtcNow
   - Método público AlterarStatus(StatusProposta novoStatus) que:
     - Lança DomainException se o status atual já for Rejeitada (estado final)
     - Lança DomainException se tentar voltar de Aprovada para EmAnalise
     - Lança DomainException se novoStatus for igual ao status atual
     - Caso contrário, atualiza Status e seta DataAtualizacao = DateTime.UtcNow
   - Método público EstaAprovada() retornando bool

Escreva comentários XML doc curtos explicando por que a regra de negócio fica na entidade e não em outra camada.
```

---

## Etapa 3 — Application (Ports e DTOs) do PropostaService

```
Dentro de Proposta.Application, crie:

DTOs (records, em DTOs/):
- CriarPropostaDto(string NomeSegurado, string CpfSegurado, decimal ValorCobertura)
- AlterarStatusPropostaDto(StatusProposta NovoStatus)
- PropostaResponseDto(Guid Id, string NomeSegurado, string CpfSegurado, decimal ValorCobertura,
  StatusProposta Status, DateTime DataCriacao, DateTime? DataAtualizacao)

Mapper (Mappers/PropostaMapper.cs):
- Extension method ParaDto(this PropostaSeguro proposta) retornando PropostaResponseDto

Ports/Out/IPropostaRepository.cs (driven port):
- Task<PropostaSeguro?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
- Task<IEnumerable<PropostaSeguro>> ListarAsync(CancellationToken ct = default)
- Task AdicionarAsync(PropostaSeguro proposta, CancellationToken ct = default)
- Task AtualizarAsync(PropostaSeguro proposta, CancellationToken ct = default)

Ports/In/ (driving ports, uma interface por caso de uso):
- ICriarPropostaUseCase: Task<PropostaResponseDto> ExecutarAsync(CriarPropostaDto dto, CancellationToken ct = default)
- IListarPropostasUseCase: Task<IEnumerable<PropostaResponseDto>> ExecutarAsync(CancellationToken ct = default)
- IConsultarPropostaUseCase: Task<PropostaResponseDto?> ExecutarAsync(Guid id, CancellationToken ct = default)
- IAlterarStatusPropostaUseCase: Task<PropostaResponseDto> ExecutarAsync(Guid id, AlterarStatusPropostaDto dto, CancellationToken ct = default)

Explique, num comentário no topo da pasta Ports, a diferença entre Ports/In e Ports/Out para eu conseguir
explicar isso numa entrevista técnica com minhas próprias palavras.
```

---

## Etapa 4 — Use Cases do PropostaService

```
Dentro de Proposta.Application/UseCases, implemente as quatro interfaces de Ports/In criadas na etapa anterior:

- CriarPropostaUseCase: recebe IPropostaRepository via construtor, instancia PropostaSeguro (deixando a validação
  a cargo do construtor da entidade), chama AdicionarAsync, retorna o DTO mapeado.
- ListarPropostasUseCase: chama ListarAsync do repositório e mapeia a coleção.
- ConsultarPropostaUseCase: chama ObterPorIdAsync e mapeia (retornando null se não encontrado).
- AlterarStatusPropostaUseCase: busca a proposta por Id (lança DomainException "não encontrada" se nula),
  chama proposta.AlterarStatus(dto.NovoStatus), persiste via AtualizarAsync, retorna o DTO mapeado.

Todos os Use Cases devem depender apenas de IPropostaRepository (interface), nunca de uma implementação concreta.
Não adicione lógica de validação nos Use Cases — isso já vive na entidade PropostaSeguro.
```

---

## Etapa 5 — Infrastructure (EF Core) do PropostaService

```
Dentro de Proposta.Infrastructure/Persistence, crie:

1. PropostaDbContext.cs (herda DbContext):
   - DbSet<PropostaSeguro> Propostas
   - OnModelCreating configurando: tabela "Propostas", chave Id, NomeSegurado (required, max 200),
     CpfSegurado (required, max 11), ValorCobertura (decimal(18,2)), Status convertido para string
     (HasConversion<string>()), DataCriacao required, DataAtualizacao opcional.

2. PropostaRepository.cs implementando IPropostaRepository (de Proposta.Application.Ports.Out):
   - ObterPorIdAsync: AsNoTracking().FirstOrDefaultAsync
   - ListarAsync: AsNoTracking().OrderByDescending(DataCriacao).ToListAsync
   - AdicionarAsync: AddAsync + SaveChangesAsync
   - AtualizarAsync: Update + SaveChangesAsync

Depois, gere a migration inicial:
  dotnet ef migrations add InitialCreate --project src/Proposta.Infrastructure --startup-project src/Proposta.Api

Confirme que a migration foi criada e me mostre o SQL gerado (dotnet ef migrations script).
```

---

## Etapa 6 — API (Controller + Program.cs) do PropostaService

```
Dentro de Proposta.Api, crie:

1. Controllers/PropostasController.cs ([ApiController], [Route("api/[controller]")]):
   - POST / -> Criar(CriarPropostaDto dto): chama ICriarPropostaUseCase, retorna 201 CreatedAtAction
     apontando para ObterPorId, captura DomainException retornando 400 com { erro = mensagem }.
   - GET / -> Listar(): chama IListarPropostasUseCase, retorna 200 com a lista.
   - GET /{id:guid} -> ObterPorId(Guid id): chama IConsultarPropostaUseCase, retorna 200 ou 404.
   - PATCH /{id:guid}/status -> AlterarStatus(Guid id, AlterarStatusPropostaDto dto): chama
     IAlterarStatusPropostaUseCase, captura DomainException — se a mensagem contiver "não encontrada" retorna 404,
     senão 400.

2. Program.cs como Composition Root:
   - AddControllers, AddEndpointsApiExplorer, AddSwaggerGen
   - AddDbContext<PropostaDbContext> usando UseNpgsql com a connection string de appsettings
   - AddScoped<IPropostaRepository, PropostaRepository>
   - AddScoped para os quatro Use Cases (interface -> implementação)
   - Aplicar migrations automaticamente no startup (db.Database.Migrate()) dentro de um scope
   - UseSwagger/UseSwaggerUI em Development, UseHttpsRedirection, MapControllers
   - Adicionar `public partial class Program { }` ao final, para permitir testes de integração futuros

3. appsettings.json com ConnectionStrings:PropostaDb apontando para host "proposta-db" (nome do serviço no
   docker-compose), porta 5432, database proposta_db, usuário e senha postgres/postgres.

4. appsettings.Development.json sobrescrevendo o host para "localhost" (para rodar fora do Docker).

Rode `dotnet build` na solution inteira e confirme que compila sem erros.
```

---

## Etapa 7 — Testes unitários do PropostaService

```
Dentro de Proposta.UnitTests, usando xUnit + Moq, crie:

1. Domain/PropostaSeguroTests.cs (testes da entidade, sem mocks):
   - Deve criar proposta válida com Status = EmAnalise
   - Deve lançar DomainException se NomeSegurado for vazio
   - Deve lançar DomainException se CpfSegurado não tiver 11 dígitos
   - Deve lançar DomainException se ValorCobertura for <= 0
   - Deve permitir transição de EmAnalise para Aprovada
   - Deve permitir transição de EmAnalise para Rejeitada
   - Deve lançar DomainException ao tentar voltar de Aprovada para EmAnalise
   - Deve lançar DomainException ao tentar alterar status de uma proposta já Rejeitada
   - Deve lançar DomainException ao tentar setar o mesmo status atual

2. UseCases/CriarPropostaUseCaseTests.cs (mockando IPropostaRepository com Moq):
   - Deve chamar AdicionarAsync uma vez com uma proposta válida
   - Deve propagar DomainException se os dados forem inválidos (sem chamar AdicionarAsync)

3. UseCases/AlterarStatusPropostaUseCaseTests.cs (mockando IPropostaRepository):
   - Deve lançar DomainException "não encontrada" se ObterPorIdAsync retornar null
   - Deve chamar AtualizarAsync quando a transição de status for válida

Use Assert.Throws<DomainException> / Assert.ThrowsAsync<DomainException> onde aplicável.
Rode `dotnet test` e me mostre o resultado — todos os testes devem passar.
```

---

## Etapa 8 — Domain e Application do ContratacaoService

```
Repita a estrutura de Hexagonal Architecture agora para ContratacaoService, adaptando ao contexto de contratação.

Contratacao.Domain/Entities/Contratacao.cs:
   - Propriedades com setter privado: Id (Guid), PropostaId (Guid), DataContratacao (DateTime)
   - Construtor privado sem parâmetros (EF Core)
   - Construtor público (Guid propostaId) que valida propostaId != Guid.Empty (senão DomainException),
     gera novo Id, seta DataContratacao = DateTime.UtcNow
Contratacao.Domain/Exceptions/DomainException.cs — igual ao do PropostaService.

Contratacao.Application/DTOs/:
   - ContratarPropostaDto(Guid PropostaId)
   - ContratacaoResponseDto(Guid Id, Guid PropostaId, DateTime DataContratacao)
   - PropostaStatusDto(Guid Id, string Status)  -- representa a resposta vinda do PropostaService via HTTP

Contratacao.Application/Ports/Out/:
   - IContratacaoRepository: Task<Contratacao?> ObterPorPropostaIdAsync(Guid propostaId, CancellationToken ct = default);
     Task AdicionarAsync(Contratacao contratacao, CancellationToken ct = default)
   - IPropostaServiceClient: Task<PropostaStatusDto?> ObterStatusPropostaAsync(Guid propostaId, CancellationToken ct = default)
     -- este é o Port que representa a comunicação HTTP com o outro microsserviço. A Infrastructure vai
     implementá-lo com HttpClient; a Application não sabe (nem deve saber) que é HTTP.

Contratacao.Application/Ports/In/IContratarPropostaUseCase.cs:
   - Task<ContratacaoResponseDto> ExecutarAsync(ContratarPropostaDto dto, CancellationToken ct = default)

Contratacao.Application/UseCases/ContratarPropostaUseCase.cs, implementando a regra de negócio central do serviço:
   1. Busca o status da proposta via IPropostaServiceClient.
   2. Se a proposta não existir (retorno null), lança DomainException "Proposta não encontrada".
   3. Se o status não for "Aprovada", lança DomainException "Somente propostas aprovadas podem ser contratadas".
   4. Verifica via IContratacaoRepository se já existe contratação para essa proposta; se existir,
      lança DomainException "Proposta já contratada".
   5. Cria a entidade Contratacao, persiste via AdicionarAsync, retorna o DTO de resposta.

Explique num comentário por que essa orquestração entre dois serviços vive no Use Case do ContratacaoService,
e não em um "orquestrador" externo.
```

---

## Etapa 9 — Infrastructure do ContratacaoService

```
Dentro de Contratacao.Infrastructure, crie:

1. Persistence/ContratacaoDbContext.cs — DbSet<Contratacao> Contratacoes, mapeamento simples (tabela
   "Contratacoes", chave Id, PropostaId required, DataContratacao required, índice único em PropostaId
   para reforçar a regra de "não pode contratar duas vezes" também no nível de banco).

2. Persistence/ContratacaoRepository.cs implementando IContratacaoRepository com EF Core.

3. ExternalServices/PropostaServiceHttpClient.cs implementando IPropostaServiceClient:
   - Recebe HttpClient via construtor (injetado como Typed Client)
   - ObterStatusPropostaAsync faz GET para "api/propostas/{propostaId}"
   - Se a resposta for 404, retorna null
   - Se for 200, desserializa o JSON para PropostaStatusDto (atenção: o campo Status do PropostaService
     é um enum serializado como string — desserialize de forma compatível)
   - Trate falhas de rede/timeout de forma que não derrubem a aplicação (log + relançar como DomainException
     "Não foi possível verificar o status da proposta no momento")

Gere a migration inicial:
  dotnet ef migrations add InitialCreate --project src/Contratacao.Infrastructure --startup-project src/Contratacao.Api
```

---

## Etapa 10 — API do ContratacaoService

```
Dentro de Contratacao.Api, crie:

1. Controllers/ContratacoesController.cs:
   - POST /api/contratacoes -> recebe ContratarPropostaDto, chama IContratarPropostaUseCase,
     retorna 201 Created com o resultado, captura DomainException retornando 400 (ou 404 se a mensagem
     mencionar "não encontrada").

2. Program.cs:
   - AddDbContext<ContratacaoDbContext> com UseNpgsql
   - AddScoped<IContratacaoRepository, ContratacaoRepository>
   - Registrar o cliente HTTP tipado:
     builder.Services.AddHttpClient<IPropostaServiceClient, PropostaServiceHttpClient>(client =>
     {
         client.BaseAddress = new Uri(builder.Configuration["PropostaService:BaseUrl"]!);
     });
   - AddScoped<IContratarPropostaUseCase, ContratarPropostaUseCase>
   - Migrate automático no startup, Swagger em Development, MapControllers

3. appsettings.json com:
   - ConnectionStrings:ContratacaoDb apontando para host "contratacao-db"
   - PropostaService:BaseUrl = "http://proposta-api:8080" (nome do serviço no docker-compose)

4. appsettings.Development.json sobrescrevendo hosts para localhost e a porta local da PropostaService.

Rode `dotnet build` na solution inteira novamente.
```

---

## Etapa 11 — Testes unitários do ContratacaoService

```
Dentro de Contratacao.UnitTests, usando xUnit + Moq, crie ContratarPropostaUseCaseTests.cs cobrindo:

- Deve lançar DomainException se IPropostaServiceClient retornar null (proposta não encontrada)
- Deve lançar DomainException se o status da proposta não for "Aprovada"
- Deve lançar DomainException se já existir uma Contratacao para a mesma PropostaId
- Deve chamar IContratacaoRepository.AdicionarAsync uma vez quando tudo for válido, e retornar o DTO correto

Mocke tanto IPropostaServiceClient quanto IContratacaoRepository. Rode `dotnet test` e confirme que passam.
```

---

## Etapa 12 — Docker e docker-compose

```
Crie:

1. Dockerfile em PropostaService/src/Proposta.Api/Dockerfile (multi-stage: build com
   mcr.microsoft.com/dotnet/sdk:8.0, runtime com mcr.microsoft.com/dotnet/aspnet:8.0, expondo porta 8080).

2. Dockerfile equivalente em ContratacaoService/src/Contratacao.Api/Dockerfile.

3. docker-compose.yml na raiz do repositório, com os serviços:
   - proposta-db (postgres:16-alpine, volume nomeado, porta 5432 exposta só internamente)
   - contratacao-db (postgres:16-alpine, volume nomeado separado)
   - proposta-api (build a partir do Dockerfile do PropostaService, depends_on proposta-db, porta 8081:8080)
   - contratacao-api (build a partir do Dockerfile do ContratacaoService, depends_on: proposta-db, contratacao-db,
     proposta-api; porta 8082:8080)
   Todos na mesma rede Docker (bridge), para que os serviços se enxerguem pelo nome.

Teste subindo tudo com `docker-compose up --build` e valide manualmente com curl ou o Swagger de cada serviço:
1. Criar uma proposta (POST /api/propostas)
2. Aprovar a proposta (PATCH /api/propostas/{id}/status)
3. Contratar a proposta (POST /api/contratacoes) e confirmar que funciona
4. Tentar contratar de novo a mesma proposta e confirmar que retorna erro de negócio
```

---

## Etapa 13 — README e diagrama de arquitetura

```
Crie um README.md na raiz do repositório contendo:

1. Descrição curta do projeto e do que ele resolve (gestão de propostas de seguro + contratação).
2. Diagrama em Mermaid (bloco ```mermaid) mostrando a Arquitetura Hexagonal dos dois serviços:
   caixas para Domain, Application (Ports In/Out), Infrastructure (Adapters), Api, e a seta de comunicação
   HTTP do ContratacaoService para o PropostaService.
3. Instruções de execução:
   - Via Docker: `docker-compose up --build`, endpoints disponíveis em localhost:8081 (Proposta) e
     localhost:8082 (Contratação), com link para o Swagger de cada um.
   - Localmente sem Docker: como subir um Postgres, rodar `dotnet ef database update` em cada serviço,
     e `dotnet run` em cada Api.
   - Como rodar os testes: `dotnet test` na raiz da solution.
4. Explicação breve (5-8 linhas) da decisão de Arquitetura Hexagonal: por que Ports & Adapters, por que
   a comunicação entre serviços é isolada atrás de um Port (IPropostaServiceClient), e como isso facilita
   trocar Postgres por outro banco ou REST por mensageria no futuro sem tocar nas regras de negócio.
5. Exemplos de request/response (JSON) para os principais endpoints.

Revise o README gerado e ajuste a linguagem para soar natural, como se você (Alex Pimenta, Arquiteto de
Software) tivesse escrito, já que este é um artefato que pode ser mostrado como parte da avaliação técnica.
```

---

## Etapa 14 — Revisão final

```
Faça uma revisão final do repositório inteiro verificando:

1. `dotnet build` e `dotnet test` passam sem erros na raiz da solution.
2. Nenhum projeto de camada interna (Domain, Application) referencia pacotes de infraestrutura
   (EF Core, HttpClient) — só interfaces.
3. Todos os Use Cases dependem apenas de interfaces (Ports), nunca de classes concretas.
4. Todas as exceções de negócio são DomainException, tratadas corretamente nos Controllers.
5. O .gitignore cobre bin/, obj/, .vs/, appsettings.Development.json (se contiver segredo — aqui não contém,
   pode manter versionado por ser só ambiente local).
6. Gere um resumo em texto do que foi construído, para eu revisar e conseguir explicar cada decisão
   arquitetural numa eventual entrevista técnica de revisão de código.
```

---

## Observação final

Já iniciei manualmente a implementação do PropostaService (Domain, Application, Infrastructure e API) seguindo exatamente essas convenções, como prova de conceito. Se quiser, posso empacotar esse código já pronto para você usar como referência de "gabarito" enquanto revisa o que as ferramentas de IA vão gerar a partir deste plano.
