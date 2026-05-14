import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import flowbiteReact from "flowbite-react/plugin/vite";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), flowbiteReact()],
   server: {
    allowedHosts: ["sc-dev-ticketspro.ngrok.io"],
    proxy: {
      '/api': {
        target: 'http://localhost:5220', // Your .NET API URL
        changeOrigin: true,
        secure: false,
        // This removes '/api' from the front of the URL path
        rewrite: (path) => path.replace(/^\/api/, ''), 
      }
    }
  }
})