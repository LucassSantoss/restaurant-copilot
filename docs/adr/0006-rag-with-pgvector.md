# 0006. RAG com pgvector no mesmo PostgreSQL

- **Status:** Aceito
- **Data:** 2026-11-11

## Contexto

Perguntas como *"o que os clientes estão falando sobre a embalagem?"* não se resolvem com filtros exatos: é preciso encontrar avaliações **pelo significado** ("a tampa abriu", "molho vazou") e entregá-las ao LLM como contexto. Isso exige embeddings e busca por similaridade.

## Decisão

- **Banco vetorial:** extensão **pgvector** no mesmo PostgreSQL dos dados relacionais, em vez de um serviço dedicado (Pinecone, Qdrant...). A tabela `search_documents` guarda origem (`MenuItem`/`Review`), id de origem, texto e vetor `vector(768)`.
- **Índice HNSW com `vector_cosine_ops`:** busca aproximada de vizinhos em tempo sublinear; o operador do índice é o mesmo usado na consulta (distância de cosseno), senão o índice seria ignorado.
- **Documentos autocontidos:** cada item vira um texto em português com o que o LLM precisa para citá-lo (prato, nota, data local, dia da semana), porque o trecho é lido sem as linhas do banco ao redor.
- **Tipos de tarefa:** documentos são embutidos como `RETRIEVAL_DOCUMENT` e perguntas como `RETRIEVAL_QUERY`, o que melhora a recuperação no Gemini.
- **Dimensão 768:** o `gemini-embedding-001` permite vetores truncados (até 3072). 768 dimensões ocupam 4× menos espaço com perda pequena de qualidade. A dimensão está no schema; o reindex compara a configuração do modelo com a do banco **antes** de gastar cota.
- **Reindex completo e atômico:** `POST /api/ingestion/reindex` reconstrói o índice e o troca numa única transação; buscas nunca veem um índice pela metade. Com ~240 documentos, isso custa 3 chamadas em lote ao Gemini.

## Consequências

- Uma única infraestrutura para dados relacionais e vetoriais: backups, transações e joins juntos, e um container a menos.
- Avaliações novas só entram na busca semântica após o próximo reindex (os insights e o texto para SQL as veem na hora). Indexação incremental é um próximo passo simples (`upsert` por `source_id`), deixado de fora para economizar cota.
- Para milhões de vetores, um banco vetorial dedicado ou particionamento passaria a valer a pena.
