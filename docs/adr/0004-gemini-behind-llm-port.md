# 0004. Google Gemini atrás de um port próprio, com cliente HTTP e resiliência

- **Status:** Aceito
- **Data:** 2026-11-11

## Contexto

O projeto precisa de um LLM com geração de texto, saída JSON estruturada, function calling e embeddings, sem custo. O plano gratuito do Google AI Studio (Gemini) atende os quatro requisitos, mas tem limites baixos de requisições por minuto e por dia (respostas `429`), e os modelos oferecidos mudam com frequência.

## Decisão

1. **Port próprio na `Application`.** `ILlmClient` tem um único método, `GenerateAsync(LlmRequest)`. O pedido descreve mensagens, ferramentas (JSON Schema) e, opcionalmente, um JSON Schema de resposta; a resposta traz texto, chamadas de ferramenta e uso de tokens. `IEmbeddingService` gera vetores distinguindo documento de consulta. Nenhum tipo do Gemini vaza para fora da `Infrastructure`.
2. **Cliente HTTP próprio em vez do SDK.** `GeminiClient` e `GeminiEmbeddingService` são `HttpClient` tipados sobre a REST API (`generateContent`, `batchEmbedContents`). Isso dá controle total do formato enviado, facilita testar com um `HttpMessageHandler` falso e reaproveita o pipeline de resiliência padrão do .NET.
3. **Esquema derivado do tipo.** `GenerateJsonAsync<T>` gera o JSON Schema a partir de `T` com o `JsonSchemaExporter` do .NET, então esquema e desserialização nunca divergem.
4. **Resiliência com `Microsoft.Extensions.Http.Resilience`** (Polly): retry exponencial com jitter para `429`/`5xx`, respeitando `Retry-After`; timeout por tentativa e total; circuit breaker. Os valores ficam em `GeminiOptions`.
5. **Tudo configurável:** `Gemini:GenerationModel`, `Gemini:EmbeddingModel`, `Gemini:EmbeddingDimensions` e a chave (`Gemini:ApiKey`, via user-secrets ou variável de ambiente).
6. **Health check "degradado"**: sem chave ou com o Gemini fora do ar, `/health` reporta `Degraded` (HTTP 200). As funções de IA falham com erro claro (`Llm.NotConfigured`, `Llm.RateLimited`...), mas a API continua servindo dados.

## Consequências

- **Trocar de provedor** (Groq, Ollama, OpenAI) exige apenas uma nova implementação de `ILlmClient`/`IEmbeddingService` e a troca do registro na injeção de dependência; casos de uso e domínio não mudam.
- **Detalhes do provedor que precisam voltar na conversa** (as *thought signatures* dos modelos Gemini 3 em chamadas de ferramenta) trafegam como um campo opaco (`LlmToolCall.ProviderState`), sem a aplicação conhecer seu significado.
- **Custo de manutenção:** mudanças na REST API do Gemini exigem ajustar os contratos em `GeminiContracts.cs`, que um SDK absorveria. Como usamos poucos campos, o risco é baixo.
