import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
    plugins: [react()],
    server: {
        proxy: {
            '/api': {
                target: process.env.services__gamemaster__http__0 || 'http://localhost:5100',
                changeOrigin: true,
            },
        },
    },
});
