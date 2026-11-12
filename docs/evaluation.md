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

> A avaliação **nunca roda automaticamente**: o CI executa apenas os testes unitários e de integração, que usam dublês do LLM. Cada pergunta da avaliação gasta cota real do Gemini, então ela é sempre disparada à mão.

```bash
docker compose up -d --build     # API em http://localhost:8080, com GEMINI_API_KEY no .env
dotnet run --project backend/tools/RestaurantCopilot.Evaluation
```

Por padrão o runner roda a suíte **`smoke`**: 4 perguntas escolhidas para serem baratas (uma ferramenta cada, correção por número ou palavra-chave, sem juiz), cerca de 8 chamadas ao LLM. Como o plano gratuito do `gemini-3.5-flash` permite **20 requisições por dia**, sobra cota para usar o copiloto no mesmo dia.

| Opção | Efeito |
|---|---|
| `--suite smoke` (padrão) | as 4 perguntas da suíte rápida |
| `--suite full` | as 26 perguntas (~90 chamadas: com o plano gratuito, divida em vários dias com `--only`) |
| `--only sales-04,reviews-05` | só as perguntas indicadas |
| `--max-llm-calls 10` (padrão) | teto de chamadas ao LLM; ao atingir, as perguntas restantes são **puladas** (não contam como erro) |
| `--prepare` | classifica as avaliações pendentes e recria o índice semântico antes (só na primeira vez; gasta ~10 chamadas de geração) |
| `--delay 4` | segundos entre perguntas |

O relatório é gravado em [`evaluation/results/latest.md`](../evaluation/results/latest.md), com a taxa de acerto geral e por categoria, latência, tokens e cada resposta. O runner chama a API por HTTP, como um cliente real, com `Cache-Control: no-cache` para medir o agente e não o cache.

## Resultados

> ⏳ **Ainda não concluída.** Na primeira execução com `gemini-3.5-flash`, as 4 perguntas que receberam resposta (`sales-01`, `02`, `03` e `05`) acertaram; as demais esbarraram na cota gratuita do modelo (20 requisições por dia), já parcialmente consumida pelos testes manuais. A rodada completa precisa de ~90 chamadas (agente + juiz); com o plano gratuito, use a suíte `smoke` no dia a dia e divida a `full` em vários dias com `--only`. O resumo será registrado aqui e no README, e o relatório completo ficará em `evaluation/results/latest.md`.
