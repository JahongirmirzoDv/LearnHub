# LearnHub – Requirements Audit

Each requirement from [Requirements-Checklist.md](Requirements-Checklist.md) with its implementation, evidence and status.
Paths without a prefix are relative to `src/LearnHub/`. Test evidence refers to GitHub Actions run
[34800787823](https://github.com/JahongirmirzoDv/LearnHub/actions/runs/34800787823) unless stated otherwise.

## Summary

| | Total | Pass | Blocked |
|---|---:|---:|---:|
| Mandatory | 110 | 108 | 2 |
| Optional | 6 | 6 | 0 |

**Blocked (external):** DEP-01 and DEP-02 need the team's Azure sign-in. Everything up to that step is built and
verified: the provisioning script, the OIDC deployment workflow, the Production configuration (smoke-tested in CI behind a
simulated TLS proxy) and the full test suite plus migrations on SQL Server 2022. Running `infra/provision.sh` in Azure
Cloud Shell and adding the five repository variables completes both (see [Deployment.md](Deployment.md)).


## Technology and platform

| ID | Requirement | Type | Implementation | Evidence | Status |
|----|-------------|------|----------------|----------|--------|
| TECH-01 | Built with .NET technologies – ASP.NET Core MVC on the latest stable LTS (.NET 10) | M | `src/LearnHub/LearnHub.csproj` targets .NET 10 (`Directory.Build.props`), ASP.NET Core MVC | CI build | PASS |
| TECH-02 | C# for all server-side code | M | All server code in C# | Repository | PASS |
| TECH-03 | Database connectivity through Entity Framework Core | M | `src/LearnHub/Data/ApplicationDbContext.cs`, EF Core 10 LINQ in `src/LearnHub/Services/` | Integration tests on SQLite and SQL Server | PASS |
| TECH-04 | SQLite for local development, Azure SQL / SQL Server for production | M | `SqliteDbContext` / `SqlServerDbContext`, provider chosen by `Database:Provider` (`src/LearnHub/Data/DatabaseServiceCollectionExtensions.cs`) | CI jobs: SQLite and SQL Server 2022 (182/182 each) | PASS |
| TECH-05 | ASP.NET Core Identity for authentication | M | Identity with roles in `src/LearnHub/Infrastructure/ServiceCollectionExtensions.cs` | `AuthenticationTests` | PASS |
| TECH-06 | Razor views, HTML5 semantic markup | M | Razor views with semantic landmarks in `src/LearnHub/Views/`, `src/LearnHub/Areas/Admin/Views/` | Screenshots; axe-core scans | PASS |
| TECH-07 | CSS3 with Bootstrap 5 customised by a project design system | M | Bootstrap 5.3.8 plus design system in `src/LearnHub/wwwroot/css/site.css` and `admin.css` | Screenshots in `docs/assets/screens/` | PASS |
| TECH-08 | JavaScript for client-side processing where appropriate | M | Progressive enhancement in `src/LearnHub/wwwroot/js/site.js`, client validation in `validation.js` | Browser tests (quiz counter, password meter, validation) | PASS |
| TECH-09 | Multimedia (images, embedded video, PDF documents, SVG illustrations) | M | SVG covers, PNG diagrams, embedded YouTube/Vimeo video, PDF cheat sheets, interactive SVG map | `AdminManagementTests.Resources_are_created_validated_streamed_and_deleted`; lesson screenshot | PASS |
| TECH-10 | Server-side processing (controllers, services, EF Core queries) | M | Controllers, services and EF Core queries in `src/LearnHub/Controllers/`, `src/LearnHub/Services/` | Integration tests | PASS |
| TECH-11 | Proper project and file organisation | M | `src/`, `tests/`, `Documentation/`, `docs/`, `infra/`, `scripts/`; layered folders (Architecture.md §2) | Repository structure | PASS |

## Guest

| ID | Requirement | Type | Implementation | Evidence | Status |
|----|-------------|------|----------------|----------|--------|
| GUEST-01 | View Home page | M | `HomeController.Index`, `Views/Home/Index.cshtml` | `PublicSiteTests.Public_pages_load_for_guests`; home screenshot | PASS |
| GUEST-02 | View About page | M | `HomeController.About` | `PublicSiteTests.Public_pages_load_for_guests`; browser test | PASS |
| GUEST-03 | Browse published courses | M | `CoursesController.Index`, `CourseCatalogService` | `PublicSiteTests.Seeded_catalogue_is_shown_with_published_courses_only` | PASS |
| GUEST-04 | Search courses (server-side) | M | Server-side `EF.Functions.Like` search with escaped wildcards (`Services/SearchPattern.cs`) | `PublicSiteTests.Search_is_performed_on_the_server`; browser test | PASS |
| GUEST-05 | Filter courses by category and difficulty | M | Category and difficulty filters, sorting, pagination | `PublicSiteTests.Category_filter_limits_results_to_that_category`; `CatalogueServiceTests` | PASS |
| GUEST-06 | Open course details | M | `CoursesController.Details` with course route | `PublicSiteTests.Course_details_show_route_and_enrolment_call_to_action` | PASS |
| GUEST-07 | View selected public (preview) learning resources | M | `IsPreview` resources open to guests (`LearningResourceService`) | `PublicSiteTests.Preview_lessons_are_open_to_guests_but_other_lessons_require_login` | PASS |
| GUEST-08 | Register | M | `AccountController.Register` | `AuthenticationTests.Registration_creates_a_student_signs_them_in_and_hashes_the_password` | PASS |
| GUEST-09 | Login | M | `AccountController.Login` | `AuthenticationTests.Students_and_admins_are_sent_to_their_own_dashboards_after_login` | PASS |
| GUEST-10 | View Contact page and send a contact message | M | `HomeController.Contact`, `ContactService` (stored, honeypot, rate limited) | `AdminManagementTests.Contact_messages_reach_the_admin_inbox`; browser test | PASS |

## Student

| ID | Requirement | Type | Implementation | Evidence | Status |
|----|-------------|------|----------------|----------|--------|
| STU-01 | Student dashboard with meaningful, data-driven information | M | `StudentController.Dashboard`, `DashboardService` | Student browser test; dashboard screenshot | PASS |
| STU-02 | Dashboard: enrolled courses, completed courses, available courses | M | Dashboard figures: enrolled, completed and available courses | Dashboard screenshot | PASS |
| STU-03 | Dashboard: recent activity, quiz results, learning progress | M | Recent activity, recent quiz results, progress bars, recommendations | Dashboard screenshot; `CatalogueServiceTests.Progress_counts_completed_lessons_and_passed_quizzes` | PASS |
| STU-04 | View and update profile | M | `ProfileController.Index` | `StudentJourneyTests.Profile_updates_are_validated_and_saved` | PASS |
| STU-05 | Browse courses and view course details | M | Catalogue and details available to students | Student browser test | PASS |
| STU-06 | Enrol in a course (and leave a course) | M | `CoursesController.Enroll` / `Leave`, `EnrollmentService` | `CatalogueServiceTests.Enrolling_twice_is_rejected_and_unpublished_courses_cannot_be_joined`; `StudentJourneyTests.Leaving_a_course_removes_access_to_its_lessons` | PASS |
| STU-07 | View My Courses | M | `StudentController.MyCourses` | Student browser test (My Courses screenshot) | PASS |
| STU-08 | Access learning resources of enrolled courses | M | `ResourcesController.Details` / `Open` with enrolment check | `StudentJourneyTests.A_new_student_enrols_studies_takes_the_quiz_and_sees_the_result` | PASS |
| STU-09 | Track learning progress (mark resources complete) | M | `ResourcesController.ToggleComplete`, derived progress (`ProgressService`) | `CatalogueServiceTests.Progress_counts_completed_lessons_and_passed_quizzes` | PASS |
| STU-10 | Take quizzes | M | `QuizzesController.Take`, `QuizService`, `QuizGrader` | `QuizAndCourseManagementTests.Submitting_answers_stores_a_graded_attempt_with_every_answer` | PASS |
| STU-11 | View quiz results and history | M | `QuizzesController.Result` / `History` | `QuizAndCourseManagementTests.Students_cannot_open_other_students_results_but_admins_can`; result screenshot | PASS |
| STU-12 | Logout | M | POST `AccountController.Logout` | `AuthenticationTests.Logout_ends_the_session` | PASS |
| STU-13 | Change password | O | `ProfileController.ChangePassword` (rate limited, refreshes sign-in) | Accessibility scan of the page; code review | PASS |

## Administrator

| ID | Requirement | Type | Implementation | Evidence | Status |
|----|-------------|------|----------------|----------|--------|
| ADM-01 | Separate, protected Admin area | M | `Areas/Admin`, `AdminControllerBase` with `[Authorize(Roles = "Admin")]` | `AuthorizationTests` | PASS |
| ADM-02 | Admin dashboard with real platform statistics | M | `Areas/Admin/Controllers/DashboardController.cs`, `DashboardService` | Admin browser test; dashboard screenshot | PASS |
| ADM-03 | Courses CRUD (create, list, details, edit, delete) | M | Admin `CoursesController` (index, create, details, edit, publish, delete) | `AdminManagementTests.Course_create_with_cover_edit_publish_and_delete` | PASS |
| ADM-04 | Categories CRUD | M | Admin `CategoriesController` | `AdminManagementTests.Category_create_edit_and_delete`; browser test | PASS |
| ADM-05 | Learning resources CRUD (incl. secure file upload) | M | Admin `ResourcesController`, `ResourceManagementService`, `FileStorageService` | `AdminManagementTests.Resources_are_created_validated_streamed_and_deleted` | PASS |
| ADM-06 | Enrolments management (list, enrol a student, remove) | M | Admin `EnrollmentsController` | `AdminManagementTests.Enrolments_can_be_managed_by_administrators` | PASS |
| ADM-07 | Quizzes CRUD | M | Admin `QuizzesController` (CRUD, publish) | `AdminManagementTests.Quiz_must_have_questions_before_it_can_be_published` | PASS |
| ADM-08 | Quiz questions and answer options CRUD, set correct answer | M | Admin `QuestionsController`, 2–6 options with exactly one correct | `LogicTests.Question_needs_two_answers_one_marked_correct_and_no_duplicates` | PASS |
| ADM-09 | Quiz results (attempts) view and delete | M | Admin `QuizAttemptsController` (index, details, delete) | Link crawl as admin; accessibility scan | PASS |
| ADM-10 | Users: list, view details, change role, deactivate/reactivate, safe delete | M | Admin `UsersController`, `UserManagementService` (roles, deactivate, reactivate, safe delete) | `AdminManagementTests.User_roles_deactivation_and_self_protection` | PASS |
| ADM-11 | Website content: contact message inbox | O | Admin `MessagesController` inbox | `AdminManagementTests.Contact_messages_reach_the_admin_inbox` | PASS |

## Database

| ID | Requirement | Type | Implementation | Evidence | Status |
|----|-------------|------|----------------|----------|--------|
| DB-01 | Relational model with primary keys and foreign keys | M | Keys and relationships in `Data/Configurations/` | `Documentation/ERD.md`; SQL Server migration applied in CI | PASS |
| DB-02 | Required properties and maximum lengths | M | `IsRequired` and `HasMaxLength` from `Models/FieldLengths.cs` | ERD.md; migrations | PASS |
| DB-03 | Unique constraints where appropriate | M | Unique indexes: category name, enrolment, completion, quiz answer | `DatabaseModelTests`; `CatalogueServiceTests` | PASS |
| DB-04 | Indexes where useful | M | Indexes on search, filter, ordering and foreign-key columns | ERD.md index table (checked against the SQL Server migration) | PASS |
| DB-05 | Intentional cascade / restrict delete behaviour | M | Restrict for Category→Course and QuizAnswer links; cascade elsewhere | `DatabaseModelTests.Delete_behaviour_is_intentional` | PASS |
| DB-06 | CreatedAt / UpdatedAt timestamps where useful | M | `IHasTimestamps` set in `SaveChanges`; UTC converter | ERD.md | PASS |
| DB-07 | EF Core migrations reproducible from an empty database | M | `Data/Migrations/Sqlite`, `Data/Migrations/SqlServer`; applied at start-up | CI: `dotnet ef database update` on an empty SQL Server; tests migrate fresh databases | PASS |
| DB-08 | Realistic seed data (6 categories, 8–10 courses, resources, quizzes, demo activity) | M | `Data/Seed/`: 6 categories, 10 courses, 50 resources, 9 quizzes, learners and activity | Screenshots; seed log checked in CI | PASS |
| DB-09 | Insert, Display, Update, Delete operations demonstrated | M | Insert, display, update and delete through student and admin features | `AdminManagementTests`; `StudentJourneyTests` | PASS |
| DB-10 | ERD that exactly matches the EF Core entities | M | `Documentation/ERD.md` generated from the entities and configurations | Index table verified against the SQL Server migration | PASS |

## Authentication and authorisation

| ID | Requirement | Type | Implementation | Evidence | Status |
|----|-------------|------|----------------|----------|--------|
| AUTH-01 | Register, Login, Logout | M | Register, login, logout in `AccountController` | `AuthenticationTests` | PASS |
| AUTH-02 | Password hashing through Identity (no plain-text passwords) | M | Identity password hashing | `AuthenticationTests.Registration_creates_a_student_signs_them_in_and_hashes_the_password` | PASS |
| AUTH-03 | Roles: Guest, Student, Admin with role-based authorisation | M | Anonymous guests; `Student` and `Admin` roles with role attributes | `AuthorizationTests` | PASS |
| AUTH-04 | Protected pages for students and admin-only controllers | M | `[Authorize(Roles = ...)]` on student controllers and `AdminControllerBase` | `AuthorizationTests`; link crawl per role | PASS |
| AUTH-05 | Initial admin seeded from secrets / environment variables (no password in Git) | M | `DatabaseInitializer` creates the admin from `Seed:AdminEmail` / `Seed:AdminPassword`; empty in `appsettings.json` | CI smoke log ("Administrator account … created"); Security-Review.md | PASS |
| AUTH-06 | Account lockout after repeated failed logins | O | Lockout after 5 failures for 15 minutes | `AuthenticationTests.Account_is_locked_after_five_failed_attempts` | PASS |

## Validation

| ID | Requirement | Type | Implementation | Evidence | Status |
|----|-------------|------|----------------|----------|--------|
| VAL-01 | Server-side validation with DataAnnotations and ModelState | M | DataAnnotations on view models; `ModelState` checks in controllers | `AuthenticationTests.Invalid_registration_is_rejected_by_server_side_validation`; `AdminManagementTests.Category_validation_errors_are_shown` | PASS |
| VAL-02 | Client-side validation with ASP.NET validation helpers | M | jQuery Validation Unobtrusive plus custom adapters (`wwwroot/js/validation.js`) | Browser tests (contact and registration validation) | PASS |
| VAL-03 | Bootstrap-styled, accessible validation feedback | M | Styled messages with icons, `aria-describedby` hints | Screenshots; axe-core scans | PASS |
| VAL-04 | Required, email, password, length and range rules | M | Required, email, password, length, range and compare rules | `LogicTests.Register_requires_matching_strong_passwords_and_accepted_terms` | PASS |
| VAL-05 | File validation (type, size, content signature) | M | `MaxFileSize`/`AllowedExtensions` attributes; signature and size checks in `FileStorageService` | `FileStorageServiceTests` | PASS |
| VAL-06 | Business-rule validation (e.g. exactly one correct answer) | M | Service rules: one correct answer, question before publishing, unique names, last admin | `LogicTests`; `AdminManagementTests`; `CatalogueServiceTests` | PASS |

## Security

| ID | Requirement | Type | Implementation | Evidence | Status |
|----|-------------|------|----------------|----------|--------|
| SEC-01 | No SQL injection (EF Core parameterised queries only) | M | EF Core parameterised queries; escaped LIKE patterns | `ContentSafetyTests` (search pattern); Security-Review.md | PASS |
| SEC-02 | XSS protection (Razor encoding, no raw user HTML, CSP) | M | Razor encoding, `LessonContentRenderer`, strict CSP | `ContentSafetyTests.Html_in_lesson_text_is_encoded_and_never_executed` | PASS |
| SEC-03 | CSRF protection (anti-forgery tokens on all unsafe requests) | M | Global `AutoValidateAntiforgeryTokenAttribute`; POST forms | `AuthenticationTests.Posts_without_an_anti_forgery_token_are_rejected` | PASS |
| SEC-04 | IDOR protection (ownership checks on attempts, resources, files) | M | Ownership and enrolment checks in services; protected file streaming | `QuizAndCourseManagementTests.Students_cannot_open_other_students_results_but_admins_can`; `PublicSiteTests.Private_files_cannot_be_downloaded_by_guests` | PASS |
| SEC-05 | Overposting protection (ViewModels, never bind entities) | M | View models only | `StudentJourneyTests.Tampered_quiz_answers_are_graded_on_the_server`; code review | PASS |
| SEC-06 | Safe file uploads (allow-list, magic bytes, size limit, random names, outside wwwroot) | M | Allow-list, signatures, size limits, random names, storage outside `wwwroot` | `FileStorageServiceTests` | PASS |
| SEC-07 | Safe video embedding (strict YouTube/Vimeo parsing, frame-src allow-list) | M | `VideoEmbedParser` and CSP `frame-src` | `ContentSafetyTests` (video parser) | PASS |
| SEC-08 | No secrets committed to Git | M | User secrets, App Service settings, managed identity, OIDC; no secrets in Git | Repository scan (Security-Review.md §3) | PASS |
| SEC-09 | Admin routes protected by role authorisation | M | `AdminControllerBase` | `AuthorizationTests.Students_cannot_post_to_admin_actions_even_with_a_valid_token` | PASS |
| SEC-10 | Security headers, HTTPS/HSTS in production | O | `SecurityHeadersMiddleware`, HTTPS redirection, HSTS | `PublicSiteTests.Html_responses_carry_security_headers`; smoke test | PASS |
| SEC-11 | Rate limiting on login, registration and contact form | O | Fixed-window limiter on login, registration, password change, contact | Configuration review; 429 behaviour documented | PASS |

## Error handling and logging

| ID | Requirement | Type | Implementation | Evidence | Status |
|----|-------------|------|----------------|----------|--------|
| ERR-01 | Custom 404 page | M | `ErrorController`, `Views/Error/Status.cshtml` | `PublicSiteTests.Unknown_pages_and_invalid_ids_return_a_friendly_404`; browser test | PASS |
| ERR-02 | Custom 500 / error page without stack traces in production | M | Exception handler with friendly page outside Development | Smoke test in Production mode; code review of `Program.cs` | PASS |
| ERR-03 | User-friendly validation and business error messages | M | `OperationResult` messages shown through TempData and `ModelState` | `AdminManagementTests`; `CatalogueServiceTests` | PASS |
| ERR-04 | Database error handling (e.g. unique-constraint races) | M | `DbUpdateException` from unique indexes handled (`EnrollmentService`, `CategoryService`) | `CatalogueServiceTests`; code review | PASS |
| ERR-05 | Structured logging of important events | M | Structured `ILogger` events (sign-in, lockout, seeding, enrolment, quiz, deletion, uploads) | CI checks logs for errors; seed log lines | PASS |

## UI/UX, accessibility and responsiveness

| ID | Requirement | Type | Implementation | Evidence | Status |
|----|-------------|------|----------------|----------|--------|
| UI-01 | Coherent custom design system (not default Bootstrap) | M | Interchange design system in `site.css` | Screenshots; presentation site | PASS |
| UI-02 | Navigation bar per role, hero, course cards, dashboards, tables, forms, footer | M | Role navigation, hero, cards, dashboards, tables, forms, footer | Screenshots | PASS |
| UI-03 | Empty states and success/error messages | M | Empty states and status messages (`_StatusMessage`) | Screenshots; admin tests | PASS |
| UI-04 | Breadcrumbs where useful | M | Breadcrumbs on nested pages | Navigation.md §3 | PASS |
| UI-05 | Responsive on mobile, tablet and desktop; collapsing navigation; usable tables | M | Collapsing navbar, off-canvas admin menu, stacked tables | 42 browser checks at three sizes (no horizontal overflow) | PASS |
| UI-06 | Semantic HTML, labels, alt text, keyboard access, contrast, heading structure | M | Landmarks, labels, alt text, focus styles, reduced motion | axe-core WCAG 2.2 A/AA: no violations | PASS |
| UI-07 | No placeholder/template text, no broken images or dead links | M | Real seed content; no broken images or links in the app | Link crawl tests; browser image checks. Team names on the Pages site are intentional placeholders | PASS |

## Testing and quality

| ID | Requirement | Type | Implementation | Evidence | Status |
|----|-------------|------|----------------|----------|--------|
| TEST-01 | Automated unit tests for services and helpers | M | `tests/LearnHub.Tests/Unit`, `tests/LearnHub.Tests/Services` | 182/182 passing | PASS |
| TEST-02 | Automated integration tests (auth, protected routes, CRUD, enrolment, quiz, search, validation, 404) | M | `tests/LearnHub.Tests/Integration` (auth, protected routes, CRUD, enrolment, quiz, search, validation, 404, crawl) | 182/182 passing on SQLite and SQL Server | PASS |
| TEST-03 | Browser end-to-end tests across mobile, tablet and desktop viewports | O | `tests/e2e` Playwright specs including accessibility | 42/42 passing | PASS |
| TEST-04 | Manual testing checklist with recorded results | M | `Documentation/Testing.md` §7 with recorded results | 13 items passed; keyboard-only, screen reader, real devices and live production checks are left for the team | PASS |
| TEST-05 | Build without errors and without warnings | M | CI builds with `-p:TreatWarningsAsErrors=true` | CI build: 0 warnings, 0 errors | PASS |

## Repository, CI/CD and deployment

| ID | Requirement | Type | Implementation | Evidence | Status |
|----|-------------|------|----------------|----------|--------|
| GIT-01 | Git repository with a professional structure and `.gitignore` | M | Repository layout and `.gitignore` | GitHub repository | PASS |
| GIT-02 | Sensible commit history and documented branching strategy | M | Conventional Commits history; branching strategy in `Documentation/Git-Workflow.md` | `git log`; Git-Workflow.md | PASS |
| GIT-03 | README with all required sections | M | `README.md` with all required sections | README | PASS |
| CI-01 | GitHub Actions CI: restore, build, test, fail on error | M | `.github/workflows/ci.yml` | Latest CI run green | PASS |
| CI-02 | GitHub Actions deployment workflow to Azure App Service using repository secrets | M | `.github/workflows/deploy.yml` (OpenID Connect with repository variables instead of stored secrets) | Build and publish job green; deploy job waits for Azure variables | PASS |
| DEP-01 | Production ASP.NET application on Azure App Service | M | `infra/provision.sh`, `deploy.yml`, production settings | Published app smoke-tested in Production mode in CI | **BLOCKED** – Azure sign-in |
| DEP-02 | Azure SQL production database configured through environment variables | M | Azure SQL with managed identity configured by `infra/provision.sh` through app settings | Full suite and migrations verified on SQL Server 2022 in CI | **BLOCKED** – Azure sign-in |
| DEP-03 | Documented migration strategy and production verification | M | Start-up migrations, idempotent script artifact, verification checklist | `Documentation/Deployment.md` §5 and §7 | PASS |
| PAGES-01 | Separate static GitHub Pages presentation site with "Launch LearnHub" CTA | M | `docs/index.html` with "Launch LearnHub" buttons | https://jahongirmirzodv.github.io/LearnHub/ (live) | PASS |
| PAGES-02 | Pages deployment automated with `pages.yml` | M | `.github/workflows/pages.yml` | Pages deployment runs succeeded | PASS |

## Documentation

| ID | Requirement | Type | Implementation | Evidence | Status |
|----|-------------|------|----------------|----------|--------|
| DOC-01 | Proposal: title, objectives, mission, audience modelling, scope | M | `Documentation/Proposal.md` | File | PASS |
| DOC-02 | Final report content (full structure from the brief) | M | `Documentation/Final-Report-Content.md` | File | PASS |
| DOC-03 | ERD (Mermaid) | M | `Documentation/ERD.md` | File | PASS |
| DOC-04 | Use cases | M | `Documentation/Use-Cases.md` | File | PASS |
| DOC-05 | Flowcharts (registration, login, enrolment, quiz, admin course CRUD) | M | `Documentation/Flowcharts.md` (registration, login, enrolment, quiz, admin course CRUD and more) | File | PASS |
| DOC-06 | Wireframes | M | `Documentation/Wireframes.md` | File | PASS |
| DOC-07 | Navigation structure diagram | M | `Documentation/Navigation.md` | File | PASS |
| DOC-08 | Testing documentation | M | `Documentation/Testing.md` | File | PASS |
| DOC-09 | Deployment documentation | M | `Documentation/Deployment.md` | File | PASS |
| DOC-10 | Team responsibilities and Git workflow | M | `Documentation/Team-Responsibilities.md`, `Documentation/Git-Workflow.md` | Files | PASS |
| DOC-11 | Viva preparation questions | M | `Documentation/Viva-Questions.md` | File | PASS |
