import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Proxy API calls to the backend service and strip 'api' prefix
      '/api': {
        target: process.env.services__apiservice__https__0 || process.env.services__apiservice__http__0,
        changeOrigin: true
      }
    }
  }
})
