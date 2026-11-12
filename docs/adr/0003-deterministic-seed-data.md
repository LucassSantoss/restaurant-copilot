# 0003. Dados fictícios gerados de forma determinística

- **Status:** Aceito
- **Data:** 2026-11-11

## Contexto

O projeto precisa de dados realistas (cardápio, pedidos, avaliações) para a demonstração, para os testes de integração e, principalmente, para a avaliação do agente, que compara as respostas com valores esperados. Dados reais de empresas não podem ser usados.

## Decisão

- Um gerador em [`seed/generate.cs`](../../seed/generate.cs) (um *file-based app* do .NET 10: `dotnet run seed/generate.cs`) produz os arquivos JSON em `seed/`. Ele usa uma semente fixa, então rodar de novo gera exatamente os mesmos dados.
- Os JSON gerados são **versionados**. A API, os testes e a avaliação leem os mesmos arquivos.
- O período é fixo (03/08/2026 a 08/11/2026, 14 semanas completas), com datas absolutas no fuso `America/Sao_Paulo`.
- O gerador planta padrões de negócio de propósito, para que o agente tenha algo real a descobrir: terça-feira é o dia mais fraco; entregas de sexta e sábado à noite recebem reclamações de atraso; em outubro a lasanha tem um problema de embalagem (molho vazando) que gera uma onda de avaliações negativas; o risoto de camarão recebe reclamações de preço; a última semana tem mais pedidos que a anterior.
- O `DatabaseSeeder` cria cada registro pelas factories do domínio, então os dados do seed obedecem às mesmas invariantes dos dados criados pela API. O seed só roda em banco vazio.

## Consequências

- **Positivas:** respostas esperadas da avaliação continuam válidas; testes de integração fazem asserções sobre números conhecidos; nenhum dado real envolvido.
- **Negativas:** com datas absolutas, perguntas relativas ("na última semana") dependem da data atual. O agente recebe a data de hoje no contexto, e a avaliação usa períodos explícitos.
