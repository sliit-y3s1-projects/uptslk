import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import { resolve } from "node:path";

// Test configuration. It is separate from vite.config.ts so the Tailwind plugin and the dev-server settings are not
// loaded for tests.
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { "@": resolve(import.meta.dirname, "./src") },
  },
  test: {
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
    // Only TypeScript tests. The Node test-runner file src/features/fares/tests/contracts.test.mjs is not picked up.
    include: ["src/**/*.{test,spec}.{ts,tsx}"],
    css: false,
    // Rendering whole pages is slow on a busy machine, with coverage on, or in CI; the defaults (5 s test, 1 s wait) are too tight.
    testTimeout: 20000,
    hookTimeout: 20000,
    clearMocks: true,
    restoreMocks: true,
    // A fixed API address, so mock handlers do not depend on a developer's .env file.
    env: { VITE_API_BASE_URL: "http://api.test" },
    coverage: {
      provider: "v8",
      reporter: ["text-summary", "html", "lcov"],
      reportsDirectory: "./coverage",
      include: ["src/**/*.{ts,tsx}"],
      exclude: [
        "src/components/ui/**", // generated shadcn components
        "src/test/**",
        "src/mock/**",
        "src/types/**",
        "src/main.tsx",
        "src/**/*.{test,spec}.{ts,tsx}",
      ],
    },
  },
});
