# 0005. Classificação de avaliações com saída estruturada, em lote e tolerante a falhas

- **Status:** Aceito
- **Data:** 2026-11-11

## Contexto

Cada avaliação precisa de um **sentimento** (positivo, neutro, negativo) e dos **temas** que menciona (atraso, sabor, embalagem, preço, atendimento). Um LLM classifica texto livre em português bem melhor que regras por palavra-chave, mas traz três riscos: respostas fora do formato, limites do plano gratuito e texto do cliente tentando dar instruções ao modelo (*prompt injection*).

## Decisão

- **Saída estruturada:** o classificador pede JSON com o esquema gerado a partir de um record C# (`GenerateJsonAsync<T>`, ver [ADR 0004](0004-gemini-behind-llm-port.md)), e o Gemini é obrigado a respeitá-lo. Sentimentos e temas são enums, então valores inventados falham na desserialização em vez de entrar no banco.
- **Vários temas por avaliação:** "atrasou e veio vazando" é atraso e embalagem. Os temas ficam numa coluna `text[]`, fácil de filtrar em SQL (`'Packaging' = ANY(themes)`).
- **Em lote:** `POST /api/ingestion/classify-reviews` envia até 25 avaliações por chamada, identificadas por índices curtos (0, 1, 2...) em vez de GUIDs, o que economiza tokens e evita IDs "inventados". As 215 avaliações do seed são classificadas em 9 chamadas.
- **Tolerante a falhas:** `POST /api/reviews` salva a avaliação mesmo se o LLM falhar; ela fica pendente (`classification: null`) e entra na próxima execução do lote. Perder o feedback de um cliente por causa de um `429` seria pior que atrasar um insight. O lote para e informa o motivo quando o LLM falha, ou quando o modelo ignora um lote inteiro (evita loop infinito).
- **Defesa contra prompt injection:** as avaliações vão como dados JSON, e o prompt de sistema manda tratá-las estritamente como dados. O pior caso de uma injeção bem-sucedida é uma classificação errada, porque o classificador não tem ferramentas nem acesso a dados.
- **Insights agregados em C#:** `GET /api/reviews/insights` carrega uma projeção leve das avaliações do período e agrega em memória. Os limites dos dias seguem o fuso do restaurante.

## Consequências

- As regras de agregação ficam em C# simples, cobertas por testes unitários. Para milhões de avaliações, o próximo passo seria agregar em SQL.
- A qualidade da classificação depende do modelo configurado. A avaliação do agente (Fase 9) inclui perguntas que dependem dela.
