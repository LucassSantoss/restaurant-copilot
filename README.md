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
| Análise de avaliações | Classifica sentimento e tema de cada avaliação com saída JSON estruturada do LLM | Planejado (Fase 4) |
| RAG | Embeddings do cardápio e das avaliações no PostgreSQL com pgvector | Planejado (Fase 5) |
| Agente com ferramentas | O LLM decide quando consultar vendas, avaliações ou fazer busca semântica | Planejado (Fase 6) |
| Texto para SQL | Perguntas analíticas viram SQL **somente leitura** e validado | Planejado (Fase 7) |
| Cache de respostas | Redis para reduzir latência e consumo da cota gratuita | Planejado (Fase 8) |
| Cardápio | `GET /api/menu` com filtros por categoria e disponibilidade | ✅ Pronto |
| Dados fictícios | 14 semanas de pedidos e avaliações de um restaurante fictício, carregados automaticamente | ✅ Pronto |
| Health check | `/health` com o estado do PostgreSQL e do Redis | ✅ Pronto |

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
├── docker-compose.yml              # API + PostgreSQL (pgvector) + Redis
└── .github/workflows/ci.yml
```

## Stack

| Camada | Tecnologia | Por quê |
|---|---|---|
| API | .NET 10 (LTS), ASP.NET Core | Suporte de longo prazo, performance e tipagem forte |
| Arquitetura | Clean Architecture | Isola regras de negócio de provedores que mudam ([ADR 0001](docs/adr/0001-clean-architecture.md)) |
| Banco | PostgreSQL 18 + pgvector | Dados relacionais e vetoriais no mesmo banco, sem um serviço extra |
| ORM | EF Core 10 + Npgsql, convenção snake_case | Migrations versionadas; nomes no padrão do PostgreSQL, legíveis também para o SQL gerado pelo LLM |
| Casos de uso | Handlers próprios (CQRS leve) + Scrutor | Sem dependência com licença comercial ([ADR 0002](docs/adr/0002-own-cqrs-handlers.md)) |
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

Na primeira subida, a API aplica as migrations e carrega os dados fictícios de [`seed/`](seed/) (só em banco vazio). Para recriar do zero: `docker compose down -v`.

A chave do Gemini (`GEMINI_API_KEY` no `.env`) será necessária a partir das funcionalidades de IA. Ela pode ser gerada gratuitamente no [Google AI Studio](https://aistudio.google.com/apikey).

### Rodando a API localmente (sem container)

```bash
docker compose up -d postgres redis
dotnet run --project backend/src/RestaurantCopilot.Api
```

- API: http://localhost:5080
- Documentação interativa (Scalar): http://localhost:5080/scalar
- OpenAPI: http://localhost:5080/openapi/v1.json

## Exemplos de requisições

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
GET /health
```

```json
{
  "status": "Healthy",
  "durationMs": 71.5,
  "checks": {
    "postgres": { "status": "Healthy", "description": null, "durationMs": 69.9 },
    "redis": { "status": "Healthy", "description": null, "durationMs": 49.1 }
  }
}
```

Retorna `503 Service Unavailable` quando alguma dependência está fora do ar, indicando qual.

## Testes e CI

```bash
cd backend
dotnet test
```

Os testes rodam na **Microsoft.Testing.Platform** com xUnit v3. Os testes de integração sobem PostgreSQL e Redis reais com **Testcontainers**, então é preciso ter o Docker rodando.

| Projeto | Cobre |
|---|---|
| `RestaurantCopilot.Domain.Tests` | Invariantes das entidades e value objects (`Money`, `Rating`, `Order`, `Review`...) |
| `RestaurantCopilot.Application.Tests` | Casos de uso com dublês (NSubstitute) |
| `RestaurantCopilot.Infrastructure.Tests` | Repositórios, mapeamentos do EF Core e seed contra PostgreSQL real |
| `RestaurantCopilot.Api.Tests` | Regras de dependência entre camadas e endpoints de ponta a ponta (ex.: `/api/menu`, `/health` com Redis fora do ar) |

O [workflow de CI](.github/workflows/ci.yml) roda em todo push na `main` e em pull requests: restore, `dotnet format --verify-no-changes`, build em Release com warnings tratados como erros, testes e build da imagem Docker.

## Resultados da avaliação do agente

Planejado (Fase 9): um conjunto de perguntas com respostas esperadas sobre os dados fictícios do seed e um runner que mede a taxa de acerto do agente.

## Decisões técnicas e trade-offs

- **Warnings como erros e analyzers no nível `latest-recommended`** nos projetos de produção: o build falha antes de o problema chegar ao code review.
- **Versões de pacotes centralizadas** em `Directory.Packages.props`: uma única fonte de verdade para dependências.
- **Imagem Docker sem root:** a API roda com o usuário `app` da imagem oficial da Microsoft.
- **Erros de negócio como valores:** factories e casos de uso retornam `Result<T>`; a API traduz o tipo do erro em status HTTP (400/404/409) com Problem Details. Exceções ficam para o que é realmente excepcional.
- **Seed determinístico e versionado** ([ADR 0003](docs/adr/0003-deterministic-seed-data.md)): os mesmos dados alimentam demo, testes e avaliação do agente, e todo registro passa pelas regras do domínio.
- **Enums gravados como texto** no banco (`'Delivery'` em vez de `0`): consultas, inclusive as geradas pelo LLM, ficam legíveis sem tabela de-para.

As decisões completas estão nos [ADRs](docs/adr/).

## Próximos passos

1. ~~Fundação: solution, Docker Compose, CI, health check~~ ✅
2. ~~Domínio e dados: entidades, EF Core, migrations e seed fictício~~ ✅
3. Integração com o Gemini atrás de `ILlmClient`, com resiliência
4. Análise de avaliações e endpoint de insights
5. RAG com pgvector
6. Agente com function calling e `/api/chat`
7. Texto para SQL com validação e usuário somente leitura
8. Cache com Redis
9. Avaliação do agente
10. Documentação final e diagramas
11. Frontend (React + TypeScript + Vite)

## Licença

[MIT](LICENSE)
