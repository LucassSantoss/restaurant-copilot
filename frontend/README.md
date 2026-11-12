# RestaurantCopilot — Frontend

Interface de chat do RestaurantCopilot em React 19 + TypeScript + Vite, com Tailwind CSS 4 e animações com Motion.

```bash
npm install
npm run dev      # http://localhost:5173, com proxy de /api e /health para http://localhost:8080
npm run build    # checagem de tipos + build de produção
npm run lint     # oxlint
```

Para apontar para outra API: `API_URL=http://host:porta npm run dev`. No Docker Compose, o serviço `frontend` (nginx) serve o build em http://localhost:3000 e encaminha `/api` para a API.

## Estrutura

- `src/api`: cliente HTTP e contratos da API.
- `src/hooks`: estado da conversa (persistido no `localStorage`) e carregamento de dados.
- `src/components`: barra lateral (insights de avaliações e status), chat, indicador de "pensando", detalhes de cada resposta (ferramentas, SQL, fontes e tokens).
