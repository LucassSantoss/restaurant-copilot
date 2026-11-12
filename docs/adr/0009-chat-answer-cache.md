# 0009. Cache de respostas do agente no Redis, com invalidação por versão

- **Status:** Aceito
- **Data:** 2026-11-11

## Contexto

Cada pergunta ao agente custa de 2 a ~7 chamadas ao Gemini e alguns segundos. O plano gratuito tem limites diários baixos, e donos de restaurante tendem a repetir as mesmas perguntas ("quanto vendi esta semana?"). Por outro lado, uma resposta em cache não pode ficar errada quando os dados mudam ou quando o dia vira.

## Decisão

- **`ICacheService` na `Application`**, implementado por `RedisCacheService` (`IDistributedCache` + Redis, valores em JSON).
- **Chave** = `chat:v1:{versão dos dados}:{data de hoje no fuso do restaurante}:{SHA-256 da pergunta normalizada}`:
  - **pergunta normalizada** (minúsculas, espaços colapsados, sem pontuação final): "Quanto vendi?" e "  quanto VENDI " compartilham a resposta;
  - **data de hoje:** "esta semana" significa outra coisa amanhã;
  - **versão dos dados:** um valor em `chat:data-version`, substituído a cada escrita que muda o que o agente lê (nova avaliação, classificação, reindex). Isso invalida **todas** as respostas de uma vez, sem varrer nem apagar chaves; as antigas expiram pelo TTL.
- **TTL** configurável (`ChatCache:TimeToLive`, 1 h por padrão). Erros do agente nunca são cacheados.
- **Falha do Redis não é falha do usuário:** leitura com erro vira *miss* e escrita com erro é ignorada, com log de aviso.
- A resposta indica `"cached": true` quando veio do cache.

## Consequências

- Perguntas repetidas respondem em milissegundos e sem consumir cota.
- Pedidos novos chegando por outro sistema não passariam pela API e não invalidariam o cache; nesse cenário, o TTL limita a defasagem. Uma integração real publicaria um evento que também troca a versão.
- Perguntas equivalentes com palavras diferentes ("faturamento" e "vendas") não compartilham cache. Um cache semântico (por similaridade de embedding) é um possível próximo passo, com o risco de servir a resposta de uma pergunta parecida, mas diferente.
