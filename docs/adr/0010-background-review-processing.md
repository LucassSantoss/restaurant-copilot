# 0010. Processamento de avaliações em segundo plano (classificação e indexação incremental)

- **Status:** Aceito (complementa o [ADR 0005](0005-review-classification.md) e o [ADR 0006](0006-rag-with-pgvector.md))
- **Data:** 2026-11-12

## Contexto

Até aqui, uma avaliação nova era classificada na hora (se o LLM respondesse), mas só entrava na busca semântica depois de alguém chamar `POST /api/ingestion/reindex`. O reindex completo refaz ~270 documentos e leva ~2 minutos no plano gratuito: não serve para ser disparado a cada avaliação. E uma avaliação que ficou pendente por falha do LLM dependia de alguém chamar `POST /api/ingestion/classify-reviews`.

## Decisão

- **Indexação incremental:** o caso de uso `IndexPendingDocuments` embute só os pratos e avaliações que **ainda não têm documento** em `search_documents` e grava com *upsert* (troca o documento do mesmo item). O que está pendente é deduzido do próprio índice (consulta "sem documento correspondente"), então não houve migration nem campo de controle a manter em sincronia.
- **Worker em segundo plano** (`ReviewProcessingWorker`, um `BackgroundService`): a cada execução, classifica as avaliações pendentes (opcional) e indexa os documentos pendentes. Ele roda:
  - **na hora**, quando uma avaliação é cadastrada (o caso de uso chama `IReviewProcessingTrigger.Notify()`; várias notificações seguidas viram uma única execução);
  - **a cada 30 s** (`ReviewProcessing:PollInterval`), para pegar o que tiver sobrado.
- **Pendências vivem no banco:** se a API reiniciar ou o Gemini cair, nada se perde; a próxima execução continua de onde parou.
- **Backoff exponencial** em falhas (30 s, 1 min, 2 min... até 15 min, `ReviewProcessing:MaxBackoff`), para não insistir num provedor no limite de cota.
- **Sem gasto à toa:** sem pendências, nenhuma chamada ao Gemini é feita. Um lote que continua pendente depois de indexado interrompe a execução, em vez de gastar cota em loop.
- **Custo controlado por configuração:** cada avaliação nova custa 1 embedding (cota própria, 100/min) e, se `ReviewProcessing:ClassifyReviews` estiver ligado, 1 requisição de geração (cota de 20/dia). O worker inteiro pode ser desligado com `ReviewProcessing:Enabled=false`.
- O **reindex completo continua existindo** para reconstruir tudo (por exemplo, ao trocar o modelo de embeddings). Num banco novo, o worker também monta o índice sozinho na primeira subida.

## Consequências

- Avaliações novas ficam classificadas e pesquisáveis segundos depois de cadastradas, sem intervenção manual.
- Em várias instâncias da API, cada uma teria seu worker; o upsert torna isso seguro, mas desperdiçaria chamadas. Com mais de uma instância, o próximo passo seria uma fila (ou um lock no banco) para eleger um único processador.
- Num banco novo e com a chave configurada, a primeira subida classifica as 242 avaliações do seed (~10 requisições de geração) e indexa ~270 documentos (~3 min). Para evitar isso, ligue o worker só depois (`ReviewProcessing:Enabled=false`).
