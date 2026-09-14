# LearnHub – Flowcharts

Each flowchart follows the real code path. File paths are relative to `src/LearnHub/`.

## 1. Request pipeline

Every request passes through the middleware configured in `Program.cs`. Railway terminates TLS at its edge proxy and
forwards plain HTTP with `X-Forwarded-Proto` and `X-Forwarded-For`, so `UsePlatformProxyHeaders()` runs first and
turns those headers into `Request.IsHttps` and the real client address.

```mermaid
flowchart TD
    request["HTTP request"] --> forwarded["Forwarded headers (UsePlatformProxyHeaders: the platform proxy's X-Forwarded-Proto and X-Forwarded-For)"]
    forwarded --> errors{"Production?"}
    errors -->|yes| handler["Exception handler to /Error and HSTS"]
    errors -->|no| devpage["Developer exception page"]
    handler --> status["Status code pages: re-execute /Error/{code}"]
    devpage --> status
    status --> headers["Security headers: CSP, nosniff, frame, referrer and permissions policies"]
    headers --> https["HTTPS redirection"]
    https --> compress["Response compression (static assets only)"]
    compress --> static{"Static file or public thumbnail?"}
    static -->|yes| file["Serve file with cache headers"]
    static -->|no| routing["Routing"]
    routing --> rate{"Endpoint has the Forms rate limit and the limit is used up?"}
    rate -->|yes| r429["429 Too Many Requests"]
    rate -->|no| authn["Authentication: read the LearnHub.Auth cookie, re-check the security stamp every 5 minutes"]
    authn --> authz{"Authorised for this endpoint?"}
    authz -->|"not signed in"| login["302 to /Account/Login?ReturnUrl=..."]
    authz -->|"wrong role"| denied["302 to /Account/AccessDenied (403 page)"]
    authz -->|yes| antiforgery{"POST with a valid antiforgery token?"}
    antiforgery -->|no| r400["400 Bad Request"]
    antiforgery -->|yes| action["Controller action, then service, then EF Core"]
```

## 2. Start-up: database initialisation

`Data/Seed/DatabaseInitializer.cs` runs once before the application starts listening.

```mermaid
flowchart TD
    start["Application start"] --> migrate{"Database:ApplyMigrationsOnStartup?"}
    migrate -->|yes| apply["Apply pending migrations (EF Core migration lock)"]
    migrate -->|no| roles
    apply --> roles["Create Admin and Student roles if missing"]
    roles --> adminExists{"Account for Seed:AdminEmail exists?"}
    adminExists -->|yes| demo
    adminExists -->|no| password{"Seed:AdminPassword set?"}
    password -->|no| warn["Log a warning: no administrator account"]
    password -->|yes| create["Create the administrator and add the Admin role"]
    warn --> demo
    create --> demo{"Seed:DemoData and no categories yet?"}
    demo -->|no| listen["Start accepting requests"]
    demo -->|yes| seed["Seed categories, courses, lessons, files, quizzes, learners and activity"]
    seed --> listen
```

## 3. Registration

`Controllers/AccountController.cs` (Register) and `ViewModels/Account/AccountViewModels.cs`.

```mermaid
flowchart TD
    open["Guest opens /Account/Register"] --> fill["Fill in name, email, password, confirmation and terms"]
    fill --> client{"Client validation passes?"}
    client -->|no| hints["Show messages beside the fields"]
    hints --> fill
    client -->|yes| post["POST with antiforgery token"]
    post --> limit{"Within the rate limit?"}
    limit -->|no| r429["429 Too Many Requests"]
    limit -->|yes| server{"ModelState valid? (same rules on the server)"}
    server -->|no| redisplay["Return the form with errors"]
    redisplay --> fill
    server -->|yes| createUser["Identity creates the user and hashes the password"]
    createUser --> ok{"Created? (unique email, password policy)"}
    ok -->|no| identityErrors["Add Identity errors to the form"]
    identityErrors --> fill
    ok -->|yes| role["Add the Student role"]
    role --> signin["Sign in"]
    signin --> dashboard["Redirect to the local return URL, otherwise to the dashboard for the account's role: /Admin for an administrator, /Student/Dashboard for a student"]
```

## 4. Login

`Controllers/AccountController.cs` (Login). Identity options: 5 failed attempts lock the account for 15 minutes.

```mermaid
flowchart TD
    open["User opens /Account/Login"] --> submit["POST email, password, remember me"]
    submit --> limit{"Within the rate limit?"}
    limit -->|no| r429["429 Too Many Requests"]
    limit -->|yes| valid{"ModelState valid?"}
    valid -->|no| form["Show the form with errors"]
    valid -->|yes| check["PasswordSignInAsync with lockoutOnFailure"]
    check --> result{"Result"}
    result -->|succeeded| local{"Return URL is local?"}
    local -->|yes| back["Redirect to the return URL"]
    local -->|no| isAdmin{"User has the Admin role?"}
    isAdmin -->|yes| admin["Redirect to /Admin"]
    isAdmin -->|no| student["Redirect to /Student/Dashboard"]
    result -->|"locked out or deactivated"| locked["This account is locked. Try again in 15 minutes, or contact the LearnHub team if it has been deactivated."]
    result -->|failed| generic["The email address or password is incorrect. (failure counted)"]
    locked --> form
    generic --> form
```

## 5. Enrolling in a course

`Controllers/CoursesController.cs` (Enroll) and `Services/EnrollmentService.cs`.

```mermaid
flowchart TD
    click["Student selects Enrol on the course page"] --> post["POST /Courses/Enroll/{id} with antiforgery token"]
    post --> role{"Signed in with the Student role?"}
    role -->|no| challenge["Login redirect or access denied"]
    role -->|yes| published{"Course exists and is published?"}
    published -->|no| notFound["404 page (a missing course and a draft look the same)"]
    published -->|yes| already{"Already enrolled?"}
    already -->|yes| infoMessage["Message: You are already enrolled in this course"]
    already -->|no| insert["Insert enrolment (UserId, CourseId, EnrolledAt, LastAccessedAt)"]
    insert --> unique{"Unique index accepted the insert?"}
    unique -->|"no: simultaneous double submit"| infoMessage
    unique -->|yes| progress["Recalculate this enrolment's stored progress"]
    progress --> success["Message: You're enrolled. Your route starts at the first lesson."]
    infoMessage --> course["Redirect to the course page"]
    success --> course
```

## 6. Opening a lesson or a lesson file

`Controllers/ResourcesController.cs` (Details, Open) and `Services/LearningResourceService.cs`.

```mermaid
flowchart TD
    request["GET /Resources/Details/{id} or /Resources/Open/{id}"] --> exists{"Resource exists and its course is published, or the user is an admin?"}
    exists -->|no| notFound["404 page"]
    exists -->|yes| preview{"Free preview lesson?"}
    preview -->|yes| allowed["Allowed"]
    preview -->|no| admin{"Administrator?"}
    admin -->|yes| allowed
    admin -->|no| signedIn{"Signed in?"}
    signedIn -->|no| login["Challenge: redirect to login with return URL"]
    signedIn -->|yes| enrolled{"Enrolled in the course?"}
    enrolled -->|no| coursePage["Redirect to the course page to enrol"]
    enrolled -->|yes| allowed
    allowed --> kind{"Details or Open?"}
    kind -->|Details| view["Show the lesson, outline, completion state and previous and next lessons"]
    kind -->|Open| stream["Stream the private file with its stored content type"]
```

## 7. Marking a lesson complete and calculating progress

`Controllers/ResourcesController.cs` (ToggleComplete), `Services/LearningResourceService.cs` and
`Services/ProgressService.cs`.

```mermaid
flowchart TD
    click["Student selects Mark as complete"] --> post["POST /Resources/ToggleComplete/{id}?nextId=..."]
    post --> found{"Lesson exists in a published course?"}
    found -->|no| notFound["404 page"]
    found -->|yes| enrolled{"Student enrolled?"}
    enrolled -->|no| warning["Warning: Enrol in this course to track your progress"]
    enrolled -->|yes| existing{"Completion record exists?"}
    existing -->|no| add["Add completion with CompletedAt"]
    existing -->|yes| remove["Remove the completion (mark as not complete)"]
    add --> next{"Now complete and a next lesson exists?"}
    next -->|yes| nextLesson["Redirect to the next lesson: Lesson complete. On to the next stop."]
    next -->|no| same["Redirect back to the lesson"]
    remove --> same
    same --> progress
    nextLesson --> progress["Progress = (published lessons completed + passed published quizzes that have questions) / (published lessons + published quizzes that have questions); ProgressService stores the result on the enrolment"]
```

## 8. Taking and grading a quiz

`Controllers/QuizzesController.cs` (Take, Result), `Services/QuizService.cs` and `Services/QuizGrader.cs`.

```mermaid
flowchart TD
    open["GET /Quizzes/Take/{id}"] --> available{"Quiz published in a published course?"}
    available -->|no| notFound["404 page"]
    available -->|yes| enrolled{"Student enrolled?"}
    enrolled -->|no| coursePage["Redirect to the course page"]
    enrolled -->|yes| form["Show questions and options without correct answers, plus a signed start token"]
    form --> answer["Student answers; counter shows answered questions"]
    answer --> unanswered{"Any unanswered questions?"}
    unanswered -->|yes| confirm{"Student confirms submitting anyway?"}
    confirm -->|no| answer
    confirm -->|yes| submit["POST answers with antiforgery token"]
    unanswered -->|no| submit
    submit --> recheck["Check quiz availability and enrolment again"]
    recheck --> load["Load questions and options from the database"]
    load --> grade["For each question: the chosen option counts only if it belongs to that question; an unanswered or foreign option id scores nothing"]
    grade --> score["Score = the points of the correct questions; percent = floor(score x 100 / total points); passed = percent >= pass mark"]
    score --> progress["Recalculate the stored course progress: a passed quiz counts towards it"]
    progress --> save["Save one QuizAttempt snapshot (start and completion times, correct count, question count, score, maximum score, percentage, passed) and one QuizAnswer per question"]
    save --> result["Redirect to /Quizzes/Result/{attemptId}"]
    result --> owner{"Attempt belongs to this student?"}
    owner -->|no| notFound
    owner -->|yes| review["Show score, pass or fail and every answer with its explanation"]
```

```mermaid
sequenceDiagram
    actor Student
    participant Browser
    participant Controller as QuizzesController
    participant Service as QuizService
    participant Grader as QuizGrader
    participant Db as ApplicationDbContext

    Student->>Browser: Choose answers and select Submit answers
    Browser->>Controller: POST /Quizzes/Take/5 (answers, antiforgery token, start token)
    Controller->>Service: SubmitAsync(quizId, userId, answers, startToken)
    Service->>Db: Find published quiz and check enrolment
    Db-->>Service: Quiz with pass mark, enrolment exists
    Service->>Db: Load questions with their points and options
    Db-->>Service: Questions and correct flags
    Service->>Grader: Grade(questions, answers, passMark)
    Grader-->>Service: Correct count, score, maximum score, passed, answers
    Service->>Service: Read StartedAt from the signed start token (falling back to the completion time)
    Service->>Db: Insert QuizAttempt with QuizAnswers
    Db-->>Service: Attempt id
    Service->>Db: Recalculate the stored course progress
    Service-->>Controller: Allowed, attempt id
    Controller-->>Browser: 302 to /Quizzes/Result/{attemptId}
    Browser->>Controller: GET /Quizzes/Result/{attemptId}
    Controller->>Service: GetResultAsync(attemptId, userId, isAdmin: false)
    Service->>Db: Attempt for this user with answers and explanations
    Db-->>Service: Result
    Controller-->>Browser: Result page
```

## 9. Admin: creating, editing and deleting a course

`Areas/Admin/Controllers/CoursesController.cs` and `Services/CourseManagementService.cs`.

```mermaid
flowchart TD
    start["Administrator opens Admin, Courses"] --> choice{"Action"}
    choice -->|"New course or Edit"| form["Fill in the course form, optional cover image"]
    form --> valid{"ModelState valid and category exists?"}
    valid -->|no| errors["Show the form with errors"]
    errors --> form
    valid -->|yes| cover{"Cover image uploaded?"}
    cover -->|yes| upload["Validate and store the image (see flowchart 11)"]
    upload --> uploadOk{"Accepted?"}
    uploadOk -->|no| errors
    uploadOk -->|yes| save
    cover -->|no| save["Save the course; UpdatedAt set automatically"]
    save --> replaced{"Edit replaced an uploaded cover?"}
    replaced -->|yes| deleteOld["Delete the old uploaded cover file"]
    replaced -->|no| details["Show the course details with a success message"]
    deleteOld --> details
    choice -->|Delete| impact["Confirmation page shows lessons, quizzes, enrolments and attempts that will be removed"]
    impact --> confirmDelete{"Administrator confirms?"}
    confirmDelete -->|no| list["Back to the list"]
    confirmDelete -->|yes| files["Collect the course's lesson files and cover"]
    files --> transaction["Transaction: delete this course's QuizAnswers, then delete the course (lessons, quizzes, questions, attempts and enrolments cascade)"]
    transaction --> cleanup["Delete the files from storage"]
    cleanup --> list
    choice -->|"Publish or move to drafts"| toggle["Toggle Published, then back to the course details"]
    toggle --> details
```

## 10. Admin: saving a question

`Areas/Admin/Controllers/QuestionsController.cs`, `ViewModels/Admin/AdminLearningViewModels.cs` and
`Services/QuestionManagementService.cs`.

```mermaid
flowchart TD
    form["Question form: text, explanation, order, up to 6 option rows, one correct radio button"] --> post["POST with antiforgery token"]
    post --> text{"Question text present and within 500 characters?"}
    text -->|no| errors["Show the form with errors"]
    text -->|yes| count{"At least 2 non-empty options (maximum 6)?"}
    count -->|no| errors
    count -->|yes| correct{"Exactly one option marked correct?"}
    correct -->|no| errors
    correct -->|yes| duplicates{"Option texts unique?"}
    duplicates -->|no| errors
    duplicates -->|yes| save["Transaction: update the question, add, update or remove options (only options of this question)"]
    save --> removed{"A removed option was chosen in past attempts?"}
    removed -->|yes| keep["Clear the selection on those answers so the attempts are kept"]
    removed -->|no| back["Back to the quiz details"]
    keep --> back
```

## 11. File upload validation

`Services/Storage/FileStorageService.cs` and `Services/Storage/UploadRules.cs`. For learning resources,
`Services/ResourceManagementService.cs` first checks that the file suits the resource type (a PDF for a PDF resource,
an image for an image resource).

```mermaid
flowchart TD
    upload["File posted from an admin form"] --> chosen{"File chosen and not empty?"}
    chosen -->|no| e1["Error: Please choose a file to upload / The selected file is empty"]
    chosen -->|yes| declared{"Declared size within the limit? (images 2 MB, PDF 10 MB)"}
    declared -->|no| e2["Error: The file is too large"]
    declared -->|yes| extension{"Extension allowed for this use? (cover: jpg, jpeg, png, webp; resource: those or pdf)"}
    extension -->|no| e3["Error: This file type is not allowed"]
    extension -->|yes| signature{"First 12 bytes are a JPEG, PNG, WebP or PDF signature allowed for this use?"}
    signature -->|no| e4["Error: The file content does not match an allowed file type (logged)"]
    signature -->|yes| name["Random file name with the extension of the detected type"]
    name --> write["Write into private storage outside wwwroot (thumbnails or resources folder)"]
    write --> actual{"Bytes actually written within the limit?"}
    actual -->|no| e5["Delete the file; error: The file is too large"]
    actual -->|yes| record["Return stored name, original name, detected content type and size for the database"]
```
