import vue from "@vitejs/plugin-vue";
import { defineConfig, loadEnv } from "vite";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");

  return {
    plugins: [vue()],

    server: {
      host: "0.0.0.0",

      watch: {
        usePolling: true,
      },

      proxy: {
        "/api": {
          target: env.VITE_API_PROXY_TARGET ?? "http://localhost:8000",

          changeOrigin: true,
        },
      },
    },
  };
});
