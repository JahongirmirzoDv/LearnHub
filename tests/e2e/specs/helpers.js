// @ts-check
const { expect } = require("@playwright/test");
const path = require("node:path");

const baseUrl = process.env.E2E_BASE_URL ?? "http://127.0.0.1:5080";

/**
 * Collects console errors, failed requests and broken images while a test runs,
 * and checks that pages never scroll sideways (a common mobile layout bug).
 */
function watchPage(page) {
  const problems = [];
  page.on("console", (message) => {
    if (message.type() === "error") {
      problems.push(`console: ${message.text()}`);
    }
  });
  page.on("pageerror", (error) => problems.push(`script error: ${error.message}`));
  page.on("response", (response) => {
    const url = response.url();
    // Only LearnHub's own requests; embedded third-party players are outside our control.
    if (response.status() >= 400 && url.startsWith(baseUrl)) {
      problems.push(`HTTP ${response.status()}: ${url}`);
    }
  });
  return problems;
}

async function expectHealthyPage(page, problems) {
  // $$eval and evaluate are Playwright APIs that run the callback inside the test browser (not JavaScript eval).
  const brokenImages = await page.$$eval("img", (images) =>
    images.filter((img) => img.complete && img.naturalWidth === 0 && img.getAttribute("src") !== "data:,").map((img) => img.src)
  );
  expect(brokenImages, "broken images").toEqual([]);

  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  expect(overflow, "page must not scroll horizontally").toBeLessThanOrEqual(1);

  expect(problems, "console errors or failed requests").toEqual([]);
}

async function screenshot(page, testInfo, name) {
  const file = path.join(__dirname, "..", "screenshots", testInfo.project.name, `${name}.png`);
  await page.screenshot({ path: file, fullPage: true });
}

async function login(page, email, password) {
  await page.goto("/Account/Login");
  await page.getByLabel("Email address").fill(email);
  await page.getByLabel("Password", { exact: true }).fill(password);
  await page.getByRole("button", { name: "Log in" }).click();
}

async function openMenuIfCollapsed(page) {
  const toggler = page.getByRole("button", { name: "Toggle navigation" });
  if (await toggler.isVisible()) {
    await toggler.click();
  }
}

module.exports = { watchPage, expectHealthyPage, screenshot, login, openMenuIfCollapsed };
