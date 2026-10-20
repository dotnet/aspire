import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Aspire injects the address of the business API.
      '/api': {
        target: process.env.API_HTTPS || process.env.API_HTTP,
        changeOrigin: true,
        secure: false
      }
    }
  }
});
