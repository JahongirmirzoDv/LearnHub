// @ts-check
const { test, expect } = require("@playwright/test");
const { watchPage, expectHealthyPage, screenshot, login } = require("./helpers");

const email = process.env.E2E_ADMIN_EMAIL;
const password = process.env.E2E_ADMIN_PASSWORD;

test.describe("Administrator experience", () => {
  test.skip(!email || !password, "E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD are not set.");

  test("dashboard and management pages", async ({ page }, testInfo) => {
    const problems = watchPage(page);

    await login(page, email, password);
    await expect(page).toHaveURL(/\/Admin$/);
    await expect(page.getByRole("heading", { level: 1, name: "Dashboard" })).toBeVisible();
    await expectHealthyPage(page, problems);
    await screenshot(page, testInfo, "20-admin-dashboard");

    for (const [url, heading, name] of [
      ["/Admin/Courses", "Courses", "21-admin-courses"],
      ["/Admin/Quizzes", "Quizzes", "23-admin-quizzes"],
      ["/Admin/Users", "Users", "24-admin-users"],
      ["/Admin/Enrollments", "Enrolments", "25-admin-enrolments"]
    ]) {
      await page.goto(url);
      await expect(page.getByRole("heading", { level: 1, name: heading })).toBeVisible();
      await expectHealthyPage(page, problems);
      await screenshot(page, testInfo, name);
    }

    await page.goto("/Admin/Courses");
    await page.getByRole("link", { name: /^Edit/ }).first().click();
    await expect(page.getByRole("heading", { level: 1, name: "Edit course" })).toBeVisible();
    await expectHealthyPage(page, problems);
    await screenshot(page, testInfo, "22-admin-course-form");
  });

  test("category create and delete through the forms", async ({ page }, testInfo) => {
    const problems = watchPage(page);
    const name = `E2E ${testInfo.project.name} ${Date.now()}`.slice(0, 40);

    await login(page, email, password);
    await page.goto("/Admin/Categories/Create");

    await page.getByRole("button", { name: "Create category" }).click();
    await expect(page.getByText("Please enter a category name.")).toBeVisible();

    await page.getByLabel("Name").fill(name);
    await page.getByLabel("Description").fill("Created by the automated browser test.");
    await page.locator("label[for='icon-robot']").click();
    await page.getByRole("button", { name: "Create category" }).click();

    await expect(page).toHaveURL(/\/Admin\/Categories$/);
    await expect(page.getByText(`Category "${name}" created.`)).toBeVisible();
    await screenshot(page, testInfo, "26-admin-category-created");

    // Table rows are restyled as cards on phones, so locate the row by its text instead of its ARIA role.
    const row = page.locator("tr", { hasText: name });
    await row.getByRole("link", { name: /^Delete/ }).click();
    await page.getByRole("button", { name: "Delete category" }).click();
    await expect(page.getByText("Category deleted.")).toBeVisible();
    await expectHealthyPage(page, problems);
  });
});
