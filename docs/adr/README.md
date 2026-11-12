# Architecture Decision Records

Cada decisão técnica relevante do projeto é registrada aqui como um ADR curto: contexto, decisão e consequências. ADRs não são editados depois de aceitos; se uma decisão mudar, um novo ADR substitui o anterior.

| # | Decisão | Status |
|---|---|---|
| [0001](0001-clean-architecture.md) | Clean Architecture com quatro camadas | Aceito |
| [0002](0002-own-cqrs-handlers.md) | CQRS leve com handlers próprios (sem MediatR) | Aceito |
| [0003](0003-deterministic-seed-data.md) | Dados fictícios gerados de forma determinística | Aceito |
| [0004](0004-gemini-behind-llm-port.md) | Google Gemini atrás de um port próprio, com cliente HTTP e resiliência | Aceito |
| [0005](0005-review-classification.md) | Classificação de avaliações com saída estruturada, em lote e tolerante a falhas | Aceito |
| [0006](0006-rag-with-pgvector.md) | RAG com pgvector no mesmo PostgreSQL | Aceito |
| [0007](0007-agent-function-calling.md) | Agente com function calling, ferramentas tipadas e loop limitado | Aceito |
| [0008](0008-text-to-sql-defense-in-depth.md) | Texto para SQL com defesa em profundidade | Aceito |
| [0009](0009-chat-answer-cache.md) | Cache de respostas do agente no Redis, com invalidação por versão | Aceito |

## Modelo

```markdown
# NNNN. Título

- **Status:** Proposto | Aceito | Substituído por NNNN
- **Data:** AAAA-MM-DD

## Contexto
## Decisão
## Consequências
```
