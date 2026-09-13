using LearnHub.Models;

namespace LearnHub.Data.Seed;

internal static partial class DemoCatalog
{
    private static SeedCourse ModernHtmlAndCss() => new(
        Title: "Modern HTML and CSS",
        Category: WebDevelopment,
        Difficulty: DifficultyLevel.Beginner,
        DurationMinutes: 540,
        Instructor: "Ms. Sofia Reyes",
        Cover: "html-css",
        ShortDescription: "Build accessible, responsive web pages with semantic HTML5 and modern CSS layout techniques.",
        Description: """
            Every website starts with HTML for structure and CSS for presentation. This course teaches both the way professional front-end developers use them today.

            You will write semantic HTML5 that works for screen readers and search engines, style pages with the box model, and build responsive layouts with flexbox and CSS grid. Along the way you will test pages on mobile, tablet and desktop screen sizes.
            """,
        Outcomes: """
            Structure pages with semantic HTML5 elements
            Write accessible forms with labels and helpful error messages
            Explain the CSS box model and specificity
            Create responsive layouts with flexbox and grid
            Test pages at mobile, tablet and desktop widths
            """,
        IsPublished: true,
        Resources:
        [
            new("How a web page is built", ResourceType.Article, "HTML gives meaning, CSS gives presentation and JavaScript adds behaviour.", 10, IsPreview: true, Body: """
                ## Three languages, three jobs

                - **HTML** describes what the content *is*: headings, paragraphs, lists, forms, images.
                - **CSS** describes how it *looks*: colours, spacing, typography and layout.
                - **JavaScript** describes how it *behaves* when people interact with it.

                Keeping the three separate makes pages easier to maintain and more accessible.

                ## Semantic HTML5

                ```
                <header>
                  <nav aria-label="Main">…</nav>
                </header>
                <main>
                  <article>
                    <h1>Modern HTML and CSS</h1>
                    <p>Build accessible, responsive pages.</p>
                  </article>
                </main>
                <footer>…</footer>
                ```

                Screen readers use these elements to let people jump straight to the navigation or the main content, and search engines use them to understand the page.

                > Use exactly one `h1` per page and never skip heading levels just to get a smaller font – change the size with CSS instead.

                ## The viewport meta tag

                ```
                <meta name="viewport" content="width=device-width, initial-scale=1">
                ```

                Without it, phones render the page at desktop width and shrink it, making text unreadable.
                """),
            new("HTML full tutorial for beginners", ResourceType.Video,
                "freeCodeCamp's HTML course. Watch the parts on text elements, links, images and forms.",
                45, Url: Watch("kUMe1FH4CHE")),
            new("The CSS box model", ResourceType.Image,
                "Diagram of the CSS box model: the content area is surrounded by padding, then the border, then the margin.",
                5, File: "css-box-model.png"),
            new("CSS from zero to hero", ResourceType.Video,
                "A complete CSS course. The sections on selectors, the box model and flexbox are the most useful for this course.",
                45, Url: Watch("1Rs2ND1ryYc")),
            new("Responsive layouts with flexbox and grid", ResourceType.Article, "Choose the right layout tool and adapt designs to any screen.", 15, Body: """
                ## Flexbox: one direction at a time

                Flexbox arranges items in a row **or** a column and shares space between them.

                ```
                .toolbar {
                  display: flex;
                  gap: 1rem;
                  align-items: center;
                  justify-content: space-between;
                }
                ```

                ## Grid: rows and columns together

                ```
                .course-grid {
                  display: grid;
                  grid-template-columns: repeat(auto-fill, minmax(18rem, 1fr));
                  gap: 1.25rem;
                }
                ```

                `auto-fill` with `minmax` creates as many columns as fit, so the same rule produces one column on a phone and three on a laptop – no media query needed.

                ## Media queries for bigger changes

                ```
                @media (min-width: 992px) {
                  .page { grid-template-columns: 17rem 1fr; }
                }
                ```

                Design for small screens first, then add rules for wider screens. This is called **mobile-first** CSS.

                ## Test like a user

                1. Open the browser developer tools and switch on device mode.
                2. Check a phone (375 px), a tablet (768 px) and a desktop (1280 px) width.
                3. Navigate the page using only the keyboard and make sure the focus is always visible.
                """),
            new("MDN: Learn web development", ResourceType.Link,
                "Mozilla's free curriculum with interactive exercises for HTML, CSS and JavaScript.",
                20, Url: "https://developer.mozilla.org/en-US/docs/Learn_web_development")
        ],
        Quizzes:
        [
            new("HTML and CSS essentials", "Semantic structure, accessibility and layout.", 60,
            [
                new("Which element should wrap the main navigation links of a page?",
                    "nav is the semantic element for major navigation blocks, so assistive technologies can find it quickly.",
                    1, "<div>", "<nav>", "<section>", "<aside>"),
                new("In the CSS box model, what sits directly between the content and the border?",
                    "Padding is the space inside the border around the content; margin is outside the border.",
                    1, "margin", "padding", "outline", "gap"),
                new("Which attribute connects a <label> to its input field?",
                    "The label's for attribute must match the input's id, so clicking the label focuses the field and screen readers announce it.",
                    1, "name", "for", "title", "aria-hidden"),
                new("Which CSS layout system is designed for rows and columns at the same time?",
                    "Grid controls rows and columns together; flexbox arranges items along one direction at a time.",
                    1, "flexbox", "grid", "float", "position: absolute"),
                new("What does <meta name=\"viewport\" content=\"width=device-width\"> do?",
                    "It tells mobile browsers to lay out the page at the device's width instead of a zoomed-out desktop width.",
                    1, "Disables zooming", "Makes the layout use the device's width", "Loads a separate mobile stylesheet", "Improves search ranking directly")
            ])
        ]);

    private static SeedCourse JavaScriptEssentials() => new(
        Title: "JavaScript Essentials",
        Category: WebDevelopment,
        Difficulty: DifficultyLevel.Beginner,
        DurationMinutes: 480,
        Instructor: "Mr. Daniel Okafor",
        Cover: "javascript-essentials",
        ShortDescription: "Add interactivity to web pages with modern JavaScript, the DOM and event handling.",
        Description: """
            JavaScript runs in every browser and turns static pages into interactive applications. This course covers the language fundamentals and then applies them to real page behaviour.

            You will learn variables, functions, arrays and objects, then select and change elements through the Document Object Model (DOM), respond to events and validate forms before they are submitted.
            """,
        Outcomes: """
            Use let, const, functions, arrays and objects
            Select and update page elements through the DOM
            Respond to clicks, input and form submission events
            Validate form input in the browser and explain why the server must validate again
            Debug scripts with the browser developer tools
            """,
        IsPublished: true,
        Resources:
        [
            new("Where JavaScript runs", ResourceType.Article, "The browser, the page and the difference between client and server.", 8, IsPreview: true, Body: """
                ## Client side and server side

                When you open a LearnHub page, the **server** (ASP.NET Core) builds the HTML and sends it to your browser. JavaScript then runs on the **client** – inside your browser – to make the page respond instantly without another request.

                - Server-side code can use the database and secrets. Users cannot see or change it.
                - Client-side code is downloaded to the browser. Anyone can read it, so it must never contain secrets.

                ## Adding a script to a page

                ```
                <script src="/js/site.js" defer></script>
                ```

                `defer` downloads the file in parallel and runs it after the HTML has been parsed.

                > Keep JavaScript in separate files instead of inline `onclick` attributes. It is easier to maintain and allows a strict Content Security Policy.
                """),
            new("JavaScript full course for beginners", ResourceType.Video,
                "freeCodeCamp's complete JavaScript course. Watch the sections on variables, functions, arrays and objects.",
                60, Url: Watch("PkZNo7MFNFg")),
            new("The DOM and events", ResourceType.Article, "Find elements, change them and react to what users do.", 15, Body: """
                ## Selecting elements

                ```
                const counter = document.querySelector("[data-quiz-counter]");
                const options = document.querySelectorAll("input[type=radio]");
                ```

                `querySelector` returns the first match; `querySelectorAll` returns every match.

                ## Changing the page

                ```
                counter.textContent = "3 of 5 answered";
                counter.classList.add("is-highlighted");
                ```

                Prefer `textContent` over `innerHTML` when showing text: it never interprets the value as HTML, which prevents cross-site scripting.

                ## Listening for events

                ```
                const form = document.querySelector("form[data-quiz]");
                form.addEventListener("change", () => {
                  const answered = form.querySelectorAll("input:checked").length;
                  counter.textContent = `${answered} answered`;
                });
                ```

                This is exactly how LearnHub's quiz page updates its answered counter.
                """),
            new("Client-side form validation", ResourceType.Article, "Give instant feedback in the browser without trusting it.", 12, Body: """
                ## Built-in HTML validation

                ```
                <input type="email" id="email" name="email" required maxlength="256">
                ```

                The browser already refuses an empty or badly formatted email address.

                ## Validation in ASP.NET Core

                ASP.NET Core reads data annotations such as `[Required]` and `[EmailAddress]` on a view model and writes matching `data-val-*` attributes into the HTML. The jQuery Validation Unobtrusive library uses those attributes to show messages as the user types.

                ## Why the server must check again

                1. Users can switch JavaScript off.
                2. Tools such as curl or Postman can send requests directly, skipping the page entirely.
                3. A modified browser can remove any client-side rule.

                > Client-side validation is for **convenience**. Server-side validation, using `ModelState.IsValid`, is for **correctness and security**.
                """),
            new("MDN JavaScript Guide", ResourceType.Link,
                "Mozilla's JavaScript guide: a reliable reference for the language features used in this course.",
                20, Url: "https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide")
        ],
        Quizzes:
        [
            new("JavaScript essentials quiz", "Variables, the DOM, events and validation.", 60,
            [
                new("Which keyword declares a variable that cannot be reassigned?",
                    "const creates a binding that cannot be reassigned, although an object it refers to can still change.",
                    2, "var", "let", "const", "static"),
                new("Which method returns the first element that matches a CSS selector?",
                    "querySelector returns the first match; querySelectorAll returns all matches.",
                    1, "document.getElementsByClassName", "document.querySelector", "document.querySelectorAll", "document.find"),
                new("What does event.preventDefault() do in a form's submit handler?",
                    "It cancels the browser's default action – here, sending the form – so the script can decide what happens.",
                    1, "Deletes the form", "Stops the default form submission", "Stops all other scripts", "Clears the input fields"),
                new("Why must the server validate data even when JavaScript validation exists?",
                    "Client-side checks improve usability, but requests can be sent without the page, so the server must enforce the rules.",
                    1, "JavaScript is slower than C#", "Users can disable or bypass browser scripts", "Browsers cannot compare strings", "Servers cannot read form data otherwise"),
                new("What is the result of \"5\" + 3 in JavaScript?",
                    "When one operand is a string, + performs string concatenation and produces \"53\".",
                    1, "8", "\"53\"", "NaN", "An error")
            ])
        ]);

    private static SeedCourse AspNetCoreMvc() => new(
        Title: "ASP.NET Core MVC in Practice",
        Category: WebDevelopment,
        Difficulty: DifficultyLevel.Intermediate,
        DurationMinutes: 720,
        Instructor: "Dr. Hannah Lim",
        Cover: "aspnet-core-mvc",
        ShortDescription: "Build a data-driven web application with ASP.NET Core MVC, Razor views and Entity Framework Core.",
        Description: """
            ASP.NET Core MVC is Microsoft's framework for building server-rendered web applications in C#. This course explains the Model-View-Controller pattern by building a small learning platform with the same architecture as LearnHub.

            You will follow a request from routing to a controller action, pass strongly typed view models to Razor views, validate forms with data annotations and ModelState, and store data with Entity Framework Core migrations. The final lessons cover authentication with ASP.NET Core Identity and deployment.
            """,
        Outcomes: """
            Explain how routing, controllers, models and views work together
            Pass strongly typed view models to Razor views
            Validate forms on the client and on the server
            Query and save data with Entity Framework Core
            Protect pages with authentication and role-based authorisation
            """,
        IsPublished: true,
        Resources:
        [
            new("How MVC handles a request", ResourceType.Article, "Follow one request from the browser to the database and back.", 12, IsPreview: true, Body: """
                ## The journey of a request

                Opening `/Courses/Details/5` in LearnHub triggers these steps:

                1. **Routing** matches the URL pattern `{controller}/{action}/{id}` and selects `CoursesController.Details(5)`.
                2. The **controller action** asks a service for the course data.
                3. The **service** uses Entity Framework Core to query the database.
                4. The action passes a **view model** to the Razor **view**.
                5. The view renders HTML, which is sent back to the browser.

                ## Why separate the three parts?

                - **Models** hold data and rules, and know nothing about HTML.
                - **Views** only display data, so designers can change them safely.
                - **Controllers** coordinate: they read input, call services and choose a result.

                ```
                public async Task<IActionResult> Details(int id)
                {
                    var course = await catalog.GetDetailsAsync(id, User.GetUserId(), User.IsAdmin());
                    return course is null ? NotFound() : View(course);
                }
                ```

                > Keep controllers thin. Business rules belong in services, where they can be unit tested without a web server.
                """),
            new("The MVC request pipeline", ResourceType.Image,
                "Diagram of a browser request passing through routing, a controller action, a service and the database, then back through a Razor view as HTML.",
                5, File: "mvc-request-flow.png"),
            new("ASP.NET Core MVC full course", ResourceType.Video,
                "A freeCodeCamp project course that builds a complete MVC application with Entity Framework Core.",
                60, Url: Watch("hZ1DASYd9rk")),
            new("Models, validation and ModelState", ResourceType.Article, "Validate once with data annotations and get client and server checks.", 15, Body: """
                ## Validation rules live on the view model

                ```
                public class RegisterViewModel
                {
                    [Required, StringLength(100, MinimumLength = 2)]
                    public string FullName { get; set; } = "";

                    [Required, EmailAddress]
                    public string Email { get; set; } = "";
                }
                ```

                ## The server always checks

                ```
                [HttpPost]
                public async Task<IActionResult> Register(RegisterViewModel model)
                {
                    if (!ModelState.IsValid)
                    {
                        return View(model);
                    }

                    // Create the account …
                }
                ```

                When validation fails, the same view is returned with the entered values and error messages in place.

                ## Tag helpers connect everything

                ```
                <label asp-for="Email" class="form-label"></label>
                <input asp-for="Email" class="form-control">
                <span asp-validation-for="Email"></span>
                ```

                `asp-for` generates the input name, type and `data-val-*` attributes, so the browser shows the same messages before the form is even submitted.

                > Bind forms to view models, never directly to database entities. Otherwise a user could post extra fields such as `IsAdmin=true` – an attack called overposting.
                """),
            new("Entity Framework Core basics", ResourceType.Article, "Map classes to tables, query with LINQ and evolve the schema with migrations.", 15, Body: """
                ## The DbContext

                ```
                public class ApplicationDbContext : DbContext
                {
                    public DbSet<Course> Courses => Set<Course>();
                    public DbSet<Category> Categories => Set<Category>();
                }
                ```

                Each `DbSet` becomes a table. Properties become columns, and navigation properties become foreign keys.

                ## Querying with LINQ

                ```
                var beginnerCourses = await db.Courses
                    .Where(c => c.IsPublished && c.Difficulty == DifficultyLevel.Beginner)
                    .OrderBy(c => c.Title)
                    .Select(c => new { c.Id, c.Title })
                    .ToListAsync();
                ```

                EF Core translates this into a parameterised SQL query, which protects against SQL injection.

                ## Migrations

                ```
                dotnet ef migrations add AddCourseLevel
                dotnet ef database update
                ```

                A migration is a C# file describing a schema change. Committing migrations lets every team member and the production server build exactly the same database.
                """),
            new("HTTP status codes reference", ResourceType.Pdf,
                "The status codes a web developer meets every day, with what each one means for users.",
                5, File: "http-status-codes.pdf"),
            new("ASP.NET Core MVC documentation", ResourceType.Link,
                "Microsoft's official overview of ASP.NET Core MVC, with links to routing, controllers and views.",
                20, Url: "https://learn.microsoft.com/en-us/aspnet/core/mvc/overview")
        ],
        Quizzes:
        [
            new("MVC architecture quiz", "Routing, validation, security and Entity Framework Core.", 70,
            [
                new("In ASP.NET Core MVC, which component receives the request and chooses the response?",
                    "Routing selects a controller action, which runs the application logic and returns a result such as a view or a redirect.",
                    1, "The view", "The controller action", "The model", "The layout page"),
                new("What does ModelState.IsValid report?",
                    "ModelState collects model binding and data-annotation errors; IsValid is false when any rule failed.",
                    1, "Whether the database is reachable", "Whether model binding and validation succeeded", "Whether the user is signed in", "Whether the view file exists"),
                new("Why should forms bind to view models instead of entity classes?",
                    "A view model only exposes the fields a form should accept, so extra posted values such as IsAdmin are ignored.",
                    1, "Entities cannot have properties", "To prevent overposting of fields users must not change", "View models make queries faster", "Razor cannot display entities"),
                new("Which attribute validates the anti-forgery token on a POST action?",
                    "[ValidateAntiForgeryToken] checks the hidden token that the form tag helper adds, which blocks cross-site request forgery.",
                    1, "[Authorize]", "[ValidateAntiForgeryToken]", "[HttpGet]", "[AllowAnonymous]"),
                new("Which command applies pending Entity Framework Core migrations to the database?",
                    "migrations add creates a migration file; database update runs pending migrations against the database.",
                    1, "dotnet ef migrations add", "dotnet ef database update", "dotnet build", "dotnet ef dbcontext info")
            ])
        ]);
}
