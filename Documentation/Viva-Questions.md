# LearnHub – Viva Preparation

Likely questions with model answers based on this code base. File paths are relative to `src/LearnHub/` unless they
start with `tests/`, `.github/`, `infra/` or `scripts/`. Every member should be able to answer the architecture,
database and security questions, not only those in their own area.

## 1. Project and requirements

**Q1. What problem does LearnHub solve?**
Computing students often study from scattered material with no order and no feedback. LearnHub gives them structured
courses (lessons and quizzes in a fixed route), explanations after every quiz and a dashboard that shows progress.
Administrators get one protected area to manage courses, files, quizzes, users and enrolments.

**Q2. Who are the users and what can each do?**
Guests browse, search and filter the catalogue, open course details and preview lessons, register, log in and send
contact messages. Students additionally enrol, study lessons, mark them complete, take quizzes, review results and
edit their profile. Administrators manage every entity in `/Admin` and see platform statistics.

**Q3. What was deliberately left out, and why?**
Payments, certificates, chat, forums, an instructor role, email delivery and multiple languages. They would add
external services or large features without improving the core learning journey the brief assesses. They are
recorded as future enhancements so the scope stayed under control.

**Q4. How do you prove every requirement was met?**
`Documentation/Requirements-Checklist.md` gives each requirement an ID, and `Documentation/Requirements-Audit.md` maps
each ID to the implementing files and to test or screenshot evidence.

## 2. Architecture and MVC

**Q5. Describe the architecture.**
One ASP.NET Core MVC application in layers: controllers handle HTTP concerns (binding, `ModelState`, redirects and
authorisation attributes); services in `Services/` hold business rules and queries; EF Core's `ApplicationDbContext`
talks to the database. Views only display view models. We avoided extra projects or patterns such as CQRS because they
would add complexity without benefit at this size.

**Q6. Walk through what happens when a student opens a course page.**
Routing maps `/Courses/Details/5` to `CoursesController.Details`. Middleware has already applied security headers,
authentication and authorisation. The action asks `ICourseCatalogService` for a `CourseDetailsViewModel`, which runs an
EF Core query (published course, route items, enrolment and progress for this user). The controller returns the view,
which Razor renders with the layout.

**Q7. Why do services return view models or `OperationResult` instead of entities?**
Views then receive exactly the data they need, queries select only those columns, and controllers can react to
outcomes (success, not found, validation failure) without knowing EF Core. `Services/OperationResult.cs` makes those
outcomes explicit.

**Q8. How are services wired up?**
Dependency injection in `Infrastructure/ServiceCollectionExtensions.cs`: services are scoped (one per request, like the
DbContext); `FileStorageService` and `TimeProvider.System` are singletons. Controllers receive interfaces through
constructors, which also lets tests replace them.

**Q9. Why is `TimeProvider` injected instead of calling `DateTime.UtcNow`?**
Services that record times (enrolments, attempts, completions) can be tested with a fixed clock, and the seeder can
create realistic past activity relative to one "now".

**Q10. What are Areas and why use one for the admin?**
An Area groups controllers and views under `/Admin` with its own layout (`Areas/Admin/Views/Shared/_AdminLayout.cshtml`).
Every admin controller inherits `Areas/Admin/Controllers/AdminControllerBase.cs`, which carries `[Area("Admin")]` and
`[Authorize(Roles = "Admin")]`, so a new admin page cannot be added without protection.

## 3. Razor views and front end

**Q11. What are tag helpers? Give examples from the project.**
Server-side components that turn attributes into HTML. Built-in ones such as `asp-for`, `asp-action` and
`asp-validation-for` generate names, URLs, antiforgery tokens and validation attributes. We wrote our own in
`Infrastructure/TagHelpers.cs`: `lh-nav` highlights the current navigation link and sets `aria-current="page"`, and
`<course-cover>` renders a cover image or a fallback icon.

**Q12. How did you make the site look like LearnHub rather than default Bootstrap?**
`wwwroot/css/site.css` defines design tokens (ink, paper, signal yellow, eight category line colours, Overpass font,
spacing and radii) and restyles components around the interchange idea: category lines, course routes, progress bars
with a signal marker, and the home page map. Bootstrap provides the grid, utilities and accessible components.

**Q13. How is the home page map built?**
`Infrastructure/InterchangeMap.cs` builds an SVG with `TagBuilder` (so every value is HTML-encoded): up to eight
category lines with 45° bends meeting at the hub, each line a link to the filtered catalogue with an accessible name.
The CSS animates the lines drawing in only when the user has not asked for reduced motion; the resting style is the
finished map.

**Q14. What does JavaScript do, and does the site work without it?**
`wwwroot/js/site.js` adds progressive enhancements: local time formatting, character counters, the password strength
meter, image previews, the quiz answered counter and unanswered warning, confirmation prompts, auto-submitting
filters, type-specific resource fields and the answer option editor. Every rule is enforced on the server, so forms
still work and stay safe without JavaScript.

**Q15. Why are Bootstrap and jQuery stored in `wwwroot/lib` instead of loaded from a CDN?**
The Content Security Policy allows scripts, styles and fonts only from our own origin (`'self'`). Loading from a CDN
would require weakening the policy and would make the site depend on a third party.

**Q16. How did you make the interface responsive and accessible?**
Mobile-first CSS with Bootstrap breakpoints: the navbar collapses, the admin sidebar becomes off-canvas below 992 px,
admin tables become labelled stacked records below 768 px, and course grids change column counts. Accessibility:
semantic landmarks, skip links, labels and hints linked with `aria-describedby`, visible focus styles, `fieldset` and
`legend` for quiz questions, and reduced-motion support. Playwright tests run at three sizes, and axe-core reports no
WCAG 2.2 A/AA violations.

## 4. Validation

**Q17. How does validation work on the client and on the server?**
View models carry DataAnnotations such as `[Required]`, `[StringLength]`, `[EmailAddress]`, `[RegularExpression]`
and `[Compare]` (see `ViewModels/Account/AccountViewModels.cs`). Tag helpers turn them into `data-val-*` attributes
that jQuery Validation Unobtrusive enforces in the browser. On submit, model binding validates again and controllers
check `ModelState.IsValid` before calling services, because client checks can be bypassed.

**Q18. Did you write custom validation attributes?**
Yes. `Infrastructure/MustBeTrueAttribute.cs` requires the terms checkbox, and `Infrastructure/FileValidationAttributes.cs`
checks upload sizes and extensions. Both implement `IClientModelValidator`, so the same rule also runs in the browser
through `wwwroot/js/validation.js`.

**Q19. Which rules cannot be expressed with attributes?**
Business rules that need the database or several fields: unique category names, exactly one correct answer among two to
six options, a quiz needing a question before publishing, enrolment only in published courses, and protection of the
last administrator. Services check them and return `OperationResult` failures that controllers add to `ModelState`.

**Q20. How do you keep form limits and database column sizes consistent?**
`Models/FieldLengths.cs` is the single source of truth: EF Core configurations use it for `HasMaxLength` and view
models use it in `[StringLength]`, so a form can never accept more than the column holds.

## 5. Database design

**Q21. Explain the main entities and relationships.**
A category has many courses. A course has many learning resources, quizzes and enrolments. A quiz has questions, a
question has answer options. A student has enrolments, resource completions and quiz attempts; an attempt has one quiz
answer per question. Contact messages stand alone. The full diagram is in `Documentation/ERD.md`.

**Q22. Which constraints protect data integrity?**
Primary and foreign keys; required columns with maximum lengths; unique indexes on category name,
`(UserId, CourseId)` for enrolments, `(UserId, LearningResourceId)` for completions and `(QuizAttemptId, QuestionId)`
for answers; and check constraints such as `DurationMinutes > 0` and `ScorePercent BETWEEN 0 AND 100`
(`Data/Configurations/`).

**Q23. Why does deleting a category restrict, while deleting a course cascades?**
A category is a label shared by many courses; deleting it by mistake must not wipe courses, so the delete is blocked
while courses exist. A course owns its lessons, quizzes and enrolments, which are meaningless without it, and the
confirmation page shows exactly what will be removed.

**Q24. Why are `QuizAnswer → Question` and `QuizAnswer → AnswerOption` set to Restrict?**
With cascades there would be two delete paths from a quiz to its answers (through attempts and through questions), and
SQL Server rejects multiple cascade paths. So the services delete the dependent answers first with `ExecuteDeleteAsync`
and then the parent, inside one transaction.

**Q25. Why do quiz attempts store the score instead of recalculating it?**
Administrators can edit a quiz after students took it. Storing `CorrectCount`, `QuestionCount`, `ScorePercent`,
`Passed` and `IsCorrect` per answer keeps every student's history truthful.

**Q26. And why is course progress not stored?**
Progress depends on the current number of lessons and published quizzes. Calculating it from completions and passed
attempts means adding a lesson correctly lowers everyone's percentage, with no stale column to update.

**Q27. How are dates handled?**
All `DateTime` values are stored as UTC through a value converter configured in `Data/ApplicationDbContext.cs`, and
`CreatedAt` and `UpdatedAt` are set automatically in `SaveChanges`. The browser converts them to local time.

## 6. Entity Framework Core

**Q28. How do you support SQLite and SQL Server with one model?**
`ApplicationDbContext` is abstract and holds the whole model. `SqliteDbContext` and `SqlServerDbContext` inherit it,
and each has its own migrations folder because providers generate different column types.
`Data/DatabaseServiceCollectionExtensions.cs` registers the one selected by `Database:Provider`; the rest of the
application depends only on `ApplicationDbContext`.

**Q29. How are migrations created and applied?**
`dotnet ef migrations add Name --context SqliteDbContext --output-dir Data/Migrations/Sqlite`, and the same for
`SqlServerDbContext`, or the "EF Core migration (cloud)" workflow for members without the SDK. They are applied at
start-up by `Data/Seed/DatabaseInitializer.cs`, and CI applies all SQL Server migrations to an empty database on every
push.

**Q30. You found a query that worked on SQL Server but failed on SQLite. What happened?**
The admin course details query projected nested collections with `Take`, which EF Core translates using SQL `APPLY`.
SQLite does not support `APPLY`, so the page returned 500 in tests. We rewrote it as separate flat queries, which work
on both providers (`Services/CourseManagementService.cs`).

**Q31. Why use `AsNoTracking`, `ExecuteDeleteAsync` and projections?**
Read-only queries use `AsNoTracking` and project to view models, so EF Core neither tracks entities nor loads unneeded
columns. `ExecuteDeleteAsync` deletes rows in the database with one statement, which avoids loading whole object graphs
and avoids the relationship errors we hit when deleting tracked dependants.

**Q32. How do transactions work with Azure SQL's retry policy?**
The SQL Server provider uses `EnableRetryOnFailure`, which does not allow user transactions started directly.
`Data/TransactionExtensions.cs` runs the transaction inside `Database.CreateExecutionStrategy().ExecuteAsync`, so the
whole unit is retried together after a transient failure.

**Q33. Is LINQ safe from SQL injection?**
Yes: EF Core sends values as parameters. The catalogue search uses `EF.Functions.Like` with a pattern built by
`Services/SearchPattern.cs`, which also escapes `%`, `_` and `[` so they are matched literally.

**Q34. What happens if two requests enrol the same student at the same time?**
Both may pass the "already enrolled" check, but the unique `(UserId, CourseId)` index rejects the second insert.
`Services/EnrollmentService.cs` catches the `DbUpdateException`, clears the change tracker and shows "You are already
enrolled in this course." instead of an error page.

## 7. Authentication and authorisation

**Q35. How are passwords stored?**
ASP.NET Core Identity stores a salted PBKDF2 hash with many iterations, never the password. The policy (8–100
characters with upper-case, lower-case and a digit) is applied in Identity options and in the registration view model.

**Q36. How are roles assigned?**
Registration adds the `Student` role. The first administrator is created at start-up from configuration
(`Seed:AdminEmail`, `Seed:AdminPassword`) if no such account exists; after that, administrators change roles in the
admin area.

**Q37. Why is the admin password not in `appsettings.json`?**
Anything in Git can leak. Locally it is set with `dotnet user-secrets`; in production it is an App Service setting that
`infra/provision.sh` asks for without displaying it and that should be removed after the first login.

**Q38. What protects against brute-force login attempts?**
Identity lockout (five failed attempts lock the account for 15 minutes), a generic error message that does not reveal
whether an email exists, and a rate limit of 10 form submissions per minute per IP on login, registration, password
change and contact.

**Q39. How does deactivating a user work, and how fast does it take effect?**
`Services/UserManagementService.cs` sets `LockoutEnd` to the maximum date and updates the security stamp. Identity
refuses future sign-ins, and because the cookie's security stamp is re-validated every five minutes, an existing session
ends within that time. Administrators cannot deactivate, demote or delete themselves or the last active administrator.

**Q40. How does the app redirect users after login without an open redirect?**
`AccountController.RedirectToLocal` follows `returnUrl` only when `Url.IsLocalUrl` accepts it; otherwise administrators
go to `/Admin` and students to their dashboard. A test tries an external URL and expects the dashboard.

## 8. Security

**Q41. How do you prevent CSRF?**
`AutoValidateAntiforgeryTokenAttribute` is registered globally, so every unsafe HTTP method needs a valid token, which
the form tag helper adds automatically. All state changes are POST forms. A test posts to login without a token and
expects 400.

**Q42. How do you prevent XSS, especially in lesson content written by admins?**
Razor encodes all output and the views never use `Html.Raw` on user input. Lesson text goes through
`Services/Content/LessonContentRenderer.cs`, which encodes everything first and then applies a fixed allow-list of
formatting. A strict CSP blocks inline scripts as a second layer.

**Q43. What is IDOR and where did you prevent it?**
Insecure direct object reference: changing an id in a URL to reach someone else's data. `QuizService` returns a result
only for the attempt's owner (others get 404), lessons and files require enrolment unless they are previews, and admin
actions require the Admin role. Tests cover each case.

**Q44. Why not bind the entity directly in forms?**
Overposting: a user could add fields such as a role flag or `IsPublished` to the request. Forms bind to view models that
contain only editable fields, and services copy those values onto entities.

**Q45. How are file uploads secured?**
`Services/Storage/FileStorageService.cs` checks the declared size, the extension allow-list and the file signature (the
first bytes must be JPEG, PNG, WebP or PDF), saves under a random name outside `wwwroot`, and enforces the size again on
the bytes actually written. Resource files are streamed by `ResourcesController.Open` after the access check.

**Q46. How are embedded videos kept safe?**
`Services/Content/VideoEmbedParser.cs` accepts only known YouTube and Vimeo URL formats, extracts the video id and builds
a `youtube-nocookie.com` or `player.vimeo.com` embed URL itself. The CSP `frame-src` allows only those two players.

**Q47. Which security headers are sent?**
`Infrastructure/SecurityHeadersMiddleware.cs` sends a Content Security Policy, `X-Content-Type-Options: nosniff`,
`X-Frame-Options: SAMEORIGIN`, `Referrer-Policy`, `Permissions-Policy` and `Cross-Origin-Opener-Policy`; production also
uses HTTPS redirection and HSTS.

**Q48. How does the deployed app connect to the database without a password?**
The web app has a system-assigned managed identity. Azure SQL uses Microsoft Entra authentication only, and a database
user for that identity has only reader, writer and DDL roles. The connection string says
`Authentication=Active Directory Managed Identity`; the platform supplies tokens automatically.

**Q49. How does GitHub deploy to Azure without a stored secret?**
OpenID Connect federation: GitHub issues a short-lived token for the `production` environment of this repository,
Microsoft Entra trusts that exact subject for a user-assigned managed identity, and `azure/login` exchanges it for an
Azure token. The identity only has Website Contributor on the web app.

## 9. Testing

**Q50. What kinds of tests did you write?**
Unit tests (grading, progress, validation, content safety, file signatures), service tests against a real in-memory
SQLite database, integration tests that run the whole app in memory with `WebApplicationFactory`, a crawler that follows
every link as each role, Playwright browser tests at three screen sizes with axe-core accessibility scans, and smoke
tests of the published app. In total 182 .NET test cases and 42 browser checks.

**Q51. How do integration tests log in and submit forms?**
`tests/LearnHub.Tests/Infrastructure/BrowserClientExtensions.cs` gets the page, extracts the antiforgery token from the
HTML, posts the form with cookies kept by the client and follows redirects manually, just like a browser.

**Q52. Why run the tests on SQL Server as well?**
SQLite and SQL Server translate some LINQ differently and enforce different rules (for example cascade paths). CI starts
a SQL Server 2022 container and runs the same 182 tests against it, because production uses Azure SQL.

**Q53. Which bugs did testing find?**
The SQLite `APPLY` failure, a relationship error when deleting courses, form pages failing behind a TLS proxy until
forwarded headers were enabled, several layouts scrolling sideways on phones and tablets, a transparent admin sidebar,
map labels overlapping, and accessibility issues (unfocusable scroll regions, links without names on phones). All are
listed with their fixes in `Documentation/Testing.md`.

## 10. Deployment and DevOps

**Q54. Why is the application on Azure App Service while GitHub Pages hosts a separate site?**
GitHub Pages serves only static files and cannot run ASP.NET Core or a database. Pages hosts the presentation site in
`docs/`; its "Launch LearnHub" buttons link to the App Service.

**Q55. What do the GitHub Actions workflows do?**
`ci.yml` builds and tests on SQLite, runs migrations, tests and a smoke test on SQL Server, and runs the browser tests.
`deploy.yml` builds, tests, publishes and deploys to Azure with OIDC, then smoke tests the live site. `pages.yml`
publishes `docs/`. `ef-migrations.yml` generates migrations for both providers.

**Q56. Why is `ASPNETCORE_FORWARDEDHEADERS_ENABLED` needed on App Service?**
TLS ends at App Service's front end, which forwards plain HTTP to the app with `X-Forwarded-Proto: https`. Without
forwarded headers the app thinks requests are HTTP: the Secure-only antiforgery cookie is refused and HTTPS redirection
could loop. CI reproduces this setup in the smoke test.

**Q57. Is applying migrations at start-up safe?**
For this single-instance deployment, yes, and EF Core takes a migration lock so two instances cannot migrate at once.
For larger systems we would apply the idempotent script, which the deploy workflow already produces, as a separate
release step.

## 11. Teamwork and reflection

**Q58. How did the team avoid conflicts?**
Clear ownership per area, short-lived branches, pull requests with green CI and review, small changes to shared files
such as `site.css` and the EF Core model, and regenerating migrations instead of hand-merging snapshots.

**Q59. What are the main limitations?**
No email confirmation or password reset by email, no multi-factor authentication, a single-instance design (start-up
migrations, local file storage), free-tier cold starts, and no alerting yet.

**Q60. What would you add next?**
Email confirmation and password reset, MFA for administrators, certificates on course completion, an instructor role,
Application Insights alerts, Dependabot for dependency updates, and file storage in Azure Blob Storage for scale-out.
