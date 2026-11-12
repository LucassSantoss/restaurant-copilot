import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// In development the API runs separately (docker compose, port 8080); proxying keeps the
// browser on a single origin, so the API needs no CORS configuration.
const apiUrl = process.env.API_URL ?? 'http://localhost:8080'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': apiUrl,
      '/health': apiUrl,
    },
  },
})
