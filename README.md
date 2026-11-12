# RestaurantCopilot

[![CI](https://github.com/LucassSantoss/restaurant-copilot/actions/workflows/ci.yml/badge.svg)](https://github.com/LucassSantoss/restaurant-copilot/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0_LTS-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**Um copiloto de IA que responde, em linguagem natural, perguntas de donos de restaurante sobre o próprio negócio, consultando os dados reais de cardápio, pedidos e avaliações.**

> **English summary:** RestaurantCopilot is an ASP.NET Core (.NET 10) API where restaurant owners ask natural-language questions about their business. An AI agent built on Google Gemini uses function calling, RAG over PostgreSQL + pgvector and guarded text-to-SQL to answer from real data, citing its sources. The codebase follows Clean Architecture, is covered by unit and Testcontainers-based integration tests, and runs locally with a single `docker compose up`.

> 🚧 **Status:** em desenvolvimento. Fundação e camada de dados prontas; as próximas fases estão no [roadmap](#próximos-passos).

<!-- Demonstração (GIF ou print) será adicionada quando o endpoint /api/chat estiver pronto. -->

---

## Problema

Donos de restaurantes pequenos e médios têm dados espalhados entre plataforma de delivery, sistema de pedidos e avaliações de clientes, mas raramente têm tempo ou conhecimento técnico para cruzá-los. Perguntas simples como *"quais pratos tiveram mais reclamações este mês?"* ou *"meu faturamento caiu em relação à semana passada?"* exigem planilhas, filtros e leitura manual de dezenas de comentários.

O RestaurantCopilot transforma essas perguntas em consultas aos dados reais e devolve uma resposta direta, com as fontes que a embasaram.

## Funcionalidades

| Funcionalidade | Descrição | Status |
|---|---|---|
| Análise de avaliações | Classifica sentimento e temas de cada avaliação com saída JSON estruturada do LLM; resumo por período em `/api/reviews/insights` | ✅ Pronto |
| RAG | Embeddings do cardápio e das avaliações no PostgreSQL com pgvector (índice HNSW) e busca semântica em `/api/search` | ✅ Pronto |
| Agente com ferramentas | `POST /api/chat`: o LLM decide quando consultar vendas, avaliações, busca semântica ou relatórios, e responde citando as fontes | ✅ Pronto |
| Texto para SQL | Ferramenta `query_database`: perguntas analíticas viram SQL validado por parser, executado com role **somente leitura** e timeout | ✅ Pronto |
| Cache de respostas | Redis para reduzir latência e consumo da cota gratuita | Planejado (Fase 8) |
| Cardápio | `GET /api/menu` com filtros por categoria e disponibilidade | ✅ Pronto |
| Dados fictícios | 14 semanas de pedidos e avaliações de um restaurante fictício, carregados automaticamente | ✅ Pronto |
| Integração com LLM | Gemini atrás de `ILlmClient`/`IEmbeddingService`, com retry para `429` e timeouts | ✅ Pronto |
| Health check | `/health` com o estado do PostgreSQL, do Redis e do Gemini | ✅ Pronto |

## Arquitetura

O backend segue **Clean Architecture**: as dependências sempre apontam para dentro, e as integrações externas ficam atrás de interfaces definidas pela camada de aplicação.

```
Api  →  Infrastructure  →  Application  →  Domain
```

| Camada | O que contém |
|---|---|
| `Domain` | Entidades e regras de negócio puras, sem dependências externas |
| `Application` | Casos de uso (commands/queries), DTOs e interfaces (ports) como `ILlmClient` e `IVectorStore` |
| `Infrastructure` | Implementações: EF Core/PostgreSQL, pgvector, Redis, cliente Gemini |
| `Api` | Endpoints, injeção de dependência, middlewares e configuração |

**Troca de provedor de LLM:** a aplicação depende só de `ILlmClient` e `IEmbeddingService`, definidos em `Application`. O `GeminiClient` é apenas uma implementação em `Infrastructure`; usar Groq, Ollama ou OpenAI significa escrever outra implementação e trocar uma linha no registro de dependências, sem tocar em casos de uso ou domínio ([ADR 0004](docs/adr/0004-gemini-behind-llm-port.md)).

Essa regra não é só convenção: **testes de arquitetura** (`NetArchTest`) quebram o build se uma camada depender de algo que não deveria. Detalhes e trade-offs em [ADR 0001](docs/adr/0001-clean-architecture.md).

**Diagramas:** serão adicionados em [`docs/diagrams/`](docs/diagrams/) (Mermaid).

### Estrutura do repositório

```
├── backend/
│   ├── src/
│   │   ├── RestaurantCopilot.Domain/
│   │   ├── RestaurantCopilot.Application/
│   │   ├── RestaurantCopilot.Infrastructure/
│   │   └── RestaurantCopilot.Api/
│   ├── tests/                      # um projeto de testes por camada
│   └── Dockerfile
├── docs/
│   ├── adr/                        # Architecture Decision Records
│   └── diagrams/                   # diagramas Mermaid
├── seed/                           # gerador e dados fictícios (JSON)
├── docker/postgres/initdb/         # cria o login somente leitura do texto para SQL
├── docker-compose.yml              # API + PostgreSQL (pgvector) + Redis
└── .github/workflows/ci.yml
```

## Stack

| Camada | Tecnologia | Por quê |
|---|---|---|
| API | .NET 10 (LTS), ASP.NET Core | Suporte de longo prazo, performance e tipagem forte |
| Arquitetura | Clean Architecture | Isola regras de negócio de provedores que mudam ([ADR 0001](docs/adr/0001-clean-architecture.md)) |
| Banco | PostgreSQL 18 + pgvector | Dados relacionais e vetoriais no mesmo banco, sem um serviço extra ([ADR 0006](docs/adr/0006-rag-with-pgvector.md)) |
| ORM | EF Core 10 + Npgsql, convenção snake_case | Migrations versionadas; nomes no padrão do PostgreSQL, legíveis também para o SQL gerado pelo LLM |
| Casos de uso | Handlers próprios (CQRS leve) + Scrutor | Sem dependência com licença comercial ([ADR 0002](docs/adr/0002-own-cqrs-handlers.md)) |
| IA | Google Gemini (plano gratuito), REST API | Texto, JSON estruturado, function calling e embeddings sem custo ([ADR 0004](docs/adr/0004-gemini-behind-llm-port.md)) |
| Resiliência | Microsoft.Extensions.Http.Resilience (Polly) | Retry com backoff para o limite do plano gratuito, timeouts e circuit breaker |
| Cache | Redis 8 | Cache de respostas do agente |
| Logs | Serilog | Logs estruturados configuráveis por `appsettings` |
| Documentação da API | OpenAPI + Scalar | Documento OpenAPI nativo do .NET com uma UI moderna |
| Testes | xUnit v3, Shouldly, NSubstitute, Testcontainers | Testes de integração contra PostgreSQL e Redis reais |
| Infra local / CI | Docker Compose, GitHub Actions | Ambiente reproduzível com um comando; build, lint e testes em todo push e PR |

## Como rodar

**Pré-requisitos:** Docker. Para desenvolver fora do container: .NET SDK 10.

```bash
cp .env.example .env          # ajuste os valores se quiser
docker compose up -d --build
curl http://localhost:8080/health
curl http://localhost:8080/api/menu
```

Na primeira subida, o PostgreSQL cria o login somente leitura (`docker/postgres/initdb`), e a API aplica as migrations e carrega os dados fictícios de [`seed/`](seed/) (só em banco vazio). Para recriar do zero: `docker compose down -v`.

As funcionalidades de IA precisam de uma chave do Gemini, gerada gratuitamente no [Google AI Studio](https://aistudio.google.com/apikey). No Docker, coloque-a em `GEMINI_API_KEY` no `.env`. Sem a chave a API sobe normalmente e o `/health` mostra o Gemini como `Degraded`.

Os modelos são configuráveis (`Gemini__GenerationModel`, `Gemini__EmbeddingModel`); os padrões estão em [`appsettings.json`](backend/src/RestaurantCopilot.Api/appsettings.json).

### Rodando a API localmente (sem container)

```bash
docker compose up -d postgres redis
dotnet user-secrets set "Gemini:ApiKey" "<sua-chave>" --project backend/src/RestaurantCopilot.Api
dotnet run --project backend/src/RestaurantCopilot.Api
```

- API: http://localhost:5080
- Documentação interativa (Scalar): http://localhost:5080/scalar
- OpenAPI: http://localhost:5080/openapi/v1.json

## Exemplos de requisições

```http
POST /api/chat
Content-Type: application/json

{ "question": "Qual foi meu faturamento na última semana comparado à anterior?" }
```

```json
{
  "answer": "Na última semana (02/11 a 08/11/2026) você faturou R$ 13.891,90 em 122 pedidos, contra R$ 11.971,00 em 100 pedidos na semana anterior (26/10 a 01/11): alta de 16,0%. ...",
  "sources": [
    { "kind": "tool", "reference": "search_sales", "description": "Vendas de 02/11/2026 a 08/11/2026" },
    { "kind": "tool", "reference": "search_sales", "description": "Vendas de 26/10/2026 a 01/11/2026" }
  ],
  "toolCalls": [
    { "tool": "search_sales", "arguments": { "period_start": "2026-11-02", "period_end": "2026-11-08" }, "succeeded": true, "error": null },
    { "tool": "search_sales", "arguments": { "period_start": "2026-10-26", "period_end": "2026-11-01" }, "succeeded": true, "error": null }
  ],
  "usage": { "inputTokens": 4210, "outputTokens": 180 }
}
```

> Os números são os do seed; a redação da resposta varia conforme o modelo.

```http
GET /api/menu?category=Dessert
```

```json
[
  {
    "id": "01a1cd1d-f94b-7901-9967-a09832d37846",
    "name": "Cannoli Siciliano",
    "description": "Massa crocante recheada com ricota doce (2 unidades).",
    "category": "Dessert",
    "price": 26.90,
    "isAvailable": true
  }
]
```

Categorias: `Starter`, `Pizza`, `Pasta`, `Risotto`, `MainCourse`, `Dessert`, `Drink`. Uma categoria desconhecida retorna `400` com um corpo [Problem Details](https://www.rfc-editor.org/rfc/rfc9457).

```http
POST /api/reviews
Content-Type: application/json

{ "orderNumber": 1001, "rating": 1, "comment": "A lasanha chegou toda vazada, molho espalhado na sacola." }
```

```json
{
  "id": "019a7c3e-5d1b-7a20-9b4e-2f1c8d6e4a10",
  "orderNumber": 1001,
  "menuItemId": null,
  "rating": 1,
  "comment": "A lasanha chegou toda vazada, molho espalhado na sacola.",
  "createdAt": "2026-11-11T18:20:00+00:00",
  "classification": { "sentiment": "Negative", "themes": ["Packaging"] }
}
```

Se o Gemini estiver indisponível, a avaliação é salva com `"classification": null` e classificada depois por `POST /api/ingestion/classify-reviews` (que também classifica as avaliações do seed).

```http
GET /api/reviews/insights?from=2026-10-01&to=2026-10-31
```

```json
{
  "from": "2026-10-01",
  "to": "2026-10-31",
  "totalReviews": 65,
  "classifiedReviews": 65,
  "averageRating": 3.62,
  "sentiment": { "positive": 34, "neutral": 8, "negative": 23 },
  "themes": [
    { "theme": "Taste", "mentions": 30, "negative": 7 },
    { "theme": "Packaging", "mentions": 15, "negative": 13 }
  ],
  "mostComplainedDishes": [
    { "menuItemId": "019a7c3e-...", "name": "Lasanha à Bolonhesa", "negativeReviews": 9 }
  ]
}
```

> Total e média vêm do seed; sentimento, temas e pratos dependem da classificação feita pelo modelo.

```http
POST /api/ingestion/reindex
```

```json
{ "menuItems": 24, "reviews": 215 }
```

```http
GET /api/search?query=problemas com a embalagem&source=Review&limit=2
```

```json
[
  {
    "source": "Review",
    "sourceId": "019a7c3e-...",
    "content": "Avaliação de cliente, nota 1/5, em quarta-feira, 28/10/2026, sobre o pedido: \"Embalagem frágil, a tampa abriu no caminho e sujou tudo.\"",
    "similarity": 0.8123
  }
]
```

```http
GET /health
```

```json
{
  "status": "Healthy",
  "durationMs": 71.5,
  "checks": {
    "postgres": { "status": "Healthy", "description": null, "durationMs": 69.9 },
    "redis": { "status": "Healthy", "description": null, "durationMs": 49.1 },
    "gemini": { "status": "Healthy", "description": "Model gemini-2.5-flash is reachable.", "durationMs": 180.2 }
  }
}
```

Retorna `503 Service Unavailable` quando o PostgreSQL ou o Redis estão fora do ar, indicando qual. Problemas no Gemini deixam o status `Degraded` (HTTP 200), porque o resto da API continua funcionando.

## Testes e CI

```bash
cd backend
dotnet test
```

Os testes rodam na **Microsoft.Testing.Platform** com xUnit v3. Os testes de integração sobem PostgreSQL e Redis reais com **Testcontainers**, então é preciso ter o Docker rodando.

| Projeto | Cobre |
|---|---|
| `RestaurantCopilot.Domain.Tests` | Invariantes das entidades e value objects (`Money`, `Rating`, `Order`, `Review`...) |
| `RestaurantCopilot.Application.Tests` | Casos de uso e saída estruturada do LLM com dublês (NSubstitute) |
| `RestaurantCopilot.Infrastructure.Tests` | Repositórios, mapeamentos do EF Core, seed e busca vetorial (pgvector) contra PostgreSQL real; guard de SQL (dezenas de tentativas de escrita, injeção e acesso a catálogos) e executor somente leitura, que bloqueia escrita e consultas longas mesmo sem o guard; cliente Gemini (formato das requisições, function calling, erros, retry de `429`) com um `HttpMessageHandler` falso, sem rede |
| `RestaurantCopilot.Api.Tests` | Regras de dependência entre camadas e endpoints de ponta a ponta (ex.: `/api/menu`, `/health` com Redis fora do ar, reindex + busca semântica com um embedding determinístico no lugar do Gemini) |

O [workflow de CI](.github/workflows/ci.yml) roda em todo push na `main` e em pull requests: restore, `dotnet format --verify-no-changes`, build em Release com warnings tratados como erros, testes e build da imagem Docker.

## Resultados da avaliação do agente

Planejado (Fase 9): um conjunto de perguntas com respostas esperadas sobre os dados fictícios do seed e um runner que mede a taxa de acerto do agente.

## Decisões técnicas e trade-offs

- **Warnings como erros e analyzers no nível `latest-recommended`** nos projetos de produção: o build falha antes de o problema chegar ao code review.
- **Versões de pacotes centralizadas** em `Directory.Packages.props`: uma única fonte de verdade para dependências.
- **Imagem Docker sem root:** a API roda com o usuário `app` da imagem oficial da Microsoft.
- **Erros de negócio como valores:** factories e casos de uso retornam `Result<T>`; a API traduz o tipo do erro em status HTTP (400/404/409) com Problem Details. Exceções ficam para o que é realmente excepcional.
- **Seed determinístico e versionado** ([ADR 0003](docs/adr/0003-deterministic-seed-data.md)): os mesmos dados alimentam demo, testes e avaliação do agente, e todo registro passa pelas regras do domínio.
- **Limites do plano gratuito do Gemini:** chamadas com retry exponencial e jitter (respeitando `Retry-After`), timeout por tentativa e total, e erros explícitos (`Llm.RateLimited`) quando as tentativas acabam. Os testes nunca chamam a API real.
- **Classificação que não perde dados:** se o LLM falhar ao cadastrar uma avaliação, ela é salva como pendente e classificada depois, em lote ([ADR 0005](docs/adr/0005-review-classification.md)).
- **Prompt injection em avaliações:** o texto do cliente vai como dado JSON e o classificador não tem ferramentas; no pior caso, uma avaliação é mal classificada.
- **RAG sem serviço extra:** vetores no próprio PostgreSQL (pgvector + HNSW), reindex atômico e checagem de dimensão antes de gastar cota ([ADR 0006](docs/adr/0006-rag-with-pgvector.md)).
- **Agente limitado e tolerante a erros:** no máximo 6 turnos com ferramentas (depois, uma resposta forçada sem ferramentas), timeout total, e erros de ferramenta devolvidos ao modelo para que ele se corrija ([ADR 0007](docs/adr/0007-agent-function-calling.md)).
- **Números vêm de ferramentas, não do modelo:** o prompt proíbe inventar dados, e cada resposta traz as ferramentas e documentos usados.
- **Texto para SQL em três camadas:** guard com parser de PostgreSQL (um único `SELECT`, sem comentários, sem catálogos), role do banco que só lê as tabelas de negócio e transação `READ ONLY` com timeout e limite de linhas. Cada camada sozinha já impede escrita ([ADR 0008](docs/adr/0008-text-to-sql-defense-in-depth.md)).
- **Enums gravados como texto** no banco (`'Delivery'` em vez de `0`): consultas, inclusive as geradas pelo LLM, ficam legíveis sem tabela de-para.

As decisões completas estão nos [ADRs](docs/adr/).

## Próximos passos

1. ~~Fundação: solution, Docker Compose, CI, health check~~ ✅
2. ~~Domínio e dados: entidades, EF Core, migrations e seed fictício~~ ✅
3. ~~Integração com o Gemini atrás de `ILlmClient`, com resiliência~~ ✅
4. ~~Análise de avaliações e endpoint de insights~~ ✅
5. ~~RAG com pgvector~~ ✅
6. ~~Agente com function calling e `/api/chat`~~ ✅
7. ~~Texto para SQL com validação e usuário somente leitura~~ ✅
8. Cache com Redis
9. Avaliação do agente
10. Documentação final e diagramas
11. Frontend (React + TypeScript + Vite)

## Licença

[MIT](LICENSE)
