import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

const apiProxyTarget = process.env.VITE_API_PROXY_TARGET ?? 'http://127.0.0.1:8080';

/** node_modules only — never split lazy route modules to avoid chunk cycles. */
function manualChunks(id: string): string | undefined {
  if (!id.includes('node_modules')) {
    return undefined;
  }

  if (id.includes('node_modules/@microsoft/signalr')) {
    return 'vendor-signalr';
  }

  if (id.includes('node_modules/qrcode')) {
    return 'vendor-qrcode';
  }

  if (
    id.includes('node_modules/react/') ||
    id.includes('node_modules/react-dom/') ||
    id.includes('node_modules/react-router') ||
    id.includes('node_modules/scheduler/')
  ) {
    return 'vendor-react';
  }

  if (id.includes('node_modules/recharts') || id.includes('node_modules/chart.js')) {
    return 'vendor-charts';
  }

  return 'vendor';
}

export default defineConfig({
  plugins: [react()],
  build: {
    chunkSizeWarningLimit: 600,
    rollupOptions: {
      output: {
        manualChunks,
      },
    },
  },
  server: {
    host: '0.0.0.0',
    port: 5173,
    strictPort: false,
    proxy: {
      '/api': { target: apiProxyTarget, changeOrigin: true },
      '/hubs': { target: apiProxyTarget, changeOrigin: true, ws: true },
      '/uploads': { target: apiProxyTarget, changeOrigin: true },
    },
  },
  preview: {
    host: '0.0.0.0',
    port: 5173,
    strictPort: false,
    proxy: {
      '/api': { target: apiProxyTarget, changeOrigin: true },
      '/hubs': { target: apiProxyTarget, changeOrigin: true, ws: true },
      '/uploads': { target: apiProxyTarget, changeOrigin: true },
    },
  },
});
