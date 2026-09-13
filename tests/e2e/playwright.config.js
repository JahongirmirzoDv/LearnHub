// @ts-check
const { defineConfig, devices } = require("@playwright/test");

/**
 * Runs against an already started LearnHub instance (see the e2e job in .github/workflows/ci.yml).
 * Each spec runs in three viewports so layout problems on phones and tablets are caught.
 */
module.exports = defineConfig({
  testDir: "./specs",
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  timeout: 60_000,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: process.env.E2E_BASE_URL ?? "http://127.0.0.1:5080",
    // CI serves HTTPS with the untrusted ASP.NET Core development certificate.
    ignoreHTTPSErrors: true,
    trace: "retain-on-failure",
    screenshot: "only-on-failure"
  },
  projects: [
    { name: "desktop", use: { ...devices["Desktop Chrome"], viewport: { width: 1440, height: 900 } } },
    { name: "tablet", use: { ...devices["Desktop Chrome"], viewport: { width: 768, height: 1024 }, hasTouch: true } },
    { name: "mobile", use: { ...devices["Pixel 7"] } }
  ]
});
