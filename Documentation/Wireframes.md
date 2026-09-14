# LearnHub – Wireframes

Low-fidelity wireframes of the main pages, matching the implemented layouts. The finished pages can be seen in the
screenshots in [`../docs/assets/screens/`](../docs/assets/screens/) and on the
[presentation site](https://jahongirmirzodv.github.io/LearnHub/#screens).

## Design concept

**LearnHub is an interchange.** Every category is a coloured transit line, a course is a route, lessons and quizzes are
stops, and a student's progress is how far along the route they are. The idea shapes real components: the home page map
where category lines meet at the LearnHub hub, the "course route" on course pages, the lesson outline, the progress
bars with a signal-yellow marker, and the admin sidebar where the current section has a yellow platform edge.

| Token | Value | Use |
|-------|-------|-----|
| Ink | `#1b2438` | Text, primary buttons, admin sidebar |
| Paper | `#f5f7fa` | Page background of content areas |
| Surface | `#ffffff` | Cards, panels, forms |
| Rail | `#dde3eb` | Borders and dividers |
| Muted | `#566076` | Secondary text |
| Signal | `#ffc53d` | Main calls to action, current step, focus halo |
| Go / Stop | `#107a55` / `#c12f3c` | Success and error states |
| Line colours | `#4b5a73`, `#c9344a`, `#157f8e`, `#6247c2`, `#b0520c`, `#587414`, `#2865c9`, `#a8337d` | One per category |
| Typeface | Overpass (variable weight) | Headings and body |

Breakpoints follow Bootstrap: phones below 768 px, tablets 768–991 px, desktops from 992 px. The navigation collapses
and the admin sidebar becomes an off-canvas panel below 992 px; admin tables become stacked records below 768 px.

## 1. Home

Desktop

```text
+--------------------------------------------------------------------------------------------+
| [mark] LearnHub   Home  Courses  About  Contact                        Log in  [Register]   |
+--------------------------------------------------------------------------------------------+
|                                               |      Web Development        Programming    |
|  Build real computing                         |      3 courses                 2 courses    |
|  skills, one lesson at a time.                |      o=====o\                  /o=====o     |
|                                               |              \                /             |
|  Short introduction to the catalogue          |  Cloud       ( LearnHub hub )   Security    |
|                                               |  o======o====(              )====o======o   |
|  +----------------------------------------+   |              /                \             |
|  | (search) Search for SQL, security...[Go]|   |      o=====o/                  \o=====o    |
|  +----------------------------------------+   |      Databases                Networking    |
|  9 courses, 47 lessons and 9 quizzes ...      |                                             |
+--------------------------------------------------------------------------------------------+
|  Popular courses                                                    [Browse all courses]   |
|  +--------------------+  +--------------------+  +--------------------+                    |
|  | [cover: line+icon] |  | [cover]            |  | [cover]            |                    |
|  | Category           |  | Category           |  | Category           |                    |
|  | Course title       |  | Course title       |  | Course title       |                    |
|  | Short description  |  | Short description  |  | Short description  |                    |
|  | (Level) 10 h 6 les |  | (Level) 8 h 5 les  |  | (Level) 12 h 7 les |                    |
|  | Instructor  4 lrnr |  | Instructor  4 lrnr |  | Instructor  3 lrnr |                    |
|  +--------------------+  +--------------------+  +--------------------+                    |
|  (second row of three cards)                                                               |
+--------------------------------------------------------------------------------------------+
|  How learning works on LearnHub                                                             |
|  (1)---------------(2)---------------(3)---------------(4)                                  |
|  Find a course      Enrol for free    Work through      Check your understanding            |
+--------------------------------------------------------------------------------------------+
|  Ready to start your first course?                   [Create a free account] [Log in]      |
+--------------------------------------------------------------------------------------------+
|  Footer: brand and line colours | Explore | Account | Project                               |
+--------------------------------------------------------------------------------------------+
```

Phone

```text
+------------------------------+
| [mark] LearnHub        [===] |
+------------------------------+
| Build real computing         |
| skills, one lesson at a time.|
| Introduction                 |
| +--------------------------+ |
| | Search...          [Go]  | |
| +--------------------------+ |
| (o) Cloud Computing  1       |
| (o) Cybersecurity    1       |
| (o) Databases 1 (o) Networking|
+------------------------------+
| Popular courses              |
| [Browse all courses]         |
| +--------------------------+ |
| | [cover]                  | |
| | Course title ...         | |
| +--------------------------+ |
| (cards stacked)              |
+------------------------------+
```

- The map is decorative on small screens, so phones get category chips (each a link to the filtered catalogue).
- The popular courses grid shows complete rows: one, two or three columns.
- The four "how it works" steps are a real sequence, so they sit on one numbered line.

## 2. Course catalogue

```text
Desktop                                                        Phone
+--------------------------------------------------------+     +----------------------------+
| Courses                                                |     | Courses                    |
| Search by topic or instructor                          |     | Search by topic...         |
| [Search courses......................]  [Search]       |     | [Search.........][Search]  |
+------------------+-------------------------------------+     +----------------------------+
| Filter courses   | 9 courses found                     |     | Filter courses             |
| Category         | +--------+ +--------+ +--------+    |     | All categories        9    |
| | All         9  | | card   | | card   | | card   |    |     | | Cloud Computing     1    |
| | Cloud       1  | +--------+ +--------+ +--------+    |     | | Cybersecurity       1    |
| | Security    1  | +--------+ +--------+ +--------+    |     | Level [v]   Sort [v]       |
| | Databases   1  | | card   | | card   | | card   |    |     | [Apply]                    |
| | ...            | +--------+ +--------+ +--------+    |     +----------------------------+
| Level [v]        | (up to 9 per page)                  |     | 9 courses found            |
| Sort [v]         |                                     |     | +------------------------+ |
| [Apply]          |       [< Previous] 1 2 [Next >]     |     | | card                   | |
+------------------+-------------------------------------+     | +------------------------+ |
                                                               | (stacked)                  |
                                                               +----------------------------+
```

- Categories are coloured line links with course counts; the selected line is highlighted and marked with
  `aria-current`.
- Level and sort submit automatically when changed (the **Apply** button covers browsers without JavaScript).
- All filters are GET parameters, so results can be bookmarked and the back button works.
- An empty result shows "No courses match these filters" with **Show all courses**.

## 3. Course details

```text
+----------------------------------------------------------------------------------------+
| Home / Courses / Networking / Networking Fundamentals                                  |
| (line) Networking                                     +----------------------------+   |
| Networking Fundamentals                               | [large cover: line + icon] |   |
| Short description                                     |                            |   |
| (Beginner) 9 h  5 lessons  1 quiz  2 learners         +----------------------------+   |
| Taught by Ms. Priyanka Rao                                                             |
+----------------------------------------------------------------------------------------+
| About this course                                     +----------------------------+   |
| Description paragraphs                                | Start learning             |   |
|                                                       | [Create a free account]    |   |
| What you will learn                                   | [Log in to enrol]          |   |
|  (v) outcome     (v) outcome                          | or, when enrolled:         |   |
|  (v) outcome     (v) outcome                          | Your progress [=====>   ]  |   |
|                                                       | [Resume]  Leave this course|   |
| Course route                                          | Beginner level             |   |
|  (o)--- How data travels   Article 10 min [Free preview]| About 9 h                 |   |
|   |                                                   | Updated Jul 21, 2026       |   |
|  (lock) The OSI model       Image 5 min               +----------------------------+   |
|   |                                                                                    |
|  (lock) ...                                                                            |
|  (lock) Networking quiz     Quiz with 5 questions  Pass mark 60%                       |
+----------------------------------------------------------------------------------------+
| More in Networking: related course cards                                               |
+----------------------------------------------------------------------------------------+
```

- The route line uses the category colour; stops show done (tick), current (yellow) or locked states.
- From 992 px the enrolment panel is a sticky column beside the content; on phones and tablets it follows the course
  route, after the description.

## 4. Lesson player

```text
Desktop                                                        Phone
+--------------------------------------------------------+     +----------------------------+
| Courses / ASP.NET Core MVC / Models...   [====>  ] 75% |     | Courses / ... / Lesson     |
+------------------+-------------------------------------+     | [=====>     ] 75%          |
| Course route     | Article  15 min                     |     +----------------------------+
| (v) How MVC...   | Models, validation and ModelState   |     | Article 15 min             |
| (v) Pipeline     | Summary                             |     | Lesson title               |
| (*) Models...    |                                     |     | Content                    |
| ( ) EF Core      | Content: headings, paragraphs,      |     | [code block scrolls ->]    |
| ( ) Status codes | code blocks, notes, or video / PDF  |     | [Mark as complete]         |
| [Course overview]| / image / external link             |     | [< Previous] [Next >]      |
|                  | [Mark as complete]                  |     | Course route (below)       |
|                  | [< Previous lesson] [Next lesson >] |     +----------------------------+
+------------------+-------------------------------------+
```

- The outline is sticky on desktop and follows the content on phones.
- Wide code blocks scroll inside their own box (and can be focused with the keyboard) instead of widening the page.

## 5. Quiz and result

```text
Take quiz                                              Result
+-----------------------------------------------+      +-----------------------------------------------+
| Dashboard / Course / MVC architecture quiz    |      | Quiz results / Course / MVC architecture quiz |
| MVC architecture quiz                         |      | MVC architecture quiz   [Retake] [Back]       |
| 5 questions  Pass mark 70%  1 previous attempt|      +-----------------------------------------------+
+-----------------------------------------------+      | +-------------------------------------------+ |
| Choose one answer for each question...        |      | | 100%   Passed                             | |
| +-------------------------------------------+ |      | |        5 of 5 correct. Pass mark is 70%.  | |
| | Question 1                                | |      | +-------------------------------------------+ |
| | ( ) option   ( ) option                   | |      | Answer review                                 |
| | ( ) option   ( ) option                   | |      | +-------------------------------------------+ |
| +-------------------------------------------+ |      | | Question 1: correct                       | |
| (one fieldset per question)                   |      | | ( ) option (v) chosen and correct         | |
|                                               |      | | Why: explanation                          | |
| 3 of 5 answered          [Submit answers]     |      | +-------------------------------------------+ |
+-----------------------------------------------+      +-----------------------------------------------+
```

- Each question is a `fieldset` with a `legend`, so screen readers announce the question with its options.
- Submitting with unanswered questions asks for confirmation first; grading happens only on the server.
- Correct answers are green, a wrong choice is red with the correct answer shown, and every question explains why.

## 6. Student dashboard

```text
+----------------------------------------------------------------------------------------+
| Welcome back, Aisyah                                                  [Browse courses] |
| You have completed 63% of your enrolled courses. Keep going.                           |
+----------------------------------------------------------------------------------------+
| [3 Enrolled] [1 Completed] [12 Lessons completed] [2 Quizzes passed] [6 Can still join] |
+-----------------------------------------------------------+----------------------------+
| Continue learning                          All my courses | Recent activity            |
| +-------------------------------------------------------+ | (v) Completed "..."        |
| | [cover] Course title   [=====>        ] 14%  [Resume] | |     date                   |
| |         Next: lesson title                            | | (+) Enrolled in ...        |
| +-------------------------------------------------------+ | (*) Scored 80% in ...      |
| (up to 3 courses)                                         | (list continues)           |
|                                                           |                            |
| Recent quiz results                         All results   |                            |
| | Quiz            | Submitted   | Score | Review |       |                            |
+-----------------------------------------------------------+----------------------------+
| Recommended for you                                              [Browse all courses]  |
| +------------------+  +------------------+  +------------------+                       |
| | course card      |  | course card      |  | course card      |                       |
| +------------------+  +------------------+  +------------------+                       |
+----------------------------------------------------------------------------------------+
```

- On phones the figure strip becomes two columns and every section stacks: continue learning, results, activity,
  recommendations.
- Empty states (no enrolments, no attempts) explain the next step and link to the catalogue.

## 7. Login and registration

```text
+---------------------------------------------------------------------------------------+
| +-------------------------------------+    What your account includes                 |
| | Create your student account         |    (*) Enrol in any published course for free |
| | Already registered? Log in.         |     |                                         |
| | Full name       [.................] |    (o) Open every lesson, video and PDF       |
| |                        0 / 100      |     |                                         |
| | Email address   [.................] |    (o) Take quizzes and review answers        |
| | Password        [.................] |     |                                         |
| | [strength meter: ####------]        |    (o) Track your progress on a dashboard     |
| | Use at least 8 characters with ...  |                                               |
| | Confirm password [................] |                                               |
| | [ ] I will use LearnHub for learning|                                               |
| |     and I have read the privacy notice                                              |
| | [        Create account        ]    |                                               |
| +-------------------------------------+                                               |
+---------------------------------------------------------------------------------------+
```

- The login page uses the same card with email, password, "remember me" and a link to registration.
- Validation messages appear under each field with an icon; the password hint is hidden while the same rule is shown as
  the error.
- On phones the benefits list moves below the form.

## 8. Admin dashboard

```text
Desktop                                                                 Phone
+-------------+--------------------------------------------------+     +---------------------------+
| [mark] LH   | Dashboard                    [View site] (LA) Name |     | [=] Dashboard   [->] (LA) |
| Admin       +--------------------------------------------------+     +---------------------------+
|             | Dashboard                   [New quiz][New course]|     | Dashboard                 |
| > Dashboard | Live figures for the catalogue                   |     | [New quiz] [New course]   |
| Catalogue   | [9 Published] [7 Students] [21 Enrolments]       |     | [9 Published][7 Students] |
|   Courses   | [7 Attempts] [2 Unread messages]                 |     | [21 Enrol.  ][7 Attempts] |
|   Categories| +---------------------+ +----------------------+ |     | [2 Unread messages      ] |
|   Resources | | Most enrolled       | | Enrolments by        | |     | Most enrolled (bars)      |
| Assessment  | | courses (bars)      | | category (bars)      | |     | By category (bars)        |
|   Quizzes   | +---------------------+ +----------------------+ |     | Recent enrolments         |
|   Results   | Recent enrolments       Recent quiz attempts     |     | Recent attempts           |
| People      | | table |               | table |                |     +---------------------------+
|   Users     |                                                  |
|   Enrolments| Content warnings (if any)                        |
|   Messages 2|                                                  |
| Website     |                                                  |
|   Main site |                                                  |
|   Log out   |                                                  |
+-------------+--------------------------------------------------+
```

- The dark sidebar keeps the admin area visually separate from the public site.
- The figure strip shows five figures in a row on desktop and two per row on phones (an odd last figure spans the
  row).

## 9. Admin list page (for example Courses)

```text
Desktop                                                                  Phone (stacked records)
+----------------------------------------------------------------------+ +-------------------------+
| Courses                                              [+ New course] | | Courses   [+ New course] |
| 10 courses match the current filters.                               | | [Search.........]        |
| +------------------------------------------------------------------+ | | [Category v] [Status v]  |
| | [Search.........]  [Category v]  [Status v]  [Apply] Reset       | | | [Apply] Reset            |
| +------------------------------------------------------------------+ | | +---------------------+ |
| | Cover | Course         | Level | Status | Les | Quiz | Lrn | Upd | | | | Course title        | |
| | [img] | Title          | (Beg) | Draft  |  3  |  0   |  0  | ... | | | | Category            | |
| |       | Category       |       |        |     |      |     |     | | | | Level      Beginner | |
| |       |                |       |        |     |  [Edit] [Delete] | | | | Status     Draft    | |
| +------------------------------------------------------------------+ | | | Lessons    3        | |
|                                     Showing 1-10 of 10  [pages]     | | |       [Edit] [Delete]| |
+----------------------------------------------------------------------+ | +---------------------+ |
                                                                         +-------------------------+
```

- Status uses badges with text (not colour alone).
- Deleting always opens a confirmation page that explains the consequences.

## 10. Admin form page (for example New course)

```text
+----------------------------------------------------------------------------------------+
| Courses / New course                                                                   |
| New course                                                                             |
+----------------------------------------------------------+-----------------------------+
| Course details                                           | Cover image                 |
|  Title            [...................................]  |  [current image]            |
|  Short description [..................................]  |  [ ] Remove the image       |
|  Description      [textarea.........................]    |  Replace the image          |
|  Learning outcomes [textarea........................]    |  [Choose file] [preview]    |
| -------------------------------------------------------- |  JPG, PNG or WebP, 2 MB     |
| Organisation                                             | Publishing                  |
|  Category [v]   Difficulty [v]   Duration (minutes) [ ]  |  [on/off] Published         |
|  Instructor [..........................]                 |  Drafts are hidden          |
|                                                          |                             |
|                                   [Cancel] [Create course]|                             |
+----------------------------------------------------------+-----------------------------+
```

- From 1200 px the side panel stays visible next to the form; below that it follows the form sections.
- Character counters, image preview and type-specific resource fields are progressive enhancements; the server
  repeats every check.
