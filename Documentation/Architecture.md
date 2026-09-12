# LearnHub – Architecture and Design

## 1. Architectural style

LearnHub is a single ASP.NET Core MVC application (.NET 10 LTS) organised in clear,
conventional layers. We deliberately avoided Clean Architecture / CQRS / microservices:
the goal is a maintainable application that every team member can explain.

```
Browser (HTML5, CSS3, Bootstrap 5, JavaScript)
        │  HTTP(S) requests, forms with anti-forgery tokens
        ▼
Controllers (+ Admin Area controllers)      ← HTTP concerns only: binding, ModelState, redirects
        │  ViewModels in / ViewModels out
        ▼
Services (business rules, queries)          ← enrolment, progress, grading, file validation …
        │  LINQ
        ▼
EF Core ApplicationDbContext (+ Identity)   ← SQLite (development) / Azure SQL (production)
```

| Layer | Folder | Responsibility |
|-------|--------|----------------|
| Models | `Models/` | EF Core entities and enums (the database shape) |
| Data | `Data/` | DbContext, Fluent API configurations, migrations, seeding |
| Services | `Services/` | Business logic and queries; returns ViewModels or `OperationResult` |
| ViewModels | `ViewModels/` | Strongly typed page and form models with DataAnnotations |
| Controllers | `Controllers/`, `Areas/Admin/Controllers/` | Thin HTTP layer, authorisation attributes |
| Views | `Views/`, `Areas/Admin/Views/` | Razor markup only – no business logic |
| Infrastructure | `Infrastructure/` | Cross-cutting: security headers, validation attributes, extensions |
| Static assets | `wwwroot/` | CSS design system, JavaScript, images, vendored libraries |

Entities are never bound directly from forms (overposting protection). Controllers
receive ViewModels, services map them onto entities.

## 2. Solution structure

```
LearnHub/
├── .github/workflows/        ci.yml · deploy.yml · pages.yml · ef-migrations.yml
├── src/LearnHub/             ASP.NET Core MVC application
├── tests/LearnHub.Tests/     xUnit unit + integration tests (WebApplicationFactory)
├── tests/e2e/                Playwright browser tests (mobile / tablet / desktop)
├── infra/                    Azure provisioning script
├── Documentation/            Assignment documentation
├── docs/                     GitHub Pages presentation website (static)
├── LearnHub.sln
└── README.md
```

`src/` and `tests/` are separated because an SDK-style web project compiles every `*.cs`
file below its folder; keeping tests outside the web project folder avoids accidental
compilation of test code into the application.

## 3. Database design decisions

The full, code-accurate ERD is in [ERD.md](ERD.md).

### 3.1 Providers and migrations

EF Core migrations are provider-specific (SQLite and SQL Server generate different column
types). LearnHub therefore uses one provider-agnostic model with two thin context types:

| Context | Provider | Migrations folder | Used when |
|---------|----------|-------------------|-----------|
| `SqliteDbContext` | SQLite | `Data/Migrations/Sqlite` | `Database:Provider = Sqlite` (development, tests) |
| `SqlServerDbContext` | SQL Server / Azure SQL | `Data/Migrations/SqlServer` | `Database:Provider = SqlServer` (production) |

Both inherit the abstract `ApplicationDbContext`, which contains all entity sets and the
Fluent API model. The application always depends on `ApplicationDbContext`; dependency
injection supplies the provider-specific subclass.

### 3.2 Delete behaviour (intentional)

| Relationship | Behaviour | Reason |
|--------------|-----------|--------|
| Category → Courses | **Restrict** | Deleting a category must never silently delete courses. The admin must move or delete courses first. |
| Course → Resources, Quizzes, Enrolments | Cascade | A course owns its content; the delete page shows the full impact before confirmation. |
| Quiz → Questions → AnswerOptions | Cascade | Questions cannot exist without their quiz. |
| Quiz → QuizAttempts → QuizAnswers | Cascade | Attempts are meaningless without the quiz. |
| QuizAnswer → Question / AnswerOption | **Restrict** | Prevents SQL Server "multiple cascade paths"; services remove dependent answers explicitly inside a transaction. |
| User → Enrolments, QuizAttempts, ResourceCompletions | Cascade | Removing an account removes that person's learning records. |
| LearningResource → ResourceCompletions | Cascade | Completion records belong to the resource. |

### 3.3 Derived rather than duplicated state

* **Course progress** is calculated, not stored: completed resources + passed published
  quizzes divided by total resources + published quizzes. Adding a new lesson therefore
  correctly lowers progress, and there is no stale `ProgressPercent` column.
* **Quiz attempts** store a score snapshot (`CorrectCount`, `QuestionCount`, `ScorePercent`,
  `Passed`) because the quiz may be edited after the attempt; history must stay truthful.
* **Deactivated users** use Identity's built-in lockout (`LockoutEnd = DateTimeOffset.MaxValue`)
  rather than a duplicate `IsActive` flag.

## 4. Roles and authorisation

| Role | How obtained | Scope |
|------|--------------|-------|
| Guest | Not signed in | Public pages, published courses, preview resources |
| Student | Automatically on registration | Dashboard, enrolment, resources of enrolled courses, quizzes, profile |
| Admin | Seeded from configuration or promoted by another admin | Everything in `/Admin` plus preview of all content |

Authorisation is declarative: `[Authorize(Roles = AppRoles.Student)]` on student
controllers and a shared `AdminControllerBase` with `[Area("Admin")]` and
`[Authorize(Roles = AppRoles.Admin)]` that every admin controller inherits, so a new admin
controller cannot be added without protection. Ownership checks (IDOR) are done in services
(for example a quiz result is only returned when `attempt.UserId == currentUserId`).

## 5. Routes

### Public and student

| Route | Controller.Action | Access |
|-------|-------------------|--------|
| `/` | Home.Index | Everyone |
| `/About`, `/Contact`, `/Privacy` | Home.About / Contact / Privacy | Everyone |
| `/Courses?q=&categoryId=&difficulty=&sort=&page=` | Courses.Index | Everyone |
| `/Courses/Details/{id}` | Courses.Details | Everyone (published courses) |
| `POST /Courses/Enroll/{id}` · `POST /Courses/Leave/{id}` | Courses.Enroll / Leave | Student |
| `/Resources/Details/{id}` | Resources.Details | Preview: everyone · otherwise enrolled student or admin |
| `/Resources/Open/{id}` | Resources.Open (file stream) | Same rule as above |
| `POST /Resources/ToggleComplete/{id}` | Resources.ToggleComplete | Enrolled student |
| `/Quizzes/Take/{id}` · `POST /Quizzes/Take/{id}` | Quizzes.Take | Enrolled student |
| `/Quizzes/Result/{attemptId}` · `/Quizzes/History` | Quizzes.Result / History | Owner student |
| `/Student/Dashboard` · `/Student/MyCourses` | Student.Dashboard / MyCourses | Student |
| `/Profile` · `/Profile/ChangePassword` | Profile.Index / ChangePassword | Signed-in user |
| `/Account/Register` · `/Account/Login` · `POST /Account/Logout` · `/Account/AccessDenied` | Account.* | Everyone |
| `/Error/{statusCode}` · `/Error` | Error.* | Everyone |
| `/health` | Health check (database connectivity) | Everyone |

### Admin area (`[Authorize(Roles = "Admin")]`)

| Route | Purpose |
|-------|---------|
| `/Admin` | Dashboard |
| `/Admin/Courses` (+ `/Create`, `/Edit/{id}`, `/Details/{id}`, `/Delete/{id}`) | Course CRUD |
| `/Admin/Categories` (+ Create/Edit/Delete) | Category CRUD |
| `/Admin/Resources` (+ Create/Edit/Delete) | Learning resource CRUD |
| `/Admin/Quizzes` (+ Create/Edit/Details/Delete) | Quiz CRUD |
| `/Admin/Questions/Create?quizId=` (+ Edit/Delete) | Question and answer option CRUD |
| `/Admin/QuizAttempts` (+ Details/Delete) | Quiz results |
| `/Admin/Enrollments` (+ Create/Delete) | Enrolment management |
| `/Admin/Users` (+ Details, role change, deactivate, Delete) | User management |
| `/Admin/Messages` (+ Details/Delete) | Contact message inbox |

## 6. Security design

| Threat | Control |
|--------|---------|
| SQL injection | Only EF Core LINQ (parameterised). Search uses `EF.Functions.Like` with escaped wildcards. |
| XSS | Razor HTML-encodes all output. Lesson text is encoded first, then a fixed allow-list of tags is applied (`LessonContentRenderer`). Strict Content-Security-Policy without inline scripts. |
| CSRF | Global `AutoValidateAntiforgeryTokenAttribute`; all state changes are POST forms with tokens. |
| IDOR | Services check ownership/enrolment for attempts, resources and files. Resource files are streamed by a controller after an access check, never exposed as static files. |
| Overposting | Forms bind to ViewModels only. |
| File uploads | Extension allow-list, size limit, magic-byte signature check, random file names, storage outside `wwwroot`, SVG/HTML never accepted. |
| Video embeds | URLs parsed with strict host/ID rules and rebuilt as `youtube-nocookie.com` / `player.vimeo.com` embed URLs; CSP `frame-src` allow-list. |
| Brute force | Identity lockout (5 attempts / 15 minutes) and rate limiting on login, registration and contact. |
| Session security | HttpOnly, SameSite=Lax, Secure (production) cookies; security stamp re-validated every 5 minutes so role changes and deactivation take effect. |
| Secrets | Admin seed password and connection strings come from user-secrets (development) or App Service settings (production). Nothing sensitive is committed. |
| Information disclosure | Developer exception page only in Development; friendly error pages elsewhere; security headers (`X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options`, `Permissions-Policy`, HSTS). |

## 7. Front-end design

* Bootstrap 5.3 provides the grid, utilities and accessible components.
* `wwwroot/css/site.css` defines the LearnHub design system (colour tokens, typography,
  spacing, radii, shadows) and component styles (navbar, hero, course cards, dashboard,
  tables, forms, empty states). `admin.css` adds the admin shell.
* JavaScript is progressive enhancement only: validation styling and ARIA wiring,
  password strength meter, character counters, image preview, quiz answered-count and
  unanswered warning, local-time formatting, confirm dialogs. Every feature works without
  JavaScript because validation and processing are repeated on the server.
* Libraries are vendored in `wwwroot/lib` (no runtime CDN), which keeps the CSP strict.

## 8. Deployment architecture

```
Developer → GitHub (main) ──► GitHub Actions CI (build, tests, SQL Server migration check, e2e)
                          ├─► deploy.yml ──► Azure App Service (Linux, .NET 10) ──► Azure SQL Database
                          └─► pages.yml  ──► GitHub Pages (static presentation site)
                                                   │ "Launch LearnHub"
                                                   └────────────► Azure App Service
```

* GitHub Pages only hosts static files, so it presents the project; the ASP.NET Core
  application itself runs on Azure App Service.
* Production configuration is supplied through App Service settings (environment
  variables): `ASPNETCORE_ENVIRONMENT`, `Database__Provider`, the `SqlServer` connection
  string, `Storage__RootPath`, `Seed__AdminEmail`, `Seed__AdminPassword`.
* Migration strategy: migrations are applied at application start-up
  (`Database:ApplyMigrationsOnStartup`), which is safe for the single-instance student
  deployment. The deploy workflow also publishes an idempotent SQL script as a build
  artefact for review or manual application.

## 9. Cloud-based development workflow

Development machines with little free disk space do not need the .NET SDK locally:
GitHub Actions restores, builds, tests, generates EF Core migrations
(`ef-migrations.yml`) and runs browser tests in the cloud. Team members with the SDK
installed can use the normal local commands documented in the README.
