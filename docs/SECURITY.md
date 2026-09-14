# LearnHub – Security Review

| | |
|---|---|
| Scope | The LearnHub application (`src/LearnHub`), its configuration, the container (`Dockerfile`, `railway.json`), the CI workflows in `.github/workflows` and the repository contents |
| Method | Manual code review against the OWASP Top 10:2025 and the assignment's security requirements (SEC-01 to SEC-11), backed by automated tests that attack the running application |
| Date | 14 September 2026 |
| Result | No open high or medium findings. Residual risks and hardening ideas are listed in section 4. |

## 1. Summary of controls

| Area | Control | Where |
|------|---------|-------|
| Authentication | ASP.NET Core Identity with PBKDF2 password hashing; 8–100 characters with upper-case, lower-case and digit; unique email; lockout after 5 failures for 15 minutes; generic error for wrong passwords; one message for lockout and deactivation | `Infrastructure/ServiceCollectionExtensions.cs`, `Controllers/AccountController.cs`, `ViewModels/Account/AccountViewModels.cs` |
| Sessions | Cookies `HttpOnly`, `SameSite=Lax`, `Secure` outside Development; 8-hour sliding expiry; security stamp re-checked every 5 minutes so role changes and deactivation apply quickly | `Infrastructure/ServiceCollectionExtensions.cs` |
| Authorisation | `[Authorize(Roles = "Student")]` on student controllers; every admin controller inherits `AdminControllerBase` (`[Area("Admin")]`, `[Authorize(Roles = "Admin")]`), so a new admin page cannot be added unprotected | `Controllers/`, `Areas/Admin/Controllers/AdminControllerBase.cs` |
| Access to data | Services check enrolment or ownership before returning lessons, files, quizzes and results (IDOR) | `Services/LearningResourceService.cs`, `Services/QuizService.cs`, `Controllers/ResourcesController.cs` |
| CSRF | Global `AutoValidateAntiforgeryTokenAttribute`; every state change is a POST form with a token | `Infrastructure/ServiceCollectionExtensions.cs` |
| Injection | Only EF Core LINQ (parameterised SQL); search terms go through `EF.Functions.Like` with `%`, `_` and `[` escaped | `Services/SearchPattern.cs`, `Services/CourseCatalogService.cs` |
| XSS | Razor encodes all output; lesson text is encoded first, then a fixed allow-list of formatting is applied; strict Content Security Policy without inline scripts or styles | `Services/Content/LessonContentRenderer.cs`, `Infrastructure/SecurityHeadersMiddleware.cs` |
| Embedded video | URLs parsed with strict host and id rules and rebuilt as `youtube-nocookie.com` or `player.vimeo.com` embed URLs; CSP `frame-src` allows only those players | `Services/Content/VideoEmbedParser.cs` |
| Overposting | Forms bind to view models; services copy allowed fields onto entities | `ViewModels/`, `Services/*ManagementService.cs` |
| File uploads | Extension allow-list (JPG, PNG, WebP up to 2 MB; PDF up to 10 MB), magic-byte signature check, size checked on the stream, random file names, storage outside `wwwroot`, files streamed only after an access check, SVG and HTML never accepted | `Services/Storage/UploadRules.cs`, `Services/Storage/FileStorageService.cs`, `Infrastructure/FileValidationAttributes.cs` |
| Abuse | Fixed-window rate limit (10 requests per 60 seconds per IP) on login, registration, password change and contact; honeypot field on the contact form | `Infrastructure/ServiceCollectionExtensions.cs`, `Services/ContactService.cs` |
| Transport and headers | HTTPS redirection and HSTS in production; `Content-Security-Policy`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, `Referrer-Policy`, `Permissions-Policy`, `Cross-Origin-Opener-Policy`; compression only for static assets (BREACH) | `Program.cs`, `Infrastructure/SecurityHeadersMiddleware.cs` |
| Trusted proxy | `UsePlatformProxyHeaders()` honours the platform proxy's `X-Forwarded-Proto` and `X-Forwarded-For`, so `Request.IsHttps` and the client address are correct behind the TLS-terminating edge. The container must only be reachable through that proxy (the trade-off is stated below and in section 4) | `Infrastructure/ApplicationBuilderExtensions.cs`, `Program.cs` |
| Errors | Developer exception page only in Development; friendly 404 and 500 pages; no stack traces; structured logging | `Program.cs`, `Controllers/ErrorController.cs` |
| Secrets | No passwords or keys in Git; demo passwords generated into a git-ignored file in Development; `Seed__AdminPassword` and `Seed__DemoStudentPassword` from environment variables in production; `.env` git-ignored while `.env.example` holds placeholders only | `appsettings.json` (empty seed passwords), `Data/Seed/DevelopmentSeedPasswords.cs`, `.env.example`, `.gitignore` |

Content Security Policy sent with every HTML response:

```
default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self';
media-src 'self'; frame-src 'self' https://www.youtube-nocookie.com https://player.vimeo.com; object-src 'none';
base-uri 'self'; form-action 'self'; frame-ancestors 'self'
```

### Trusting the platform proxy's forwarded headers

Railway terminates TLS at its edge proxy and forwards plain HTTP, describing the original request in
`X-Forwarded-Proto` and `X-Forwarded-For`. ASP.NET Core does not read those headers on its own, and its default
allow-list trusts them only from loopback, which is not where the platform proxy lives. `UsePlatformProxyHeaders()`
therefore clears `KnownIPNetworks` and `KnownProxies`, so both headers are honoured **from any source**.

That is a deliberate trade-off, and the condition it depends on is: the application must only be reachable through
that proxy. On Railway it is — the container publishes one HTTP port to the platform edge and nothing else routes to
it, and the image is never given a directly reachable public address. The residual risk is that anything able to reach
the container directly could forge the headers. Two consequences matter:

- `X-Forwarded-Proto` decides `Request.IsHttps`, so a forged value influences HSTS, the HTTPS redirection decision
  and the `Secure`-only cookie policy.
- The forms rate limiter partitions on `context.Connection.RemoteIpAddress`, so a forged `X-Forwarded-For` would let a
  caller present itself as a new address, start a fresh 10-requests-per-minute bucket and weaken the limit on login,
  registration, password change and contact.

The risk is accepted for this single-service deployment because the proxy is the only ingress. Exposing the container
on a public address, or adding a second ingress, would require restoring a known-proxy allow-list (the platform's
egress range) instead of clearing it.

## 2. OWASP Top 10:2025

| Risk | How LearnHub addresses it | Evidence (automated tests) | Status |
|------|---------------------------|----------------------------|--------|
| A01 Broken Access Control | Role attributes, admin base class, ownership and enrolment checks in services, files streamed through a controller, local-only return URLs | `AuthorizationTests` (access matrix, `Students_cannot_post_to_admin_actions_even_with_a_valid_token`, `Access_denied_page_returns_403`); `QuizAndCourseManagementTests.Students_cannot_open_other_students_results_but_admins_can`; `Students_who_are_not_enrolled_cannot_take_or_submit_the_quiz`; `StudentJourneyTests.Leaving_a_course_removes_access_to_its_lessons`; `PublicSiteTests.Private_files_cannot_be_downloaded_by_guests`; `AuthenticationTests.Login_redirects_back_to_a_local_return_url_but_never_to_another_site` | Addressed |
| A02 Security Misconfiguration | Production defaults: HTTPS, HSTS, secure cookies, CSP and headers, no developer pages; the image runs as Production and binds one HTTP port behind the platform's TLS-terminating proxy | `PublicSiteTests.Html_responses_carry_security_headers`; smoke test header checks in the `container` job | Addressed |
| A03 Software Supply Chain Failures | Central package versions (`Directory.Packages.props`), pinned GitHub Actions major versions, vendored front-end libraries with recorded versions, lock file for browser tests | Build in CI from a clean runner on every push | Partly addressed (see 4) |
| A04 Cryptographic Failures | Identity password hashing; TLS enforced; ASP.NET Core Data Protection for cookies and tokens; no custom cryptography | `AuthenticationTests.Registration_creates_a_student_signs_them_in_and_hashes_the_password` | Addressed |
| A05 Injection | EF Core parameterised queries, escaped LIKE patterns, encoded output, CSP | `ContentSafetyTests` (`Html_in_lesson_text_is_encoded_and_never_executed`, `Attack_attempts_through_formatting_markers_stay_as_text`); search tests | Addressed |
| A06 Insecure Design | Server-side grading from stored answers, correct answers never sent to the browser, score snapshots, unique indexes against double submissions, last-administrator protection | `StudentJourneyTests.Tampered_quiz_answers_are_graded_on_the_server`; `QuizAndCourseManagementTests.The_quiz_page_never_contains_the_correct_answers`; `Option_ids_that_belong_to_another_question_are_not_modified`; `AdminManagementTests.User_roles_deactivation_and_self_protection` | Addressed |
| A07 Authentication Failures | Password policy on client and server, lockout, generic messages, rate limiting, security stamp validation, deactivation through lockout | `AuthenticationTests.Account_is_locked_after_five_failed_attempts`; `Wrong_password_shows_a_generic_error`; `LogicTests.Register_requires_matching_strong_passwords_and_accepted_terms` | Addressed |
| A08 Software or Data Integrity Failures | Antiforgery tokens on every POST; uploads verified by content signature; the image is built from this repository by Railway and the presentation site is deployed from the same repository, so no artefact is copied by hand | `AuthenticationTests.Posts_without_an_anti_forgery_token_are_rejected`; `FileStorageServiceTests.Rejects_a_file_whose_content_does_not_match_its_extension`; `Rejects_files_over_the_size_limit_even_if_the_declared_length_is_small` | Addressed |
| A09 Security Logging and Alerting Failures | Structured logs for sign-in failures, lockouts, seeding, administrator creation, enrolments, deletions and discarded spam; the container log, which the `container` job keeps as an artifact | Both the `container` and `e2e` jobs fail if the application logs a `fail:` or `crit:` line | Partly addressed (see 4) |
| A10 Mishandling of Exceptional Conditions | Global exception handler with friendly pages; `DbUpdateException` from unique indexes turned into clear messages; transactions with the execution strategy; database health check | `NavigationCrawlTests` (no 500 responses for guest, student or admin); `PublicSiteTests.Unknown_pages_and_invalid_ids_return_a_friendly_404`; `Health_endpoint_reports_the_database_as_healthy` | Addressed |

## 3. Assignment security requirements

### SEC-04 IDOR (insecure direct object references)

Every identifier in a URL is treated as untrusted:

- `/Quizzes/Result/{id}` returns the attempt only when it belongs to the signed-in student. Another student's id gives
  404 rather than 403, so attempt ids cannot be probed. Administrators review attempts in **Admin → Quiz results**.
- `/Resources/Details/{id}` and `/Resources/Open/{id}` allow preview lessons for everyone; other lessons require
  enrolment in that course (or the Admin role). Files are never served from `wwwroot`.
- `/Quizzes/Take/{id}` requires enrolment and a published quiz, and a submitted option is accepted only if it
  belongs to that question.
- Admin actions are available only to administrators, even with a valid antiforgery token.

### SEC-02 XSS

- Razor encodes every value by default; the code base does not render user input with `Html.Raw`. Lesson bodies go
  through `LessonContentRenderer`, which encodes the whole text before applying paragraph, list, heading, code and
  emphasis formatting from a fixed allow-list.
- The CSP blocks inline scripts, event-handler attributes and scripts from other origins, so even an unexpected
  injection could not run code.
- Category icons come from a fixed allow-list, and video URLs are rebuilt rather than echoed.

### SEC-03 CSRF

- `AutoValidateAntiforgeryTokenAttribute` is registered globally, so every POST, PUT and DELETE needs a valid token.
  The form tag helper adds the token automatically.
- State changes are never done with GET: enrol, leave, complete, submit, publish, delete and log out are all POST forms.
- `SameSite=Lax` cookies add a second layer.

### SEC-06 Uploads

The upload pipeline in `FileStorageService` rejects a file unless every check passes:

1. A file was chosen and its extension is on the allow-list for that use (course cover: image; resource: image or PDF).
2. The size is within the limit, measured while copying rather than trusting the declared length.
3. The first bytes match a JPEG, PNG, WebP or PDF signature, and the detected type fits the extension.
4. The file is saved under a new random name with the detected extension, in `Storage:RootPath` outside `wwwroot`.

Uploaded files are served by `ResourcesController.Open` after the access check, with the stored content type and
`X-Content-Type-Options: nosniff`. Course covers are the only uploads shown as public images.

### SEC-08 Secrets

- `appsettings.json` contains no passwords (`Seed:AdminPassword` and `Seed:DemoStudentPassword` are empty) and no
  connection string with credentials: SQLite is a file, so `ConnectionStrings:DefaultConnection` is just a path.
- **Development.** `DevelopmentSeedPasswords` generates strong random passwords for the demo accounts and writes them
  to `App_Data/demo-credentials.json`, which is git-ignored (`App_Data/` is excluded wholesale). They are generated
  once and reused on later starts, so signing in as a demo account keeps working without a committed password.
- **Production.** The administrator and demo-student passwords must come from the environment variables
  `Seed__AdminPassword` and `Seed__DemoStudentPassword` (Railway service variables, or `.env` locally). With no
  password set the account is still created but cannot sign in, and the application logs a warning rather than
  inventing one. A password can also be set with `dotnet user-secrets` for local use.
- `.env` is git-ignored; `.env.example` documents every variable with placeholders only, so the repository can be
  cloned and configured without ever holding a real credential.
- CI generates throw-away passwords for the container and browser-test accounts at run time and masks them in the
  logs. The only fixed passwords in the repository are synthetic ones in
  `tests/LearnHub.Tests/Infrastructure/LearnHubWebApplicationFactory.cs` for temporary in-memory test databases.
- A repository scan for assignments of password, secret, key, token or connection string values found nothing except
  those synthetic test values.

### SEC-05 Overposting

Controllers accept view models that contain only the fields a form may change. For example, a student profile update
cannot set roles, and a course form cannot set `CreatedAt`. Services load the entity and copy the allowed values.

## 4. Residual risks and recommendations

| Risk | Current state | Recommendation |
|------|---------------|----------------|
| Forwarded headers are trusted from any source | `KnownIPNetworks` and `KnownProxies` are cleared so the platform proxy's headers are honoured (see section 1). Safe only while the proxy is the sole ingress | If the container ever becomes directly reachable or gains a second ingress, restore a known-proxy allow-list; otherwise a forged `X-Forwarded-For` can influence the per-IP rate limiter and a forged `X-Forwarded-Proto` the secure-cookie and HSTS decisions |
| No email verification or password reset by email | Registration signs in immediately; forgotten passwords need an administrator | Add an email provider, `RequireConfirmedAccount` and the Identity password-reset flow |
| No multi-factor authentication | Password only, with lockout and rate limiting | Enable Identity's authenticator-app MFA, at least for administrators |
| Dependency updates | Versions are pinned, but updates are manual | Enable Dependabot for NuGet, npm and GitHub Actions, and add `dotnet list package --vulnerable` to CI |
| Monitoring and alerting | Logs are written but no alerts are configured | Add an uptime/error monitor for `/health` and a log drain with alerts on 5xx responses and lockouts |
| Rate limiting per instance | In-memory limiter; resets on restart and is per instance | Acceptable for the single-instance deployment; use a distributed limiter (Redis or the platform's edge rules) if the service is scaled out |
| SQLite on a single volume | One file on the Railway volume, reachable only from the container, so there is no database network surface — but no automatic point-in-time recovery either | Keep the volume, take a copy before a schema change, and move to a managed database if the deployment grows |
| Uploaded PDFs | Signature-checked but not malware-scanned | Scan uploads in the storage layer if they ever move to object storage |
| Third-party video | Privacy-enhanced YouTube and Vimeo embeds | Keep the frame allow-list minimal; provide text summaries for accessibility |
