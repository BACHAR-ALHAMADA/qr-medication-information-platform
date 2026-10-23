import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const target = new URL(env.API_PROXY_TARGET || 'http://127.0.0.1:5080')
  if (!['http:', 'https:'].includes(target.protocol) || !['127.0.0.1', 'localhost'].includes(target.hostname)
    || target.username || target.password || target.pathname !== '/' || target.search || target.hash) {
    throw new Error('API_PROXY_TARGET must be a loopback HTTP(S) origin, for example http://127.0.0.1:5080.')
  }
  return {
    plugins: [react()],
    server: {
      host: '127.0.0.1', port: 5173, strictPort: true,
      proxy: { '/api': { target: target.origin, changeOrigin: true } },
    },
  }
})
