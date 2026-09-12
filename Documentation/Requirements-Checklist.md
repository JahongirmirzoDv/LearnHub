# LearnHub – Requirements Checklist

Module: **CT050-3-2-WAPP – Web Applications** (Group Assignment)

This checklist is the traceability baseline for the project. Every requirement has a
stable ID. The final audit (`Requirements-Audit.md`) maps each ID to its implementation,
evidence and status.

Legend: **M** = mandatory (assignment brief), **O** = optional improvement.

## 1. Technology and platform

| ID | Requirement | Type |
|----|-------------|------|
| TECH-01 | Built with .NET technologies – ASP.NET Core MVC on the latest stable LTS (.NET 10) | M |
| TECH-02 | C# for all server-side code | M |
| TECH-03 | Database connectivity through Entity Framework Core | M |
| TECH-04 | SQLite for local development, Azure SQL / SQL Server for production | M |
| TECH-05 | ASP.NET Core Identity for authentication | M |
| TECH-06 | Razor views, HTML5 semantic markup | M |
| TECH-07 | CSS3 with Bootstrap 5 customised by a project design system | M |
| TECH-08 | JavaScript for client-side processing where appropriate | M |
| TECH-09 | Multimedia (images, embedded video, PDF documents, SVG illustrations) | M |
| TECH-10 | Server-side processing (controllers, services, EF Core queries) | M |
| TECH-11 | Proper project and file organisation | M |

## 2. Guest (anonymous visitor)

| ID | Requirement | Type |
|----|-------------|------|
| GUEST-01 | View Home page | M |
| GUEST-02 | View About page | M |
| GUEST-03 | Browse published courses | M |
| GUEST-04 | Search courses (server-side) | M |
| GUEST-05 | Filter courses by category and difficulty | M |
| GUEST-06 | Open course details | M |
| GUEST-07 | View selected public (preview) learning resources | M |
| GUEST-08 | Register | M |
| GUEST-09 | Login | M |
| GUEST-10 | View Contact page and send a contact message | M |

## 3. Registered user (Student)

| ID | Requirement | Type |
|----|-------------|------|
| STU-01 | Student dashboard with meaningful, data-driven information | M |
| STU-02 | Dashboard: enrolled courses, completed courses, available courses | M |
| STU-03 | Dashboard: recent activity, quiz results, learning progress | M |
| STU-04 | View and update profile | M |
| STU-05 | Browse courses and view course details | M |
| STU-06 | Enrol in a course (and leave a course) | M |
| STU-07 | View My Courses | M |
| STU-08 | Access learning resources of enrolled courses | M |
| STU-09 | Track learning progress (mark resources complete) | M |
| STU-10 | Take quizzes | M |
| STU-11 | View quiz results and history | M |
| STU-12 | Logout | M |
| STU-13 | Change password | O |

## 4. Administrator

| ID | Requirement | Type |
|----|-------------|------|
| ADM-01 | Separate, protected Admin area | M |
| ADM-02 | Admin dashboard with real platform statistics | M |
| ADM-03 | Courses CRUD (create, list, details, edit, delete) | M |
| ADM-04 | Categories CRUD | M |
| ADM-05 | Learning resources CRUD (incl. secure file upload) | M |
| ADM-06 | Enrolments management (list, enrol a student, remove) | M |
| ADM-07 | Quizzes CRUD | M |
| ADM-08 | Quiz questions and answer options CRUD, set correct answer | M |
| ADM-09 | Quiz results (attempts) view and delete | M |
| ADM-10 | Users: list, view details, change role, deactivate/reactivate, safe delete | M |
| ADM-11 | Website content: contact message inbox | O |

## 5. Database

| ID | Requirement | Type |
|----|-------------|------|
| DB-01 | Relational model with primary keys and foreign keys | M |
| DB-02 | Required properties and maximum lengths | M |
| DB-03 | Unique constraints where appropriate | M |
| DB-04 | Indexes where useful | M |
| DB-05 | Intentional cascade / restrict delete behaviour | M |
| DB-06 | CreatedAt / UpdatedAt timestamps where useful | M |
| DB-07 | EF Core migrations reproducible from an empty database | M |
| DB-08 | Realistic seed data (6 categories, 8–10 courses, resources, quizzes, demo activity) | M |
| DB-09 | Insert, Display, Update, Delete operations demonstrated | M |
| DB-10 | ERD that exactly matches the EF Core entities | M |

## 6. Authentication and authorisation

| ID | Requirement | Type |
|----|-------------|------|
| AUTH-01 | Register, Login, Logout | M |
| AUTH-02 | Password hashing through Identity (no plain-text passwords) | M |
| AUTH-03 | Roles: Guest, Student, Admin with role-based authorisation | M |
| AUTH-04 | Protected pages for students and admin-only controllers | M |
| AUTH-05 | Initial admin seeded from secrets / environment variables (no password in Git) | M |
| AUTH-06 | Account lockout after repeated failed logins | O |

## 7. Validation

| ID | Requirement | Type |
|----|-------------|------|
| VAL-01 | Server-side validation with DataAnnotations and ModelState | M |
| VAL-02 | Client-side validation with ASP.NET validation helpers | M |
| VAL-03 | Bootstrap-styled, accessible validation feedback | M |
| VAL-04 | Required, email, password, length and range rules | M |
| VAL-05 | File validation (type, size, content signature) | M |
| VAL-06 | Business-rule validation (e.g. exactly one correct answer) | M |

## 8. Security

| ID | Requirement | Type |
|----|-------------|------|
| SEC-01 | No SQL injection (EF Core parameterised queries only) | M |
| SEC-02 | XSS protection (Razor encoding, no raw user HTML, CSP) | M |
| SEC-03 | CSRF protection (anti-forgery tokens on all unsafe requests) | M |
| SEC-04 | IDOR protection (ownership checks on attempts, resources, files) | M |
| SEC-05 | Overposting protection (ViewModels, never bind entities) | M |
| SEC-06 | Safe file uploads (allow-list, magic bytes, size limit, random names, outside wwwroot) | M |
| SEC-07 | Safe video embedding (strict YouTube/Vimeo parsing, frame-src allow-list) | M |
| SEC-08 | No secrets committed to Git | M |
| SEC-09 | Admin routes protected by role authorisation | M |
| SEC-10 | Security headers, HTTPS/HSTS in production | O |
| SEC-11 | Rate limiting on login, registration and contact form | O |

## 9. Error handling and logging

| ID | Requirement | Type |
|----|-------------|------|
| ERR-01 | Custom 404 page | M |
| ERR-02 | Custom 500 / error page without stack traces in production | M |
| ERR-03 | User-friendly validation and business error messages | M |
| ERR-04 | Database error handling (e.g. unique-constraint races) | M |
| ERR-05 | Structured logging of important events | M |

## 10. UI / UX, accessibility and responsiveness

| ID | Requirement | Type |
|----|-------------|------|
| UI-01 | Coherent custom design system (not default Bootstrap) | M |
| UI-02 | Navigation bar per role, hero, course cards, dashboards, tables, forms, footer | M |
| UI-03 | Empty states and success/error messages | M |
| UI-04 | Breadcrumbs where useful | M |
| UI-05 | Responsive on mobile, tablet and desktop; collapsing navigation; usable tables | M |
| UI-06 | Semantic HTML, labels, alt text, keyboard access, contrast, heading structure | M |
| UI-07 | No placeholder/template text, no broken images or dead links | M |

## 11. Testing and quality

| ID | Requirement | Type |
|----|-------------|------|
| TEST-01 | Automated unit tests for services and helpers | M |
| TEST-02 | Automated integration tests (auth, protected routes, CRUD, enrolment, quiz, search, validation, 404) | M |
| TEST-03 | Browser end-to-end tests across mobile, tablet and desktop viewports | O |
| TEST-04 | Manual testing checklist with recorded results | M |
| TEST-05 | Build without errors and without warnings | M |

## 12. Repository, CI/CD and deployment

| ID | Requirement | Type |
|----|-------------|------|
| GIT-01 | Git repository with a professional structure and `.gitignore` | M |
| GIT-02 | Sensible commit history and documented branching strategy | M |
| GIT-03 | README with all required sections | M |
| CI-01 | GitHub Actions CI: restore, build, test, fail on error | M |
| CI-02 | GitHub Actions deployment workflow to Azure App Service using repository secrets | M |
| DEP-01 | Production ASP.NET application on Azure App Service | M |
| DEP-02 | Azure SQL production database configured through environment variables | M |
| DEP-03 | Documented migration strategy and production verification | M |
| PAGES-01 | Separate static GitHub Pages presentation site with "Launch LearnHub" CTA | M |
| PAGES-02 | Pages deployment automated with `pages.yml` | M |

## 13. Documentation (university deliverables)

| ID | Requirement | Type |
|----|-------------|------|
| DOC-01 | Proposal: title, objectives, mission, audience modelling, scope | M |
| DOC-02 | Final report content (full structure from the brief) | M |
| DOC-03 | ERD (Mermaid) | M |
| DOC-04 | Use cases | M |
| DOC-05 | Flowcharts (registration, login, enrolment, quiz, admin course CRUD) | M |
| DOC-06 | Wireframes | M |
| DOC-07 | Navigation structure diagram | M |
| DOC-08 | Testing documentation | M |
| DOC-09 | Deployment documentation | M |
| DOC-10 | Team responsibilities and Git workflow | M |
| DOC-11 | Viva preparation questions | M |

## 14. Explicitly out of scope

To prevent scope creep, the following are **not** part of LearnHub v1.0:
AI tutor, live video classrooms, real-time chat, payment processing, certificates,
e-mail delivery (confirmation / password reset e-mails), instructor self-service role,
discussion forums, mobile apps, multi-language UI.
