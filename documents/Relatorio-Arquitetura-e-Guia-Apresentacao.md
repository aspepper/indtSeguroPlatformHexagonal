# 🏛️ Relatório de Arquitetura + Guia de Perguntas para Apresentação
### Projeto: `indtSeguroPlatformHexagonal` (PropostaService + ContratacaoService)

Este documento tem dois objetivos:
1. **Explicar a arquitetura em profundidade** — o suficiente para você defender qualquer decisão de design com confiança.
2. **Antecipar perguntas** que um avaliador técnico provavelmente fará, organizadas por tema, com a resposta pronta e — quando relevante — a fraqueza real que você deve admitir em vez de tentar esconder (avaliadores gostam mais de "sei que isso é uma limitação, e faria X para resolver" do que de respostas defensivas).

---

## PARTE 1 — VISÃO GERAL DO SISTEMA

O sistema resolve o ciclo de vida de uma proposta de seguro em dois microsserviços autônomos:

```
Cliente → PropostaService  (cria, lista, consulta, aprova/rejeita propostas)
              ↑
              │ HTTP GET /api/propostas/{id}   (consulta síncrona de status)
              │
Cliente → ContratacaoService (contrata uma proposta Aprovada, guarda a contratação)
```

- **PropostaService** é dono do ciclo de vida da proposta (`EmAnalise → Aprovada/Rejeitada`).
- **ContratacaoService** é dono do processo de contratação e consulta o `PropostaService` via HTTP para validar a pré-condição de negócio ("a proposta está Aprovada?") antes de efetivar.
- Cada serviço tem **seu próprio banco de dados** (`proposta_db`, `contratacao_db`) — *database-per-service*, um dos pilares de microsserviços corretamente aplicado aqui.
- Cada serviço segue internamente a mesma estrutura de 4 camadas: `Domain → Application → Infrastructure/Api`.

---

## PARTE 2 — ARQUITETURA EM DETALHE

### 2.1 As 4 camadas e o que cada uma pode/não pode fazer

| Camada | Contém | Pode depender de | Regra de ouro |
|---|---|---|---|
| **Domain** | Entidades (`PropostaSeguro`, `Contratacao`), Enums, `DomainException` | Nada (zero pacotes NuGet) | Se essa camada soubesse o que é um banco de dados, a arquitetura estaria quebrada |
| **Application** | Ports/In (contratos dos casos de uso), Ports/Out (contratos de infraestrutura), UseCases, DTOs, Mappers | Apenas Domain | Orquestra, não decide regra de negócio nem sabe qual banco/HTTP client é usado |
| **Infrastructure** | Implementações concretas dos Ports/Out: `PropostaRepository` (EF Core), `PropostaServiceHttpClient` | Domain + Application (para implementar as interfaces) | É a única camada que "sabe" que existe Postgres, EF Core ou HTTP |
| **Api** | Controllers REST, `Program.cs` (Composition Root) | Application + Infrastructure | Traduz HTTP ↔ UseCase, nada mais |

### 2.2 Ports & Adapters — o coração do padrão hexagonal

- **Port** = uma interface (contrato). Vive na camada Application.
  - **Port/In** (Driving Port): o que o mundo externo pode pedir à aplicação. Ex.: `ICriarPropostaUseCase`. Quem chama? O Controller.
  - **Port/Out** (Driven Port): o que a aplicação precisa do mundo externo. Ex.: `IPropostaRepository`, `IPropostaServiceClient`. Quem implementa? A Infrastructure.
- **Adapter** = a implementação concreta de um Port.
  - **Driving/Primary Adapter**: `PropostasController` — adapta HTTP para uma chamada de Port/In.
  - **Driven/Secondary Adapter**: `PropostaRepository` (adapta EF Core/Postgres para `IPropostaRepository`) e `PropostaServiceHttpClient` (adapta `HttpClient` para `IPropostaServiceClient`).
- **Regra da direção de dependência**: setas sempre apontam **para dentro** (Infra/Api → Application → Domain). O Domain nunca sabe que Infrastructure existe — ele só define contratos que a Infrastructure é obrigada a cumprir. Isso é o **Princípio de Inversão de Dependência (o "D" do SOLID)** aplicado na escala arquitetural.

### 2.3 DDD aplicado

- **Aggregate Root**: `PropostaSeguro` e `Contratacao` são raízes de agregado — toda mutação de estado passa por um método público da entidade (`AlterarStatus`), nunca por um setter externo. As propriedades têm `private set`.
- **Rich Domain Model** (Modelo Rico): as regras vivem na entidade (validação de CPF, valor de cobertura > 0, transições de status proibidas), não em um `Service` anêmico externo. Isso é o oposto do *Anemic Domain Model* (entidade só com getters/setters e toda a lógica em serviços).
- **Domain Exception**: `DomainException` é o vocabulário de erro do negócio — nunca vaza uma `NullReferenceException` ou `SqlException` para fora do domínio.
- **Bounded Context**: cada microsserviço é um Bounded Context — `PropostaService` não conhece a entidade `Contratacao`, e vice-versa. A "ligação" entre eles é feita por um contrato de infraestrutura (HTTP), não por referência de objeto compartilhado.
- **O que NÃO foi feito (e é importante você saber)**: não há Value Objects explícitos (ex.: uma classe `Cpf` ou `Dinheiro`) — CPF é validado inline no construtor mas permanece `string`. Isso é uma simplificação aceitável para o escopo, mas é uma limitação real do "DDD tático" — ver Parte 3 e a seção de perguntas.

### 2.4 SOLID, na prática deste projeto

| Princípio | Onde aparece |
|---|---|
| **S**RP | Cada UseCase faz uma única coisa (`CriarPropostaUseCase` só cria); Controller só traduz HTTP |
| **O**CP | Novos casos de uso são adicionados criando nova classe + interface, sem alterar as existentes |
| **L**SP | N/A direto (pouca herança no projeto) — mas as implementações de Port respeitam integralmente o contrato da interface |
| **I**SP | Interfaces pequenas e específicas (`ICriarPropostaUseCase` só tem `ExecutarAsync`, não um "God Interface" com todos os métodos) |
| **D**IP | O pilar central do projeto: Application depende de abstração (`IPropostaRepository`), Infrastructure implementa. Program.cs é o único lugar que conhece as duas pontas (Composition Root) |

### 2.5 Comunicação entre microsserviços

`ContratacaoService` consulta `PropostaService` via **HTTP síncrono** (`GET /api/propostas/{id}`), abstraído atrás de `IPropostaServiceClient`. A implementação concreta (`PropostaServiceHttpClient`) usa `HttpClient` tipado registrado com `AddHttpClient<TInterface, TImplementation>` — isso usa `IHttpClientFactory` por baixo dos panos, evitando o problema clássico de exaustão de sockets/DNS obsoleto de instanciar `new HttpClient()` a cada chamada.

### 2.6 Persistência

- PostgreSQL, um banco por serviço.
- EF Core Code-First com Migrations versionadas (`InitialCreate`).
- `AsNoTracking()` em todas as consultas de leitura (evita overhead de change tracking desnecessário).
- Índice único (`IX_Contratacoes_PropostaId`) garante a nível de banco que uma proposta não pode ter duas contratações — a defesa de consistência não depende só da checagem em memória do UseCase.

### 2.7 Docker

Dockerfile multi-stage (`sdk:8.0` para build/publish, `aspnet:8.0` para runtime — reduz o tamanho final da imagem por não carregar o SDK completo em produção). `docker-compose.yml` orquestra 4 containers: 2 bancos Postgres + 2 APIs, com rede dedicada (`seguro-network`) e `healthcheck` nos bancos.

---

## PARTE 3 — DECISÕES DE DESIGN E TRADE-OFFS (fale sobre isso proativamente)

Levar essas decisões para a apresentação **antes** que perguntem mostra maturidade técnica:

1. **Por que HTTP síncrono e não mensageria (Kafka/RabbitMQ)?**
   Simplicidade e escopo do teste. HTTP síncrono é suficiente para uma consulta de leitura pontual ("qual o status desta proposta agora?"). Mensageria brilha quando você precisa de desacoplamento temporal, replay de eventos ou quando o `ContratacaoService` precisasse *reagir* a mudanças de status (ex.: notificar automaticamente quando uma proposta é aprovada). Isso é citado no PDF do teste como *bônus*, não requisito.

2. **Por que banco de dados por serviço, e não um banco compartilhado?**
   Autonomia real de deploy e schema — um dos requisitos não-negociáveis de microsserviços. Se os dois serviços compartilhassem schema, teríamos acoplamento por banco de dados (o pior tipo de acoplamento em microsserviços, pois viola encapsulamento de dados).

3. **Por que a checagem "proposta está aprovada?" fica no `ContratarPropostaUseCase` do `ContratacaoService`, e não é o `PropostaService` quem "empurra" a informação?**
   Porque `ContratacaoService` é quem possui a regra de negócio "só contrato quem está aprovado" — essa invariante pertence ao Bounded Context de Contratação, não ao de Proposta. `PropostaService` não deveria saber que "contratação" existe.

4. **Por que não usar CQRS/MediatR?**
   Overhead desnecessário para o volume de casos de uso deste teste (4 no PropostaService, 1 no ContratacaoService). Ports/In com UseCases dedicados já entregam a mesma separação de responsabilidade com muito menos "mágica"/infraestrutura de framework — mais fácil de explicar e debugar em uma entrevista.

5. **Por que Migrations automáticas no startup (`db.Database.Migrate()`)?**
   Facilita a demonstração/avaliação (`docker compose up` já sobe com schema pronto). Em produção real, a prática recomendada é migrations aplicadas via pipeline de CI/CD dedicado, não no boot da aplicação (evita race condition entre múltiplas réplicas migrando ao mesmo tempo).

---

## PARTE 4 — GUIA DE PERGUNTAS PROVÁVEIS (com respostas)

### 🔷 4.1 Arquitetura Hexagonal

**P: Por que Arquitetura Hexagonal e não Clean Architecture "clássica" ou N-Camadas tradicional?**
R: Clean Architecture e Hexagonal são primos — ambas invertem a dependência para o Domain. Hexagonal foi escolhida porque o vocabulário Ports/In e Ports/Out deixa muito explícito "o que a aplicação oferece" vs. "o que ela precisa", o que facilita testar cada caso de uso isoladamente com mocks das portas de saída, sem subir banco nem servidor HTTP.

**P: Qual a diferença prática entre uma "Port" e uma "Interface comum"?**
R: Tecnicamente, em C#, uma Port *é* uma interface. A diferença é de intenção arquitetural: ela vive na camada Application e representa uma fronteira do sistema (entrada ou saída), não apenas um contrato interno de conveniência.

**P: Se eu abrir o projeto, como eu sei rapidamente se uma classe está no lugar certo?**
R: Regra prática: se a classe tiver `using Microsoft.EntityFrameworkCore` ou `using System.Net.Http`, ela só pode estar em `Infrastructure`. Se ela representa uma regra de negócio ("proposta aprovada não pode virar Em Análise"), só pode estar em `Domain`. Se ela orquestra (busca → chama método de domínio → salva), é um `UseCase` em `Application`.

**P: Onde estão os testes de que essa separação realmente é respeitada — algo automatizado, tipo ArchUnit/NetArchTest?**
R: **Não há.** Hoje a conformidade é garantida por revisão manual/disciplina, não por um teste de arquitetura automatizado. Isso é uma limitação real — o ideal seria adicionar um teste com `NetArchTest.Rules` no CI garantindo, por exemplo, que `Proposta.Domain` nunca referencie `Microsoft.EntityFrameworkCore`.

---

### 🔷 4.2 DDD

**P: Por que CPF e valor monetário não são Value Objects?**
R: É uma simplificação consciente para o escopo do teste. O ideal em DDD tático seria uma classe `Cpf` com sua própria validação e igualdade por valor (`record Cpf(string Numero)`), reutilizável em qualquer entidade que precise de CPF, em vez de repetir a validação (`string.Concat(...Where(char.IsDigit))`) dentro do construtor de `PropostaSeguro`. Deixei como está porque o teste tinha apenas uma entidade usando CPF — o ganho de abstrair um VO agora seria menor que o custo, mas eu sei exatamente como e por que faria isso crescer.

**P: `PropostaSeguro` é um Aggregate Root — então por que não existem outras entidades "dentro" dele (Value Objects, entidades filhas)?**
R: Porque o domínio modelado é intencionalmente simples — uma proposta de seguro, nesse escopo, não tem sub-entidades (como itens de cobertura, beneficiários, etc.). Em um domínio real de seguros isso cresceria, mas o Aggregate Root continuaria sendo o único ponto de entrada para qualquer mutação.

**P: Como você garante que ninguém consegue criar uma `PropostaSeguro` num estado inválido?**
R: O construtor público é a única forma de criar a entidade (não há `new PropostaSeguro { Status = ... }` com inicializador de objeto, pois os setters são `private`), e ele lança `DomainException` antes de atribuir qualquer campo se qualquer invariante for violada. O construtor sem parâmetros é `private` — só o EF Core consegue usá-lo via reflection para materializar do banco.

---

### 🔷 4.3 Microsserviços e Consistência de Dados

**P: O que acontece se o `PropostaService` estiver fora do ar quando alguém tentar contratar?**
R: `PropostaServiceHttpClient` captura a exceção HTTP e a converte em uma `DomainException` genérica ("Não foi possível verificar o status da proposta no momento."), retornando `400 Bad Request` ao cliente. **Limitação honesta**: não há retry automático nem circuit breaker (ex.: Polly) — uma falha transitória de rede de 1 segundo já derruba a requisição do usuário. Numa versão de produção, eu adicionaria uma política de retry com backoff exponencial e um circuit breaker para não sobrecarregar o `PropostaService` durante uma instabilidade.

**P: Isso é consistência forte ou eventual? Como você lida com o cenário: proposta é aprovada, contratação acontece, e depois alguém "desaprova" a proposta manualmente?**
R: É consistência **eventual/best-effort** no momento da contratação — o `ContratacaoService` valida o status *no instante* da chamada, mas depois disso a contratação é um fato consumado no seu próprio banco, sem reconciliação contínua com o `PropostaService`. Hoje o domínio de `PropostaSeguro` nem permite voltar de Aprovada para Em Análise (a própria entidade bloqueia isso), então o cenário de "desaprovar depois" não existe nas regras atuais. Se existisse, a solução correta seria um padrão **Saga** (coreografada ou orquestrada) com eventos de compensação, não uma simples chamada HTTP síncrona.

**P: Por que não há transação distribuída (2PC) entre os dois bancos?**
R: Por design — microsserviços com bancos independentes intencionalmente **abrem mão** de transação distribuída em favor de autonomia e disponibilidade (trade-off clássico do teorema CAP/consistência eventual). 2PC criaria acoplamento síncrono forte entre os dois bancos, o que anularia o benefício de ter serviços independentes.

**P: Como você evita que a mesma proposta seja contratada duas vezes em uma condição de corrida (duas requisições simultâneas)?**
R: A defesa real está no banco: um índice único (`IX_Contratacoes_PropostaId`) impede duas linhas com o mesmo `PropostaId`. **Ponto fraco identificado na auditoria**: o `UseCase` faz uma checagem prévia (`ObterPorPropostaIdAsync`) antes de inserir — um clássico *check-then-act* não atômico — e, se a exceção de violação de índice único (`DbUpdateException`) ocorrer numa corrida real, ela hoje **não é tratada** e retornaria um 500 em vez do esperado "Proposta já contratada." Sei exatamente como corrigir isso (capturar `DbUpdateException` no `ContratacaoRepository` e traduzir para `DomainException`) — está documentado no relatório de code review anterior.

**P: Por que `ContratacaoService` não tem sua própria cópia dos dados da proposta (nome, valor) em vez de só o `PropostaId`?**
R: Porque, para o escopo do teste, a única informação que o `ContratacaoService` precisa é "essa proposta existe e está aprovada" — e isso é validado em tempo real via HTTP. Copiar dados da proposta (nome, CPF, valor) para o banco de contratação seria um passo em direção a um modelo mais desacoplado/orientado a eventos (cada serviço mantém sua própria "vista" dos dados, sincronizada por eventos assíncronos) — um padrão válido e mais resiliente, mas fora do escopo atual.

---

### 🔷 4.4 Segurança (⚠️ prepare-se — é o ponto mais provável de questionamento)

**P: Por que as senhas do banco de dados estão em texto plano no `appsettings.json`?**
R: É uma simplificação deliberada para facilitar a execução do teste (`docker compose up` funciona sem configuração extra de segredos). Eu sei que isso não é aceitável em produção — a correção correta é usar `dotnet user-secrets` em desenvolvimento e variáveis de ambiente vindas de um cofre de segredos (Azure Key Vault, AWS Secrets Manager ou Docker Secrets) em produção, nunca commitando credenciais no `appsettings.json`.

**P: Não há autenticação em nenhum endpoint — qualquer pessoa pode consultar ou contratar qualquer proposta. Isso não é uma falha grave de segurança (BOLA — Broken Object Level Authorization)?**
R: Sim, é uma falha real se este fosse um sistema em produção — hoje não há verificação de que quem chama `GET /api/propostas/{id}` ou `POST /api/contratacoes` tem permissão sobre aquele recurso específico. O teste técnico não pedia explicitamente autenticação, mas eu reconheço a lacuna. A correção: adicionar autenticação JWT Bearer, decorar os controllers com `[Authorize]`, e — mais importante — validar no UseCase que o usuário autenticado é o dono/tem permissão sobre o `propostaId` solicitado (não basta autenticar, é preciso autorizar por recurso).

**P: Existe alguma proteção contra SQL Injection?**
R: Sim — todo acesso a dados passa por LINQ do EF Core (parametrizado automaticamente). Não há nenhuma consulta com `FromSqlRaw`/`ExecuteSqlRaw` ou concatenação de string SQL em nenhum dos dois serviços — verificado item a item na auditoria.

**P: E rate limiting / proteção contra abuso?**
R: Não implementado. `POST /api/propostas` e `POST /api/contratacoes` estão sem limite de requisição hoje. Adicionaria `Microsoft.AspNetCore.RateLimiting` (nativo do .NET 8) nas rotas de mutação.

**P: Como os erros são tratados — vocês vazam stack trace ou detalhes internos para o cliente?**
R: Não. Os `catch (DomainException ex)` nos controllers retornam apenas `ex.Message`, nunca stack trace ou exceção bruta de infraestrutura. **Mas** falta um middleware de tratamento de exceção *global* (`UseExceptionHandler`/`ProblemDetails`) para cobrir exceções não previstas (ex.: falha de conexão com o Postgres) — hoje essas cairiam no comportamento padrão do ASP.NET Core, que não vaza informação sensível mas também não é padronizado (RFC 7807).

---

### 🔷 4.5 Testes

**P: Que tipo de teste vocês escreveram e por quê?**
R: Testes unitários com xUnit + Moq, cobrindo (a) todas as regras de transição de estado de `PropostaSeguro` (a parte mais crítica do domínio) e (b) os UseCases principais (`CriarProposta`, `AlterarStatusProposta`, `ContratarProposta`), usando mocks das Ports/Out (`IPropostaRepository`, `IPropostaServiceClient`) — isso é possível *só* porque a arquitetura hexagonal isola o UseCase de qualquer dependência concreta de infraestrutura.

**P: Vocês têm testes de integração (subindo a API de verdade, com banco)?**
R: Não. `Program.cs` já expõe `public partial class Program {}` — um sinal de que a intenção de suportar `WebApplicationFactory<Program>` para testes de integração existia, mas não foi implementada por tempo. Também faltam testes para `ConsultarPropostaUseCase` e `ListarPropostasUseCase`, que são triviais mas ficaram sem cobertura.

**P: Qual a cobertura de código?**
R: Não medida formalmente (`coverlet`/`ReportGenerator` não configurados no projeto) — apenas contabilizei manualmente 17 métodos de teste (`[Fact]`/`[Theory]`) cobrindo os caminhos de sucesso e falha mais importantes do domínio e dos casos de uso.

---

### 🔷 4.6 Banco de Dados

**P: Por que PostgreSQL e não SQL Server?**
R: PostgreSQL é gratuito, roda facilmente em container (`postgres:16-alpine`, imagem leve) e tem excelente suporte via `Npgsql.EntityFrameworkCore.PostgreSQL` — sem exigir licença ou imagem pesada do SQL Server para rodar o teste localmente. O PDF permitia livre escolha.

**P: As Migrations rodam automaticamente ao subir o container — isso é uma boa prática?**
R: É prático para demonstração (`docker compose up` já entrega banco pronto), mas **não é** a prática recomendada para produção — lá, migrations deveriam rodar em um passo dedicado do pipeline de CI/CD antes do deploy da nova versão, evitando que múltiplas réplicas da API tentem migrar o schema simultaneamente na inicialização.

**P: Como vocês garantem unicidade/integridade sem depender só da aplicação?**
R: Com constraints no próprio banco — o índice único em `Contratacoes.PropostaId` é a defesa real contra dupla contratação, não apenas a checagem em memória do UseCase (embora, como admitido acima, o *tratamento* dessa exceção de banco ainda precise ser melhorado).

---

### 🔷 4.7 Docker & DevOps

**P: Por que multi-stage build no Dockerfile?**
R: Para não carregar o SDK completo (~800MB) na imagem final de produção — o estágio `build` usa `sdk:8.0` para compilar/publicar, e o estágio `final` usa apenas `aspnet:8.0` (runtime), copiando só os artefatos publicados. Isso reduz superfície de ataque e tamanho de imagem.

**P: Os containers rodam como root?**
R: Sim — hoje não há `USER` não-root definido no estágio final do Dockerfile. É uma melhoria de hardening que eu adicionaria (`RUN adduser ... && USER appuser`), mas não é um problema exclusivo deste projeto — é comum em exemplos simples de Dockerfile .NET.

**P: O `docker-compose.yml` garante que o `contratacao-api` só sobe depois que o `proposta-api` está realmente pronto para receber requisições?**
R: Parcialmente. `depends_on` com `condition: service_healthy` é usado para os **bancos** (via `pg_isready`), mas para `proposta-api` a condição é apenas `service_started` — ou seja, o container começou a rodar, não que o endpoint HTTP já responde. Isso pode causar falhas transitórias nas primeiras chamadas do `contratacao-api`. Corrigiria adicionando um `healthcheck` HTTP (`curl` no endpoint de health) na própria API.

---

### 🔷 4.8 Clean Code e Boas Práticas

**P: Como vocês evitam duplicação de código entre os dois serviços (ambos têm `DomainException`, estrutura de camadas idêntica, etc.)?**
R: Atualmente **não há** um pacote/biblioteca compartilhada (`Shared Kernel`) entre os serviços — `DomainException` é duplicada em `Proposta.Domain` e `Contratacao.Domain`, por exemplo. Isso é intencional na maioria dos casos de microsserviços de verdade: compartilhar código entre Bounded Contexts pode reintroduzir acoplamento indesejado. Mas há um limite razoável — algo genérico como uma `DomainException` base *poderia* viver em um pacote NuGet compartilhado versionado independentemente, sem violar autonomia.

**P: Por que os nomes de classes/métodos estão em português enquanto o C#/.NET é convencionalmente em inglês?**
R: Escolha estilística consciente para manter ubiquitous language (linguagem ubíqua do DDD) alinhada ao domínio de negócio em português (proposta, contratação, segurado) — evita a tradução mental constante entre "Insurance Proposal" e "proposta de seguro" ao conversar com stakeholders de negócio brasileiros. Em um time internacional, a convenção poderia mudar para inglês, mas o padrão interno se manteria consistente.

**P: Existe algum warning de compilador ou análise estática (Roslyn Analyzers) configurada?**
R: Não há `.editorconfig` nem analisadores adicionais (`Microsoft.CodeAnalysis.NetAnalyzers` customizado) configurados além do padrão do SDK. Seria uma melhoria simples de adicionar ao projeto.

---

### 🔷 4.9 Perguntas de "e se..." (cenários hipotéticos que avaliadores adoram)

**P: E se eu precisasse adicionar um terceiro microsserviço, por exemplo `PagamentoService`, que precisa saber quando uma contratação é efetivada — como você faria?**
R: Hoje seria outra chamada HTTP síncrona (`ContratacaoService` chamando `PagamentoService`), seguindo o mesmo padrão de Port/Out + Adapter. Mas esse é exatamente o ponto de virada onde eu migraria para **mensageria assíncrona** (ex.: publicar um evento `ContratacaoEfetivada` em uma fila/tópico) — porque múltiplos serviços poderiam reagir ao mesmo evento sem acoplar o `ContratacaoService` a conhecer todos os seus consumidores.

**P: Como você testaria isso em um pipeline de CI antes do deploy?**
R: `dotnet test` rodando os testes unitários existentes; adicionaria um estágio de build Docker (`docker build`) para garantir que os Dockerfiles continuam válidos; e, com testes de integração implementados, subiria os containers via `docker compose` no CI para rodar testes end-to-end antes do deploy.

**P: O sistema aguenta 1000 requisições simultâneas de criação de proposta?**
R: Estruturalmente sim — os serviços são *stateless* (sem estado em memória entre requisições), o que permite escalar horizontalmente (múltiplas réplicas atrás de um load balancer) sem mudança de código. O gargalo real seria o banco de dados único por serviço sob alta carga de escrita — nesse ponto, técnicas como read replicas, connection pooling (já implícito no Npgsql) e, em último caso, sharding entrariam em jogo. Não foi testado sob carga (não há teste de performance/k6/JMeter no projeto).

**P: Por que `ValorCobertura` é `decimal` e não `double` ou `float`?**
R: `decimal` é o tipo correto para valores monetários em C# — tem precisão exata em base 10 (evita erros de arredondamento binário que `double`/`float` introduziriam), essencial para qualquer cálculo financeiro.

---

## PARTE 5 — RESUMO "SE SÓ TIVER 2 MINUTOS"

Se o avaliador pedir um resumo de 30 segundos: **"Dois microsserviços .NET 8 independentes, cada um com seu próprio banco Postgres, comunicando-se por HTTP REST. Internamente, cada um segue Arquitetura Hexagonal com 4 camadas — Domain (regras de negócio puras, zero dependências), Application (casos de uso orquestrando via interfaces/Ports), Infrastructure (implementações concretas de EF Core e HttpClient) e Api (Controllers REST). A regra de ouro é que a dependência sempre aponta para dentro, em direção ao Domain — nunca o contrário. Testado com 17 testes unitários usando mocks nas Ports, o que só é possível justamente por causa desse isolamento."**

Se pedirem os **3 maiores pontos fortes**: isolamento real do Domain (verificável nos `.csproj`), Aggregate Roots com invariantes protegidas por construtor/métodos, e testabilidade via mocks das Ports sem precisar de banco/HTTP real.

Se pedirem as **3 maiores limitações que você já sabe**: segredos em texto plano nos configs, ausência de autenticação/autorização, e tratamento incompleto da condição de corrida na dupla contratação (a proteção do banco existe, mas a aplicação ainda não traduz a exceção de forma amigável).
