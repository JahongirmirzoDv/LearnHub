# LearnHub – Testing

## 1. Strategy

LearnHub is tested at several levels, from fast checks of single classes up to the published application running in a
real browser. Every level runs automatically in GitHub Actions on every push that touches the application.

| Level | What it proves | Tools | Where |
|-------|----------------|-------|-------|
| Unit | Grading, progress, validation rules, content safety, file signatures and names | xUnit v3 | `tests/LearnHub.Tests/Unit` |
| Data model | The EF Core model creates a valid SQLite schema, a create script that enforces the integrity rules, and only the delete rules the design intends | xUnit, EF Core | `tests/LearnHub.Tests/Data` |
| Service | Business rules against a real (in-memory SQLite) database | xUnit, EF Core | `tests/LearnHub.Tests/Services` |
| Integration | The whole application in memory: routing, authentication, authorisation, antiforgery, forms, views, database | `WebApplicationFactory` | `tests/LearnHub.Tests/Integration` |
| Link crawl | Every link reachable by a guest, a student and an administrator returns a valid page | `WebApplicationFactory` | `NavigationCrawlTests` |
| Container | The Docker image Railway builds, started the way Railway starts it — Production settings, `PORT=8080`, a volume at `/data`: migrations apply once, demo data is seeded, the administrator is created, the smoke test passes, and after a restart migrations are **not** re-applied, which proves the volume persists the SQLite file | Docker, `scripts/smoke-test.sh` | `ci.yml` job `container` |
| Browser | Guest, student and admin journeys on desktop, tablet and phone sizes; console errors, failed requests, broken images, sideways scrolling | Playwright | `tests/e2e/specs` |
| Accessibility | Automated WCAG 2.2 A and AA rules on public, student and admin pages | axe-core with Playwright | `tests/e2e/specs/accessibility.spec.js` |
| Smoke | A running instance (the container in CI, and the deployed application once Railway is deployed) serves pages, links, protected areas and security headers | `scripts/smoke-test.sh` | `ci.yml` job `container` |
| Manual | Things automation does not judge well: readability, keyboard flow, visual quality | Checklist in section 7 | This document |

`tests/LearnHub.Tests/Infrastructure` holds no tests of its own: it is the shared test host — the in-memory
application factory, the isolated SQLite database helper, the test environment and the browser client extensions.

Test data is isolated: each integration test class gets its own in-memory SQLite database with migrations and demo
data applied, so tests can run in parallel and never touch a developer's database. Test accounts use synthetic
passwords defined in the test factory; CI generates random passwords for the browser tests.

The three CI jobs are `build-and-test` (the whole .NET suite), `container` (the Docker image started the way Railway
starts it, plus the smoke test) and `e2e` (Playwright against a Production instance over HTTPS).

## 2. How to run the tests

See [README – Running the tests](../README.md#running-the-tests). In short:

```bash
dotnet test --solution LearnHub.sln
```

## 3. Latest results

Evidence: measured on 14 September 2026 on the working branch `feature/railway-firebase-sqlite` with
`dotnet test LearnHub.sln -c Release`. Every job uploads its evidence as an artifact — the TRX result files, the
Playwright report and screenshots, and the container log — so the figures below can be reproduced from the
[Actions page](https://github.com/JahongirmirzoDv/LearnHub/actions) or locally.

| Job | Result |
|-----|--------|
| `build-and-test` | Release build with warnings treated as errors: 0 warnings, 0 errors; **211 of 211 tests passed**, 0 failed, 0 skipped (116 methods; theories run once per data row) |
| `container` | The image built and started with Production settings, `PORT=8080` and a volume at `/data`; the health check passed; `scripts/smoke-test.sh` passed; the start-up log showed the migrations applied, the demo data seeded and the administrator account created, with no `fail:` or `crit:` lines; after a **restart** the log showed no migration applied again, so the SQLite file and the Data Protection key ring really live on the volume |
| `e2e` | **42 of 42 browser checks passed** (14 Playwright tests × 3 viewports: desktop 1440×900, tablet 768×1024, mobile Pixel 7), including 9 axe-core accessibility scans covering 26 page views per viewport, with no WCAG A or AA violations; no errors in the application log |

## 4. Automated test inventory

116 test methods produce 211 test cases, because theories run once per data row (`Unit` 36 methods, `Services` 23,
`Data` 3, `Integration` 54).

| Class | Methods | Covers |
|-------|---------|--------|
| `Unit/LogicTests` (grader, display format, progress, view model validation) | 16 | Scoring and the pass mark (rounding down, unanswered questions, option ids from other questions, questions weighted by their points), progress width classes and completion derived from counts, initials, human-readable durations and file sizes, category line colours, registration rules (strong matching passwords, accepted terms), question rules (two options, one correct, no duplicates), category icon allow-list |
| `Unit/ContentSafetyTests` (video parser, lesson renderer, search pattern) | 10 | YouTube and Vimeo URLs accepted only in known formats and rebuilt as privacy-enhanced embeds, HTML in lesson text is encoded, only allow-listed formatting is produced, code blocks stay encoded, attacks through formatting markers stay text, LIKE wildcards escaped, long search terms truncated |
| `Unit/FileStorageServiceTests` | 7 | Valid image saved with a random name and detected type, mismatched content rejected, oversize files rejected even with a small declared length, PDFs accepted for documents, paths outside the storage root never resolved, only uploaded thumbnails can be deleted |
| `Unit/DevelopmentSeedPasswordsTests` | 3 | Development generates strong demo passwords once and reuses them from the git-ignored credentials file; configured passwords are never replaced; other environments never generate passwords |
| `Data/DatabaseModelTests` | 3 | SQLite schema creation from the model, the create script enforcing the integrity rules, intentional delete behaviour of key relationships (Restrict and Cascade) |
| `Services/CatalogueServiceTests` | 12 | Duplicate category names (case-insensitive), category deletion rules, search and filters return published courses only (including matches in lesson titles and descriptions), unpublished details hidden except for admins, double enrolment and unpublished enrolment rejected, stored progress recalculated when content changes, draft lessons hidden and excluded from progress, leaving a course keeps completions |
| `Services/QuizAndCourseManagementTests` | 11 | Graded attempts store every answer, other students' results are hidden, non-enrolled students cannot take quizzes, quiz pages never contain correct answers, the start time comes from the signed token issued with the quiz page, question points are saved and used for later attempts, foreign option ids are not modified, removing a selected answer keeps attempts, course deletion order |
| `Integration/PublicSiteTests` | 19 | Public pages load, only published courses listed, server-side search over titles, descriptions and lesson titles, category filter and category page, paging beyond the first page, empty search state, course details, friendly 404s, drafts hidden, preview lessons open while others need login, private files protected, security headers, health endpoint, static assets and seeded covers, home map lines, isolated test database |
| `Integration/AuthenticationTests` | 9 | Registration (role, sign-in, password hashing), server-side validation, duplicate email, students and admins sent to their own dashboards after login, generic wrong-password error, local-only return URLs, logout, lockout after five failures, antiforgery rejection |
| `Integration/AuthorizationTests` | 8 | Access matrix for guest, student and admin across protected pages (47 cases over the page lists), 403 access denied page, students cannot post to admin actions even with a valid token |
| `Integration/StudentJourneyTests` | 5 | Register, enrol, study, take the quiz and see the result; tampered answers graded on the server; leaving removes lesson access; profile validation and saving; changing the email address needs the current password |
| `Integration/AdminManagementTests` | 10 | Category create, edit, delete and validation; category with courses protected; course create with cover, edit, publish, delete; resources created, validated, streamed and deleted; quiz publishing needs questions; user roles, deactivation and self-protection; enrolment management; contact inbox; spam honeypot |
| `Integration/NavigationCrawlTests` | 3 | Crawls every internal link as a guest, a student and an administrator: no 500 errors and no broken links |

### Browser scenarios (14 tests; each runs on desktop 1440×900, tablet 768×1024 and Pixel 7, so 42 checks)

| Spec | Scenario |
|------|----------|
| `public.spec.js` | Home page presents the catalogue (and the map draws one line per category) |
| `public.spec.js` | Navigation menu works on every screen size |
| `public.spec.js` | Server-side search and category filter |
| `public.spec.js` | Course details and a free preview lesson |
| `public.spec.js` | Contact form validates in the browser before submitting |
| `public.spec.js` | Registration form gives live password feedback |
| `public.spec.js` | Unknown pages show the friendly 404 page |
| `public.spec.js` | About page |
| `student.spec.js` | Dashboard, course progress, lesson and quiz |
| `admin.spec.js` | Dashboard and management pages |
| `admin.spec.js` | Category create and delete through the forms |
| `accessibility.spec.js` | Public pages: home, catalogue, course details, preview lesson, About, Contact, Privacy, login, register, 404 |
| `accessibility.spec.js` | Student pages: dashboard, My Courses, lesson, quiz history, profile, change password |
| `accessibility.spec.js` | Administration pages: dashboard, courses, new course, categories, resources, quizzes, quiz results, enrolments, users, messages |

Every browser scenario fails on console errors, failed requests to LearnHub, broken images or horizontal scrolling,
and saves full-page screenshots. The app runs from the published build in Production mode over HTTPS, so secure
cookies, CSP and error pages behave as in production.

## 5. Representative test cases

| ID | Test case | Steps | Expected | Actual | Result |
|----|-----------|-------|----------|--------|--------|
| TC-01 | Register with valid data | Fill in the form with a strong password, accept terms, submit | Account created with the Student role, signed in, dashboard shown, password stored as a hash | As expected | Pass |
| TC-02 | Register with a weak password | Enter "short" | Message "Use at least 8 characters with an upper-case letter, a lower-case letter and a number." | As expected; the hint is hidden while the same rule is shown as the error | Pass |
| TC-03 | Duplicate email | Register twice with the same email | One clear error, no second account | As expected | Pass |
| TC-04 | Lockout | Five wrong passwords | Account locked for 15 minutes with the lockout message | As expected | Pass |
| TC-05 | Open redirect | Log in with `returnUrl=https://example.org` | Redirect to the user's own dashboard | As expected | Pass |
| TC-06 | Search | Search "sql" | Published courses that match the title, the description or a lesson title are listed with the number found, and drafts never appear | As expected | Pass |
| TC-07 | Preview lesson | Guest opens a free preview, then a locked lesson | Preview shown; locked lesson redirects to login | As expected | Pass |
| TC-08 | Enrol twice | Post the enrol form twice | Second attempt shows "You are already enrolled in this course." | As expected | Pass |
| TC-09 | Quiz tampering | Submit an option id from another question | Recorded as unanswered and counted as incorrect | As expected | Pass |
| TC-10 | Result privacy | Student B opens student A's result URL | 404 | As expected | Pass |
| TC-11 | Admin access | Student opens `/Admin/Courses` and posts to an admin action | Access denied (403) and the POST is refused | As expected | Pass |
| TC-12 | Missing antiforgery token | POST to `/Account/Login` without the token | 400 Bad Request | As expected | Pass |
| TC-13 | Disguised upload | Upload an HTML file containing a script, renamed `cheat-sheet.pdf` | Rejected with "The file content does not match an allowed file type."; nothing stored | As expected | Pass |
| TC-14 | Category with courses | Delete a category that has courses | Deletion refused with an explanation | As expected | Pass |
| TC-15 | Publish empty quiz | Publish a quiz without questions | "Add at least one question before publishing this quiz." | As expected | Pass |
| TC-16 | Last administrator | The only admin tries to deactivate or demote themselves | Refused with a clear message | As expected | Pass |
| TC-17 | Course deletion | Delete a course with attempts | Answers, attempts, quizzes, lessons and enrolments removed; no database error | As expected; the deletion order is deliberate (see [ERD](ERD.md)) | Pass |
| TC-18 | Broken links | Crawl all pages as each role | No 404 or 500 responses from internal links | As expected | Pass |
| TC-19 | Responsive layout | Browser tests at 1440, 768 and Pixel 7 widths | No horizontal scrolling, menus usable | As expected (after the fixes in section 6) | Pass |
| TC-20 | Accessibility | axe-core WCAG 2.2 A/AA scan on 26 page views per viewport | No violations | As expected (after the fixes in section 6) | Pass |

## 6. Defects found and fixed

| # | Found by | Symptom | Cause | Fix | Commit |
|---|----------|---------|-------|-----|--------|
| 1 | CI build | xUnit v3 tests would not run on the .NET 10 SDK | VSTest mode no longer supported for this setup | Opted into Microsoft Testing Platform in `global.json` | `32625d6` |
| 2 | CI build | Razor compile errors in the pagination partial | A variable named `page` clashed with the reserved `@page` directive | Renamed the variables | `fc83d0a` |
| 3 | Link crawl test | `/Admin/Courses/Details/{id}` returned 500 on SQLite | The query used SQL `APPLY`, which SQLite cannot translate | Rewrote it as separate flat queries | `945aed8` |
| 4 | Admin CRUD test | Deleting a course failed with a "relationship severed" error | EF Core tried to delete tracked dependants with Restrict rules | Delete dependent quiz answers first with `ExecuteDelete` in a transaction | `945aed8` |
| 5 | CI smoke test | Form pages returned 500 in Production over plain HTTP | TLS is terminated by the platform proxy, so the application saw an insecure request and refused its own Secure-only antiforgery cookie | Added `UsePlatformProxyHeaders()` and made the smoke test send `X-Forwarded-Proto` | `3851e07` |
| 6 | Browser test (tablet) | Admin pages scrolled sideways by 119 px | Visually hidden table header text escaped the scroll container | Made `.table-wrap` the containing block | `acee776` |
| 7 | Screenshot review | Admin sidebar invisible on desktop | Bootstrap forces `.offcanvas-lg` to a transparent background | Restored the dark rail with a more specific rule | `acee776` |
| 8 | Screenshot review | Admin filters always stacked | Invalid CSS grid template (`auto-fit` combined with `auto`) | Flexible wrapping row | `acee776` |
| 9 | Browser test (phone) | Lesson pages scrolled sideways by 198 px | Grid column sized to the longest code line | `minmax(0, 1fr)` column | `3851e07` |
| 10 | Browser test (phone) | About page scrolled sideways | Bootstrap `g-5` gutters wider than the container padding | `g-4 g-lg-5` | `3851e07` |
| 11 | Screenshot review | Home map labels overlapped lines and the hub | Label positions did not account for long names | Two-line labels placed clear of the lines; long names shortened | `ab9ad5d` |
| 12 | Screenshot review | Home map invisible when animations are disabled | The resting style was hidden and only the animation fill showed it | Keyframes now animate from hidden to a visible resting style | `ab9ad5d` |
| 13 | Accessibility scan | Code blocks and the About table could not be scrolled with the keyboard | Scrollable regions without focusable content | Focusable regions with labels | `5de245b` |
| 14 | Accessibility scan | Two admin top bar links had no name on phones | Their text is hidden below the breakpoint | `aria-label` on both links | `5de245b` |
| 15 | Build log review | 91 build warnings (xUnit1051) in test code | Async calls did not pass the test cancellation token | Token passed everywhere; CI now treats warnings as errors | `273ccc6` |

## 7. Manual testing checklist

Recorded on 14 September 2026 against the container build of the `feature/railway-firebase-sqlite` working tree, using
the Playwright screenshots of every scenario on desktop, tablet and phone plus the automated reports. Items that need
a person with a live deployment are marked **To do**; the team completes them after the first Railway deploy.

| Area | Check | Result | Evidence or note |
|------|-------|--------|------------------|
| Build | Solution builds in Release with no errors and no warnings | Pass | CI build step with `-p:TreatWarningsAsErrors=true` |
| Pages | Every public, student and admin page loads without server errors | Pass | Link crawl tests as all three roles |
| Roles | Guest, student and admin see only their own navigation and pages | Pass | Authorisation tests; screenshots of each role |
| CRUD | Create, read, update and delete work for courses, categories, resources, quizzes, questions, enrolments, users and messages | Pass | Admin management integration tests; admin browser test |
| Validation | Client-side messages appear before submit; server repeats every rule | Pass | Contact and registration browser tests; server validation tests |
| Responsive | Layout correct at 1440, 768 and 412 px; no sideways scrolling | Pass | Browser tests and screenshot review (defects 6–10 fixed) |
| Console | No JavaScript errors or failed requests | Pass | Browser tests fail on any console error |
| Images | No broken images, covers and diagrams load | Pass | Browser tests check every image |
| Links | No dead internal links | Pass | Link crawl tests |
| Accessibility (automated) | No WCAG 2.2 A/AA violations found by axe-core | Pass | Accessibility spec in 3 viewports |
| Visual quality | Consistent design system, readable typography, balanced layouts | Pass | Screenshot review; issues 7, 8 and 11 fixed |
| Error pages | 404 and access denied pages are friendly and branded | Pass | Browser test and integration tests |
| Security headers | CSP and other headers present | Pass | Integration test and smoke test |
| Keyboard | Complete journeys (register, enrol, lesson, quiz, admin form) with keyboard only; focus always visible | To do | Focus styles exist for all interactive elements; needs a person to run through |
| Screen reader | Headings, landmarks, form labels and quiz questions announced correctly (NVDA or VoiceOver) | To do | Automated checks passed; needs a manual pass |
| Real devices | iPhone and Android phone in portrait and landscape | To do | Emulated Pixel 7 passed |
| Production | Health, HTTPS redirect, admin login, upload and student journey on Railway | To do | The `container` job already performs these checks against the image; run the verification table in [Deployment](DEPLOYMENT.md#7-verify-the-deployment) after the first Railway deploy |
