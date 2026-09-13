// @ts-check
const { test, expect } = require("@playwright/test");
const { watchPage, expectHealthyPage, screenshot, openMenuIfCollapsed } = require("./helpers");

test.describe("Guest experience", () => {
  test("home page presents the catalogue", async ({ page }, testInfo) => {
    const problems = watchPage(page);
    await page.goto("/");

    await expect(page.getByRole("heading", { level: 1 })).toContainText("Build real computing skills");
    await expect(page.getByRole("heading", { name: "Popular courses" })).toBeVisible();
    await expectHealthyPage(page, problems);
    await screenshot(page, testInfo, "01-home");
  });

  test("navigation menu works on every screen size", async ({ page }) => {
    await page.goto("/");
    await openMenuIfCollapsed(page);

    const nav = page.getByRole("navigation", { name: "Main navigation" });
    await nav.getByRole("link", { name: "Courses" }).click();
    await expect(page).toHaveURL(/\/Courses$/);
    await expect(page.getByRole("heading", { level: 1, name: "Courses" })).toBeVisible();
  });

  test("server-side search and category filter", async ({ page }, testInfo) => {
    const problems = watchPage(page);
    await page.goto("/Courses");

    await page.getByRole("searchbox", { name: "Search courses" }).fill("security");
    await page.getByRole("button", { name: "Search", exact: true }).click();

    await expect(page).toHaveURL(/q=security/);
    await expect(page.getByText("1 course found")).toBeVisible();
    await expect(page.getByRole("link", { name: "Web Application Security Basics" })).toBeVisible();
    await expectHealthyPage(page, problems);
    await screenshot(page, testInfo, "02-courses-search");

    await page.goto("/Courses");
    await page.getByRole("link", { name: /^Databases/ }).first().click();
    await expect(page.getByRole("link", { name: "Relational Database Design with SQL" })).toBeVisible();
  });

  test("course details and a free preview lesson", async ({ page }, testInfo) => {
    const problems = watchPage(page);
    await page.goto("/Courses");
    await page.getByRole("link", { name: "Networking Fundamentals" }).first().click();

    await expect(page.getByRole("heading", { level: 1, name: "Networking Fundamentals" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Course route" })).toBeVisible();
    await expectHealthyPage(page, problems);
    await screenshot(page, testInfo, "03-course-details");

    await page.getByRole("link", { name: "How data travels across a network" }).click();
    await expect(page.getByRole("heading", { level: 1, name: "How data travels across a network" })).toBeVisible();
    await expect(page.getByText("You are viewing a free preview.")).toBeVisible();
    await expectHealthyPage(page, problems);
    await screenshot(page, testInfo, "04-preview-lesson");
  });

  test("contact form validates in the browser before submitting", async ({ page }, testInfo) => {
    const problems = watchPage(page);
    await page.goto("/Contact");

    await page.getByRole("button", { name: "Send message" }).click();

    await expect(page).toHaveURL(/\/Contact$/);
    await expect(page.getByText("Please enter your name.")).toBeVisible();
    await expect(page.getByText("Please write a message.")).toBeVisible();
    await expectHealthyPage(page, problems);
    await screenshot(page, testInfo, "05-contact-validation");
  });

  test("registration form gives live password feedback", async ({ page }, testInfo) => {
    const problems = watchPage(page);
    await page.goto("/Account/Register");

    const password = page.getByLabel("Password", { exact: true });
    await password.fill("short");
    await password.blur();
    await expect(page.getByText("Password must be between 8 and 100 characters.")).toBeVisible();

    await password.fill("Learning2026Secure");
    await expect(page.getByText("Password strength: Strong")).toBeVisible();
    await expectHealthyPage(page, problems);
    await screenshot(page, testInfo, "06-register");
  });

  test("unknown pages show the friendly 404 page", async ({ page }, testInfo) => {
    const response = await page.goto("/no-such-page");

    expect(response?.status()).toBe(404);
    await expect(page.getByRole("heading", { level: 1 })).toContainText("on the map");
    await screenshot(page, testInfo, "07-not-found");
  });

  test("about page", async ({ page }, testInfo) => {
    const problems = watchPage(page);
    await page.goto("/About");

    await expect(page.getByRole("heading", { level: 1, name: "About LearnHub" })).toBeVisible();
    await expectHealthyPage(page, problems);
    await screenshot(page, testInfo, "08-about");
  });
});
