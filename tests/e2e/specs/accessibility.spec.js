// @ts-check
const { test, expect } = require("@playwright/test");
const AxeBuilder = require("@axe-core/playwright").default;
const { login } = require("./helpers");

// Automated WCAG 2.2 level A and AA rules. Automated checks find a large share of issues (contrast, names, labels,
// landmarks, ARIA misuse) but not all of them, so the manual checklist in Documentation/Testing.md still applies.
const WCAG_TAGS = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"];

/** Scans each page and returns one readable line per violation, so a failure lists every problem at once. */
async function scanPages(page, urls) {
  const problems = [];
  for (const url of urls) {
    await page.goto(url);
    // Embedded YouTube and Vimeo players are third-party documents outside our control.
    const results = await new AxeBuilder({ page }).withTags(WCAG_TAGS).exclude("iframe").analyze();
    for (const violation of results.violations) {
      const examples = violation.nodes.slice(0, 3).map((node) => node.target.join(" ")).join(" | ");
      problems.push(`${url} – ${violation.id} (${violation.impact}): ${violation.help} – ${examples}`);
    }
  }
  return problems;
}

/** Follows the first link matching the selector and returns its path, so tests do not depend on database ids. */
async function firstLinkPath(page, url, selector) {
  await page.goto(url);
  const href = await page.locator(selector).first().getAttribute("href");
  expect(href, `a link matching ${selector} on ${url}`).toBeTruthy();
  return /** @type {string} */ (href);
}

test.describe("Accessibility (axe-core, WCAG 2.2 A and AA)", () => {
  test("public pages", async ({ page }) => {
    const course = await firstLinkPath(page, "/Courses", "a[href^='/Courses/Details/']");
    const urls = ["/", "/Courses", course, "/About", "/Contact", "/Privacy", "/Account/Login", "/Account/Register", "/no-such-page"];

    // Guests see links only for free preview lessons.
    const previewLink = page.locator("a.route-title[href^='/Resources/Details/']");
    await page.goto(course);
    if ((await previewLink.count()) > 0) {
      urls.push(/** @type {string} */ (await previewLink.first().getAttribute("href")));
    }

    expect(await scanPages(page, urls)).toEqual([]);
  });

  test("student pages", async ({ page }) => {
    const email = process.env.E2E_STUDENT_EMAIL;
    const password = process.env.E2E_STUDENT_PASSWORD;
    test.skip(!email || !password, "E2E_STUDENT_EMAIL and E2E_STUDENT_PASSWORD are not set.");

    await login(page, /** @type {string} */ (email), /** @type {string} */ (password));
    await expect(page).toHaveURL(/\/Student\/Dashboard$/);

    const lesson = await firstLinkPath(page, "/Student/MyCourses", "a[href^='/Courses/Details/']")
      .then((details) => firstLinkPath(page, details, "a.route-title[href^='/Resources/Details/']"));

    const urls = ["/Student/Dashboard", "/Student/MyCourses", lesson, "/Quizzes/History", "/Profile", "/Profile/ChangePassword"];
    expect(await scanPages(page, urls)).toEqual([]);
  });

  test("administration pages", async ({ page }) => {
    const email = process.env.E2E_ADMIN_EMAIL;
    const password = process.env.E2E_ADMIN_PASSWORD;
    test.skip(!email || !password, "E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD are not set.");

    await login(page, /** @type {string} */ (email), /** @type {string} */ (password));
    await expect(page).toHaveURL(/\/Admin$/);

    const urls = [
      "/Admin",
      "/Admin/Courses",
      "/Admin/Courses/Create",
      "/Admin/Categories",
      "/Admin/Resources",
      "/Admin/Quizzes",
      "/Admin/QuizAttempts",
      "/Admin/Enrollments",
      "/Admin/Users",
      "/Admin/Messages"
    ];
    expect(await scanPages(page, urls)).toEqual([]);
  });
});
