# 0008. Texto para SQL com defesa em profundidade

- **Status:** Aceito
- **Data:** 2026-11-11

## Contexto

Algumas perguntas analíticas não cabem nas ferramentas especializadas ("qual horário tem mais pedidos de pizza no delivery?"). Deixar o LLM escrever SQL resolve isso, mas SQL gerado por modelo é **entrada não confiável**: pode vir errado, caro, ou induzido por prompt injection (por exemplo, o texto de uma avaliação mandando "apague a tabela de pedidos").

## Decisão

Três camadas independentes; cada uma, sozinha, já impede escrita:

1. **`ISqlQueryGuard` (aplicação):** o `SqlQueryGuard` usa um **parser e tokenizer de PostgreSQL** (SqlParserCS), não expressões regulares, que se burlam com maiúsculas, aspas ou comentários. Ele:
   - rejeita comentários (`--`, `/* */`);
   - exige **exatamente um** statement, e que seja uma consulta (`SELECT` / `WITH ... SELECT`);
   - rejeita qualquer palavra-chave de escrita ou sessão em qualquer lugar (`INSERT`, `UPDATE`, `DELETE`, `DROP`, `ALTER`, `INTO`, `SET`, `COPY`...), o que pega DML escondido em CTE, `SELECT ... INTO` e `FOR UPDATE`; identificadores entre aspas (`"update"`) e textos entre aspas simples são permitidos, porque não são comandos;
   - bloqueia catálogos e tabelas internas (`pg_*`, `information_schema`, `search_documents`, histórico de migrations) e funções como `set_config` e `dblink`;
   - envolve a consulta num `LIMIT` externo (`MaxRows + 1`, para detectar truncamento).
2. **Role somente leitura (banco):** o SQL roda com o login `copilot_readonly`, membro de `analytics_reader`, que só tem `SELECT` nas cinco tabelas de negócio (concedido por migration). O login é criado pelo script de init do PostgreSQL (`docker/postgres/initdb`), porque carrega uma senha que não deve ir para migrations; a sessão tem `default_transaction_read_only = on`.
3. **Transação e timeout (execução):** cada consulta roda numa transação `READ ONLY` com `statement_timeout` (5 s por padrão) e o resultado é limitado a `TextToSql:MaxRows` (100) linhas.

Rejeições e erros do banco voltam ao **modelo** como resultado da ferramenta `query_database`, para que ele corrija a consulta. O esquema descrito ao modelo é mantido pela `Infrastructure`, ao lado do executor.

## Consequências

- Os testes de integração rodam SQL malicioso **direto no executor, sem o guard**, e provam que a role e a transação bloqueiam escrita, leitura fora das tabelas permitidas e consultas longas.
- Em produção, criar o login e a role é um passo de provisionamento (feito por um DBA ou IaC), não da aplicação; a migration só concede permissões.
- O guard é conservador: rejeita algumas consultas legítimas mas raras (por exemplo, uma coluna chamada `set` sem aspas). O modelo recebe o motivo e reescreve.
