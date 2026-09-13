// @ts-check
const { test, expect } = require("@playwright/test");
const { watchPage, expectHealthyPage, screenshot, login } = require("./helpers");

const email = process.env.E2E_STUDENT_EMAIL;
const password = process.env.E2E_STUDENT_PASSWORD;

test.describe("Student experience", () => {
  test.skip(!email || !password, "E2E_STUDENT_EMAIL and E2E_STUDENT_PASSWORD are not set.");

  test("dashboard, course progress, lesson and quiz", async ({ page }, testInfo) => {
    const problems = watchPage(page);

    await login(page, email, password);
    await expect(page).toHaveURL(/\/Student\/Dashboard$/);
    await expect(page.getByRole("heading", { level: 1 })).toContainText("Welcome back");
    await expect(page.getByRole("heading", { name: "Continue learning" })).toBeVisible();
    await expectHealthyPage(page, problems);
    await screenshot(page, testInfo, "10-student-dashboard");

    await page.goto("/Student/MyCourses");
    await expect(page.getByRole("heading", { level: 1, name: "My courses" })).toBeVisible();
    await expectHealthyPage(page, problems);
    await screenshot(page, testInfo, "11-my-courses");

    await page.getByRole("link", { name: "ASP.NET Core MVC in Practice" }).first().click();
    await expect(page.getByRole("heading", { name: "Your progress" })).toBeVisible();
    await expectHealthyPage(page, problems);
    await screenshot(page, testInfo, "12-enrolled-course");

    await page.getByRole("link", { name: "Models, validation and ModelState" }).click();
    await expect(page.getByRole("heading", { level: 1, name: "Models, validation and ModelState" })).toBeVisible();
    await expectHealthyPage(page, problems);
    await screenshot(page, testInfo, "13-lesson");

    await page.goBack();
    await page.getByRole("link", { name: "MVC architecture quiz" }).click();
    await expect(page.getByRole("heading", { level: 1, name: "MVC architecture quiz" })).toBeVisible();

    const questions = page.locator("fieldset[data-question]");
    const count = await questions.count();
    for (let index = 0; index < count; index++) {
      await questions.nth(index).locator("label").nth(1).click();
    }
    await expect(page.getByText(`${count} of ${count} answered`)).toBeVisible();
    await screenshot(page, testInfo, "14-quiz");

    await page.getByRole("button", { name: "Submit answers" }).click();
    await expect(page).toHaveURL(/\/Quizzes\/Result\/\d+$/);
    await expect(page.getByRole("heading", { name: "Answer review" })).toBeVisible();
    await expectHealthyPage(page, problems);
    await screenshot(page, testInfo, "15-quiz-result");
  });
});
