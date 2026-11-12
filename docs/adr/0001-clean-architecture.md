# 0001. Clean Architecture com quatro camadas

- **Status:** Aceito
- **Data:** 2026-11-11

## Contexto

O RestaurantCopilot combina regras de negócio simples (cardápio, pedidos, avaliações) com várias integrações externas que tendem a mudar: o provedor de LLM (hoje Google Gemini no plano gratuito), o banco vetorial (pgvector), o cache (Redis). Queremos:

- trocar o provedor de LLM sem reescrever casos de uso;
- testar os casos de uso sem rede, banco ou chave de API;
- deixar óbvio, para quem chega ao repositório, onde cada tipo de código mora.

## Decisão

Organizar o backend em quatro projetos, com dependências sempre apontando para dentro:

```
Api  →  Infrastructure  →  Application  →  Domain
```

| Camada | Responsabilidade | Pode depender de |
|---|---|---|
| `Domain` | Entidades, value objects e regras de negócio puras | nada |
| `Application` | Casos de uso (commands/queries), DTOs e **interfaces (ports)** como `ILlmClient`, `IVectorStore`, `ICacheService` | `Domain` |
| `Infrastructure` | Implementações dos ports: EF Core/PostgreSQL, pgvector, Redis, cliente Gemini | `Application`, `Domain` |
| `Api` | Endpoints HTTP, composição da injeção de dependência, middlewares | todas (composition root) |

A regra é verificada automaticamente por testes de arquitetura (`NetArchTest`) em `RestaurantCopilot.Api.Tests/Architecture`. Eles também impedem que `Application` referencie EF Core, Npgsql, Redis ou ASP.NET Core diretamente.

## Consequências

- **Positivas:** trocar o Gemini por outro provedor (Groq, Ollama) exige apenas uma nova implementação de `ILlmClient` em `Infrastructure`. Casos de uso são testados com dublês (NSubstitute). A violação da regra de dependência quebra o build no CI.
- **Negativas:** mais projetos e mais mapeamento entre camadas do que uma API "em um projeto só". Para o tamanho deste projeto, o custo é aceitável porque o objetivo também é demonstrar uma base que escala.
