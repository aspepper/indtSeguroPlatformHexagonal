# Plataforma de Seguros — Arquitetura Hexagonal (.NET 8)

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Enabled-2496ED?logo=docker)](https://www.docker.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Hexagonal%20%7C%20DDD-blue)](https://en.wikipedia.org/wiki/Hexagonal_architecture_(software))

Bem-vindo ao projeto **Plataforma de Seguros**, desenvolvido por **Alex Pimenta** como uma demonstração prática e robusta de microsserviços modernos em **.NET 8** construídos sob os princípios de **Arquitetura Hexagonal (Ports & Adapters)**, **Domain-Driven Design (DDD)**, **Clean Code** e **SOLID**.

O sistema resolve a gestão completa do ciclo de vida de propostas de seguro e a posterior efetivação da contratação de apólices, dividindo as responsabilidades em dois microsserviços autônomos e desacoplados:
- **`PropostaService`**: Responsável pela criação, consulta e transições de estado das propostas (Em Análise, Aprovada, Rejeitada).
- **`ContratacaoService`**: Responsável por validar as pré-condições da proposta aprovada e efetivar a contratação da apólice de seguro.

---

## 🏛️ Arquitetura e Estrutura dos Serviços

Cada microsserviço segue rigorosamente a divisão em 4 camadas com fluxo de dependências invertido apontando para o núcleo de domínio (`Domain <- Application <- Infrastructure / Api`):

- **`Domain`**: Entidades ricas (`PropostaSeguro`, `Contratacao`), Value Objects, Enums e Exceções de negócio (`DomainException`). **Zero dependências externas**.
- **`Application`**: Casos de uso (`UseCases`), DTOs, Mappers e a definição dos contratos das portas (`Ports/In` para os casos de uso e `Ports/Out` para repositórios e serviços externos).
- **`Infrastructure`**: Adaptadores concretos de saída (`Driven Adapters`) implementando persistência com **EF Core / PostgreSQL** e comunicação HTTP remota com `HttpClient`.
- **`Api`**: Adaptador de entrada (`Driving Adapter`) com Controllers REST e a raiz de composição (`Composition Root` no `Program.cs`).

```mermaid
graph TD
    subgraph Client ["Cliente Externo / Swagger / Frontend"]
        HTTP_REQ["Requisição HTTP REST"]
    end

    subgraph PropostaService ["PropostaService (Porta 8081)"]
        P_API["Proposta.Api (Controller)"]
        P_PIN["Ports/In (ICriarProposta, IAlterarStatus)"]
        P_UC["UseCases (Orquestração Pura)"]
        P_DOM["Proposta.Domain (PropostaSeguro Aggregate)"]
        P_POUT["Ports/Out (IPropostaRepository)"]
        P_INFRA["Proposta.Infrastructure (EF Core Adapter)"]
    end

    subgraph ContratacaoService ["ContratacaoService (Porta 8082)"]
        C_API["Contratacao.Api (Controller)"]
        C_PIN["Ports/In (IContratarProposta)"]
        C_UC["UseCases (ContratarPropostaUseCase)"]
        C_DOM["Contratacao.Domain (Contratacao Aggregate)"]
        C_POUT_REPO["Ports/Out (IContratacaoRepository)"]
        C_POUT_HTTP["Ports/Out (IPropostaServiceClient)"]
        C_INFRA_REPO["Contratacao.Infrastructure (EF Core Adapter)"]
        C_INFRA_HTTP["PropostaServiceHttpClient (HttpClient Typed Adapter)"]
    end

    subgraph Databases ["Bancos de Dados Relacionais"]
        DB_PROP[("proposta_db (PostgreSQL)")]
        DB_CONT[("contratacao_db (PostgreSQL)")]
    end

    HTTP_REQ -->|POST/GET/PATCH| P_API
    HTTP_REQ -->|POST /api/contratacoes| C_API

    P_API --> P_PIN
    P_PIN --> P_UC
    P_UC --> P_DOM
    P_UC --> P_POUT
    P_INFRA -.->|Implementa| P_POUT
    P_INFRA --> DB_PROP

    C_API --> C_PIN
    C_PIN --> C_UC
    C_UC --> C_DOM
    C_UC --> C_POUT_REPO
    C_UC --> C_POUT_HTTP
    C_INFRA_REPO -.->|Implementa| C_POUT_REPO
    C_INFRA_HTTP -.->|Implementa| C_POUT_HTTP
    C_INFRA_REPO --> DB_CONT
    C_INFRA_HTTP -->|GET /api/propostas/{id}| P_API
```

---

## 💡 Por que Arquitetura Hexagonal? (Decisão de Arquitetura)

A escolha da Arquitetura Hexagonal (Ports & Adapters) para esta plataforma foi tomada com foco na **manutenibilidade de longo prazo** e na **independência de infraestrutura**:

1. **Proteção do Core de Negócio**: As validações e invariantes (como a obrigatoriedade do CPF com 11 dígitos, valor de cobertura positivo e transições proibidas de status) residem exclusivamente dentro das entidades do `Domain`. Nem a Web API nem o repositório conseguem violar essas regras.
2. **Isolamento de Comunicação entre Serviços**: O `ContratacaoService` consulta a proposta através da interface `IPropostaServiceClient` (`Port/Out`). Para a camada de aplicação, é irrelevante se essa chamada utiliza HTTP REST, gRPC ou mensagem assíncrona. Se no futuro migrarmos a comunicação para RabbitMQ/Kafka, alteramos apenas o adaptador na `Infrastructure`, mantendo o Use Case intacto.
3. **Substituição Transparente de Recursos**: O banco de dados PostgreSQL é um detalhe de infraestrutura plugável por trás da porta `IPropostaRepository`. Testes unitários conseguem mockar essa porta sem precisar de um banco de dados real.

---

## 🚀 Instruções de Execução

### 1. Execução via Docker Compose (Recomendado)

Certifique-se de ter o Docker e Docker Compose instalados. Na raiz do repositório, execute:

```bash
docker compose up -d --build
```

Os serviços e documentações interativas (Swagger) estarão disponíveis nos seguintes endereços:

- **PropostaService**: [http://localhost:8081/swagger](http://localhost:8081/swagger)
- **ContratacaoService**: [http://localhost:8082/swagger](http://localhost:8082/swagger)

Para parar os containers:
```bash
docker compose down -v
```

---

### 2. Execução Local (sem Docker)

#### Pré-requisitos:
- Instância do PostgreSQL rodando localmente (porta 5432) com usuário `postgres` e senha `postgres`.
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

#### Passos:
1. **Restaurar e Compilar a Solução**:
   ```bash
   dotnet restore
   dotnet build
   ```

2. **Aplicar as Migrations nos Bancos de Dados**:
   ```bash
   dotnet ef database update --project PropostaService/src/Proposta.Infrastructure --startup-project PropostaService/src/Proposta.Api
   dotnet ef database update --project ContratacaoService/src/Contratacao.Infrastructure --startup-project ContratacaoService/src/Contratacao.Api
   ```

3. **Executar os Microsserviços**:
   Em um terminal, inicie a API de Propostas:
   ```bash
   dotnet run --project PropostaService/src/Proposta.Api
   ```
   Em outro terminal, inicie a API de Contratação:
   ```bash
   dotnet run --project ContratacaoService/src/Contratacao.Api
   ```

---

### 3. Execução da Suíte de Testes Unitários

Para rodar todos os testes unitários da solução (cobertura de entidades, invariantes de domínio e casos de uso mockados com Moq):

```bash
dotnet test
```

---

## 📋 Exemplos de Requisições e Respostas (Endpoints API)

### 1. Criar Proposta (`POST /api/propostas`)

**Requisição**:
`POST http://localhost:8081/api/propostas`
```json
{
  "nomeSegurado": "Alex Pimenta",
  "cpfSegurado": "12345678901",
  "valorCobertura": 150000.00
}
```

**Resposta (`201 Created`)**:
```json
{
  "id": "bd621fc7-fe89-4169-b25a-710f5223e769",
  "nomeSegurado": "Alex Pimenta",
  "cpfSegurado": "12345678901",
  "valorCobertura": 150000.00,
  "status": "EmAnalise",
  "dataCriacao": "2026-09-01T17:53:43.4189242Z",
  "dataAtualizacao": null
}
```

---

### 2. Aprovar Proposta (`PATCH /api/propostas/{id}/status`)

**Requisição**:
`PATCH http://localhost:8081/api/propostas/bd621fc7-fe89-4169-b25a-710f5223e769/status`
```json
{
  "novoStatus": 2
}
```
*(Nota: StatusEnum — `1 = EmAnalise`, `2 = Aprovada`, `3 = Rejeitada`)*

**Resposta (`200 OK`)**:
```json
{
  "id": "bd621fc7-fe89-4169-b25a-710f5223e769",
  "nomeSegurado": "Alex Pimenta",
  "cpfSegurado": "12345678901",
  "valorCobertura": 150000.00,
  "status": "Aprovada",
  "dataCriacao": "2026-09-01T17:53:43.418924Z",
  "dataAtualizacao": "2026-09-01T17:53:43.7610399Z"
}
```

---

### 3. Efetivar Contratação (`POST /api/contratacoes`)

**Requisição**:
`POST http://localhost:8082/api/contratacoes`
```json
{
  "propostaId": "bd621fc7-fe89-4169-b25a-710f5223e769"
}
```

**Resposta Sucesso (`201 Created`)**:
```json
{
  "id": "8da469b0-9601-49b0-b812-00c8185071c9",
  "propostaId": "bd621fc7-fe89-4169-b25a-710f5223e769",
  "dataContratacao": "2026-09-01T17:53:44.0579869Z"
}
```

**Resposta Tentativa Duplicada (`400 Bad Request`)**:
```json
{
  "erro": "Proposta já contratada."
}
```

---

## 🧪 Testes via Terminal / CMD e Inspeção dos Bancos de Dados

### 1. Execução de Testes End-to-End com `curl`

Para testar o fluxo completo da aplicação (Criar Proposta → Aprovar Proposta → Efetivar Contratação → Validar Bloqueio de Duplicidade), utilize os scripts abaixo de acordo com o seu sistema operacional.

#### Linux / macOS (Bash)
Crie um arquivo chamado `testar_api.sh` na raiz do projeto:

```bash
#!/bin/bash

# Cores para output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${BLUE}====================================================${NC}"
echo -e "${BLUE}    Iniciando Testes da Plataforma de Seguros${NC}"
echo -e "${BLUE}====================================================${NC}\n"

# 1. Criar Proposta no PropostaService (Porta 8081)
echo -e "${GREEN}[1/4] Criando nova proposta...${NC}"
RESP_CRIAR=$(curl -s -X POST http://localhost:8081/api/propostas \
  -H "Content-Type: application/json" \
  -d '{
    "nomeSegurado": "Alex Pimenta",
    "cpfSegurado": "12345678901",
    "valorCobertura": 150000.00
  }')

echo -e "Resposta: $RESP_CRIAR\n"

# Extrair ID da proposta
PROPOSTA_ID=$(echo $RESP_CRIAR \vert{} grep -o '"id":"[^"]*' \vert{} grep -o '[^"]*$')

if [ -z "$PROPOSTA_ID" ] \vert{}\vert{} [ "$PROPOSTA_ID" == "null" ]; then
    echo -e "${RED}Erro ao obter o ID da proposta. Verifique se os containers estão rodando.${NC}"
    exit 1
fi

# 2. Aprovar a Proposta (Status 2 = Aprovada)
echo -e "${GREEN}[2/4] Aprovando a proposta (ID: ${PROPOSTA_ID})...${NC}"
RESP_APROVAR=$(curl -s -X PATCH "http://localhost:8081/api/propostas/${PROPOSTA_ID}/status" \
  -H "Content-Type: application/json" \
  -d '{ "novoStatus": 2 }')

echo -e "Resposta: $RESP_APROVAR\n"

# 3. Efetivar Contratação no ContratacaoService (Porta 8082)
echo -e "${GREEN}[3/4] Efetivando contratação da apólice...${NC}"
RESP_CONTRATAR=$(curl -s -X POST http://localhost:8082/api/contratacoes \
  -H "Content-Type: application/json" \
  -d "{\"propostaId\": \"${PROPOSTA_ID}\"}")

echo -e "Resposta: $RESP_CONTRATAR\n"

# 4. Validar Regra de Negócio: Tentativa Duplicada
echo -e "${GREEN}[4/4] Testando tentativa de contratação duplicada...${NC}"
RESP_DUPLICADO=$(curl -s -X POST http://localhost:8082/api/contratacoes \
  -H "Content-Type: application/json" \
  -d "{\"propostaId\": \"${PROPOSTA_ID}\"}")

echo -e "Resposta (Erro Esperado 400): $RESP_DUPLICADO\n"
echo -e "${BLUE}====================================================${NC}"
echo -e "${BLUE}             Testes Concluídos!                     ${NC}"
echo -e "${BLUE}====================================================${NC}"
```

---

## ✒️ Autor

**Alex Pimenta**  
Arquiteto de Software & Engenheiro de Sistemas
