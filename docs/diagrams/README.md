# Diagramas

Os diagramas de arquitetura serão escritos em [Mermaid](https://mermaid.js.org/), para que sejam versionados como texto e renderizados direto no GitHub.

Diagramas planejados:

- Visão geral dos componentes (API, PostgreSQL + pgvector, Redis, Gemini)
- Camadas da Clean Architecture e direção das dependências
- Fluxo do agente em `POST /api/chat` (cache → orquestrador → ferramentas → resposta com fontes)
- Pipeline de ingestão do RAG (cardápio e avaliações → embeddings → pgvector)
