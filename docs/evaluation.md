# Avaliação do agente

Como medimos se o RestaurantCopilot responde **certo**, não só de forma convincente.

## Metodologia

O conjunto de avaliação ([`evaluation/questions.json`](../evaluation/questions.json)) tem **26 perguntas** sobre os dados fictícios do seed, em cinco categorias:

| Categoria | Perguntas | O que testa |
|---|---|---|
| `sales` | 12 | Faturamento, pedidos, ticket médio, comparação de semanas, dias da semana, pratos e canais (`search_sales`, `generate_report`) |
| `text-to-sql` | 4 | Perguntas que as ferramentas especializadas não cobrem (hora do dia, maior pedido, pedidos com um prato) e exigem `query_database` |
| `reviews` | 7 | Contagens, notas médias, pratos com mais reclamações e perguntas abertas sobre o que os clientes dizem (RAG) |
| `advice` | 2 | Sugestão de promoção e resumo do mês, que precisam se apoiar nos dados |
| `safety` | 1 | Pedido para apagar dados: o agente não pode alegar que alterou nada |

Cada pergunta é corrigida de um destes jeitos:

- **`numeric` (match exato de números):** todos os valores esperados precisam aparecer na resposta, com tolerância de 0,5% (ou a definida na pergunta). O extrator entende `R$ 55.284,70`, `55,284.70` e `55284.7`.
- **`keywords`:** termos que precisam aparecer (ignorando maiúsculas e acentos); `delivery|entrega` aceita qualquer um.
- **`judge` (LLM como juiz):** para respostas abertas, um LLM verifica critérios explícitos (por exemplo, "deve apontar a Lasanha à Bolonhesa como o prato mais afetado"). O juiz é rigoroso: respostas genéricas reprovam.

Princípios do conjunto:

- **Períodos explícitos** ("outubro de 2026"), para que o resultado não dependa do dia em que a avaliação roda.
- **Valores esperados reproduzíveis:** cada pergunta numérica traz o `referenceSql` que calcula o valor no banco. Todos foram conferidos contra o PostgreSQL com o seed carregado.
- **Padrões plantados no seed** ([ADR 0003](adr/0003-deterministic-seed-data.md)) dão respostas inequívocas: terça é o dia mais fraco, sábado o mais forte, a lasanha concentra as reclamações de outubro.
- **Ferramentas esperadas** são registradas e reportadas (o agente usou uma ferramenta adequada?), mas não entram na nota: o que vale é a resposta.

## Como rodar

```bash
docker compose up -d --build            # API em http://localhost:8080, com GEMINI_API_KEY no .env
dotnet user-secrets set "Gemini:ApiKey" "<sua-chave>" --project backend/src/RestaurantCopilot.Api   # usada pelo juiz
dotnet run --project backend/tools/RestaurantCopilot.Evaluation -- --prepare
```

- `--prepare` classifica as avaliações pendentes e recria o índice semântico antes de começar (necessário na primeira vez).
- `--delay 4` espaça as perguntas (em segundos) para respeitar o limite por minuto do plano gratuito.
- `--only sales-01,reviews-05` roda só algumas perguntas.
- O relatório é gravado em [`evaluation/results/latest.md`](../evaluation/results/latest.md), com a taxa de acerto geral e por categoria, latência, tokens e cada resposta.

O runner chama a API por HTTP, como um cliente real, com `Cache-Control: no-cache` para medir o agente e não o cache.

## Resultados

> ⏳ **Ainda não executada.** A execução exige uma chave do Gemini. Ao rodar, o resumo será registrado aqui e no README, e o relatório completo ficará em `evaluation/results/latest.md`.
