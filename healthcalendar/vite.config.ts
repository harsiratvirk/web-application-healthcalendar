import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ command, mode}) => {
  const env = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [react()],
    server: {
      // Uses the VITE_PORT from .env.development as port
      // Uses 6000 as port if VITE_PORT is not set 
      port: Number(env.VITE_PORT) || 6000
    }
  }
})

