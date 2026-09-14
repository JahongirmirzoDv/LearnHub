# LearnHub – Team Responsibilities

Replace the placeholders with each member's name and TP number before submission.

| Member | Name | TP number | Main role |
|--------|------|-----------|-----------|
| Member 1 | *to be filled in* | TP000000 | Project lead: architecture, authentication, security review, deployment |
| Member 2 | *to be filled in* | TP000000 | Course catalogue, search, enrolment, student dashboard |
| Member 3 | *to be filled in* | TP000000 | Learning resources, uploads, quizzes, grading, progress tracking |
| Member 4 | *to be filled in* | TP000000 | Administration area, testing, documentation, presentation site |

## 1. Responsibility matrix

R = responsible (owns the work and its quality), S = supports (reviews, helps, tests).

| Area | Member 1 | Member 2 | Member 3 | Member 4 |
|------|:--------:|:--------:|:--------:|:--------:|
| Requirements, proposal and schedule | R | S | S | S |
| Architecture, solution structure, configuration | R | S | S | S |
| Database model, migrations, ERD | S | S | R | S |
| Authentication, roles, profile and account pages | R | S | – | S |
| Home, About, Contact and course catalogue | S | R | – | S |
| Course details and enrolment | S | R | S | – |
| Student dashboard and My Courses | – | R | S | S |
| Lessons, file storage and uploads | S | – | R | S |
| Quizzes, grading and results | – | S | R | S |
| Progress calculation | – | S | R | – |
| Admin area: courses, categories, resources | – | S | S | R |
| Admin area: quizzes, users, enrolments, messages, dashboard | S | – | S | R |
| Design system, responsive layout, accessibility | S | R | S | S |
| Seed catalogue, lesson content and media | S | S | R | S |
| Security review and security tests | R | S | S | S |
| Automated tests and CI | S | S | S | R |
| Deployment to Railway and Firebase Hosting | R | – | – | S |
| Report, documentation and viva preparation | S | S | S | R |

## 2. Owned files

Paths are relative to the repository root. Owners review every pull request that touches their files.

### Member 1 – architecture, authentication, security, deployment

- `src/LearnHub/Program.cs`, `src/LearnHub/appsettings.json`
- `src/LearnHub/Infrastructure/ServiceCollectionExtensions.cs`, `ApplicationBuilderExtensions.cs`,
  `SecurityHeadersMiddleware.cs`, `Options.cs`, `ClaimsPrincipalExtensions.cs`, `LearnHubClaimsPrincipalFactory.cs`
- `src/LearnHub/Controllers/AccountController.cs`, `ProfileController.cs`, `ErrorController.cs`
- `src/LearnHub/ViewModels/Account/`, `src/LearnHub/Views/Account/`, `src/LearnHub/Views/Profile/`
- `src/LearnHub/Data/ApplicationDbContext.cs`, `DatabaseServiceCollectionExtensions.cs`, `Data/Seed/DatabaseInitializer.cs`
- `.github/workflows/ci.yml`, `ef-migrations.yml`, `Dockerfile`, `railway.json`, `firebase.json`, `.firebaserc`, `scripts/`
- Documentation: `ARCHITECTURE.md`, `SECURITY.md`, `DEPLOYMENT.md`, `PROPOSAL.md`

### Member 2 – catalogue, enrolment, student dashboard

- `src/LearnHub/Controllers/HomeController.cs`, `CoursesController.cs`, `StudentController.cs`
- `src/LearnHub/Services/CourseCatalogService.cs`, `CategoryService.cs`, `EnrollmentService.cs`, `DashboardService.cs`,
  `ContactService.cs`, `CourseProjections.cs`, `SearchPattern.cs`
- `src/LearnHub/ViewModels/Public/`, `src/LearnHub/Views/Home/`, `src/LearnHub/Views/Courses/`, `src/LearnHub/Views/Student/`
- `src/LearnHub/Views/Shared/_Layout.cshtml`, `_CourseCard.cshtml`, `_Pagination.cshtml`
- `src/LearnHub/wwwroot/css/site.css` (design system), `src/LearnHub/Infrastructure/InterchangeMap.cs`
- Documentation: `USE_CASES.md`, `NAVIGATION.md`

### Member 3 – lessons, uploads, quizzes, progress

- `src/LearnHub/Models/` and `src/LearnHub/Data/Configurations/`, `src/LearnHub/Data/Migrations/`
- `src/LearnHub/Controllers/ResourcesController.cs`, `QuizzesController.cs`
- `src/LearnHub/Services/LearningResourceService.cs`, `QuizService.cs`, `QuizGrader.cs`, `ProgressService.cs`
- `src/LearnHub/Services/Storage/` (upload rules and file storage), `src/LearnHub/Services/Content/` (video parser,
  lesson renderer)
- `src/LearnHub/ViewModels/Learning/`, `src/LearnHub/Views/Resources/`, `src/LearnHub/Views/Quizzes/`
- `src/LearnHub/Data/Seed/DemoCatalog*.cs`, `DemoDataSeeder.cs`, `Data/Seed/Files/`, `wwwroot/images/courses/`
- Documentation: `ERD.md`, `FLOWCHARTS.md`

### Member 4 – administration, testing, documentation, presentation site

- `src/LearnHub/Areas/Admin/` (controllers and views), `src/LearnHub/wwwroot/css/admin.css`
- `src/LearnHub/Services/CourseManagementService.cs`, `ResourceManagementService.cs`, `QuizManagementService.cs`,
  `QuestionManagementService.cs`, `UserManagementService.cs`, `LookupService.cs`
- `src/LearnHub/ViewModels/Admin/`, `src/LearnHub/wwwroot/js/site.js`
- `tests/LearnHub.Tests/`, `tests/e2e/`
- `FirebaseLanding/` (the presentation site, deployed to Firebase Hosting) and `docs/` (the project documentation)
- Documentation: `TESTING.md`, `WIREFRAMES.md`, `REPORT_NOTES.md`, `VIVA.md`, `TEAM.md`, `GIT_WORKFLOW.md`,
  `REQUIREMENT_TRACEABILITY.md`, `README.md`

## 3. How the team works

| Practice | Agreement |
|----------|-----------|
| Planning | A short planning meeting each Monday; tasks are GitHub issues labelled by area and assigned to one owner |
| Daily updates | A message in the group chat: done yesterday, doing today, blocked by |
| Branches and pull requests | One short-lived branch per task; pull request with a description and screenshots for UI changes; at least one reviewer; merge only when CI is green (see [GIT_WORKFLOW.md](GIT_WORKFLOW.md)) |
| Definition of done | Code builds, tests added or updated, all CI jobs pass, no secrets committed, documentation updated, reviewed |
| Code review focus | Correct authorisation, validation on the server, no entity binding, clear names, accessible markup |
| Shared files | `site.css`, `Program.cs` and the EF Core model change only through small, reviewed pull requests to avoid conflicts |
| Viva | Every member can explain the whole architecture and at least one feature outside their own area |

## 4. Individual contribution statements

Each member completes a short statement for the report (about 100–150 words): what they built, the hardest problem
they solved, one thing they would improve, and links to representative commits or pull requests.

| Member | Statement |
|--------|-----------|
| Member 1 | *to be completed by the member* |
| Member 2 | *to be completed by the member* |
| Member 3 | *to be completed by the member* |
| Member 4 | *to be completed by the member* |

## 5. Peer assessment

| Criterion | Member 1 | Member 2 | Member 3 | Member 4 |
|-----------|:--------:|:--------:|:--------:|:--------:|
| Contribution to code (1–5) | | | | |
| Contribution to documentation (1–5) | | | | |
| Communication and reliability (1–5) | | | | |
| Agreed share of the group mark (%) | | | | |
