/// <reference types="vitest/config" />
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue(), tailwindcss()],
  server: {
    // Forwards /api to the backend, so the browser never makes a cross-origin call in development.
    proxy: {
      '/api': 'http://localhost:5080',
    },
  },
  test: {
    // Vitest reuses this config (decision #14). jsdom gives the tests a DOM for Vue components.
    environment: 'jsdom',
  },
})
