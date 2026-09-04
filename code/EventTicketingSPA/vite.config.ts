import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import fs from 'fs'
import path from 'path'

// Try to load certs from .cert/localhost.pem and .cert/localhost-key.pem
const certDir = path.resolve(__dirname, '.cert')
const certFile = path.join(certDir, 'localhost.pem')
const keyFile = path.join(certDir, 'localhost-key.pem')

let httpsOption: any = true
if (fs.existsSync(certFile) && fs.existsSync(keyFile)) {
  httpsOption = {
    cert: fs.readFileSync(certFile, 'utf8'),
    key: fs.readFileSync(keyFile, 'utf8'),
  }
}

// Default API https target — ensure this matches your dotnet launchSettings HTTPS URL
const API_TARGET = process.env.VITE_API_BASE_URL || 'http://localhost:5220'

export default defineConfig({
  plugins: [react()],
  server: {
    
    //Use to run this ass https server
    //https: httpsOption,
    host: true, // Same as running --host
    allowedHosts: ['.trycloudflare.com'],  // Allows the Cloudflare URL to connect

    
    port: 5173,
    // hmr: {
    //   clientPort: 443, 
    // },
    proxy: {
      // Proxy API calls to the backend so cookies are first-party in dev
      '/api': {
        target: API_TARGET,
        changeOrigin: true,
        secure: false,
        rewrite: (p) => p.replace(/^\/api/, '/api')
      }
    }
  }
})
