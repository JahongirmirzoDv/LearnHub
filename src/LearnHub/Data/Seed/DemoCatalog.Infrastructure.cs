using LearnHub.Models;

namespace LearnHub.Data.Seed;

internal static partial class DemoCatalog
{
    private static SeedCourse RelationalDatabaseDesign() => new(
        Title: "Relational Database Design with SQL",
        Category: Database,
        Difficulty: DifficultyLevel.Intermediate,
        DurationMinutes: 600,
        Instructor: "Prof. Mei Ling Tan",
        Cover: "relational-databases",
        ShortDescription: "Design normalised relational databases and query them confidently with joins and aggregates.",
        Description: """
            Most applications keep their data in a relational database. Good design keeps that data accurate; good queries turn it into useful information.

            This course covers entities, primary and foreign keys, relationships and entity-relationship diagrams. You will normalise a messy table to third normal form and write SQL queries that filter, sort, join and summarise data. The examples use a course-enrolment database similar to LearnHub's own schema.
            """,
        Outcomes: """
            Identify entities, attributes and relationships from requirements
            Draw entity-relationship diagrams with the correct cardinality
            Choose primary keys, foreign keys and constraints
            Normalise tables to third normal form
            Write SELECT queries with WHERE, ORDER BY, JOIN and GROUP BY
            """,
        IsPublished: true,
        Resources:
        [
            new("Tables, keys and relationships", ResourceType.Article, "The vocabulary of relational design, using a student enrolment example.", 12, IsPreview: true, Body: """
                ## Entities become tables

                An **entity** is something the system must remember: a student, a course, an enrolment. Each entity becomes a table and each attribute becomes a column.

                ## Keys identify and connect rows

                - A **primary key** uniquely identifies each row, for example `Courses.Id`.
                - A **foreign key** points to a row in another table, for example `Enrolments.CourseId`.
                - A **unique constraint** stops duplicates, for example one enrolment per student per course.

                ## Relationship types

                1. **One-to-many** – one category has many courses. The foreign key goes on the "many" side.
                2. **Many-to-many** – students take many courses and courses have many students. A linking table (`Enrolments`) holds pairs of foreign keys.
                3. **One-to-one** – rare; often a sign that two tables should be merged.

                ```
                CREATE TABLE Enrolments (
                    Id INT PRIMARY KEY,
                    StudentId INT NOT NULL REFERENCES Students(Id),
                    CourseId INT NOT NULL REFERENCES Courses(Id),
                    EnrolledAt DATETIME2 NOT NULL,
                    CONSTRAINT UQ_Enrolment UNIQUE (StudentId, CourseId)
                );
                ```

                > Decide what should happen when a parent row is deleted. Should deleting a course delete its enrolments (cascade) or be blocked (restrict)? Make the choice deliberately.
                """),
            new("SQL full database course", ResourceType.Video,
                "freeCodeCamp's SQL course. Watch the sections on creating tables, inserting data and writing queries.",
                60, Url: Watch("HXV3zeQKqGY")),
            new("Example ER diagram", ResourceType.Image,
                "Entity-relationship diagram of a course enrolment database: Student and Course tables linked through an Enrolment table with foreign keys.",
                5, File: "erd-example.png"),
            new("Database normalisation explained", ResourceType.Video,
                "Decomplexify's clear walkthrough of first to fifth normal form with examples.",
                28, Url: Watch("GFQaEYEc8_8")),
            new("Writing JOIN queries", ResourceType.Article, "Combine rows from related tables and summarise them.", 15, Body: """
                ## INNER JOIN: only matching rows

                ```
                SELECT s.FullName, c.Title, e.EnrolledAt
                FROM Enrolments AS e
                INNER JOIN Students AS s ON s.Id = e.StudentId
                INNER JOIN Courses AS c ON c.Id = e.CourseId
                ORDER BY e.EnrolledAt DESC;
                ```

                ## LEFT JOIN: keep rows without a match

                ```
                SELECT c.Title, COUNT(e.Id) AS Learners
                FROM Courses AS c
                LEFT JOIN Enrolments AS e ON e.CourseId = c.Id
                GROUP BY c.Title;
                ```

                Courses with no enrolments still appear, with a count of 0. An INNER JOIN would hide them.

                ## Filtering groups with HAVING

                ```
                SELECT c.Title, AVG(a.ScorePercent) AS AverageScore
                FROM QuizAttempts AS a
                INNER JOIN Quizzes AS q ON q.Id = a.QuizId
                INNER JOIN Courses AS c ON c.Id = q.CourseId
                GROUP BY c.Title
                HAVING AVG(a.ScorePercent) < 70;
                ```

                `WHERE` filters rows **before** grouping; `HAVING` filters the groups **after** aggregation.
                """),
            new("Exercise: an enrolment report", ResourceType.Exercise, "Write a JOIN with GROUP BY for a real reporting question.", 15,
                Body: """
                    ## Your task

                    A learning system has these tables:

                    - `Courses(Id, Title)`
                    - `Enrollments(Id, UserId, CourseId, EnrolledAt)`

                    Write one query that lists **every** course title with the number of students enrolled in it, including courses nobody has joined yet (they should show 0). Sort the most popular course first.
                    """,
                Solution: """
                    ```
                    SELECT c.Title, COUNT(e.Id) AS StudentCount
                    FROM Courses AS c
                    LEFT JOIN Enrollments AS e ON e.CourseId = c.Id
                    GROUP BY c.Id, c.Title
                    ORDER BY StudentCount DESC, c.Title;
                    ```

                    - A `LEFT JOIN` keeps courses without enrolments; an `INNER JOIN` would drop them.
                    - `COUNT(e.Id)` counts only matched rows, so unmatched courses get 0. `COUNT(*)` would wrongly count them as 1.
                    """),
            new("SQL JOIN cheat sheet", ResourceType.Pdf,
                "One page comparing INNER, LEFT, RIGHT and FULL joins with example queries.",
                5, File: "sql-joins-cheat-sheet.pdf")
        ],
        Quizzes:
        [
            new("SQL and normalisation quiz", "Keys, relationships, joins and normal forms.", 60,
            [
                new("What uniquely identifies each row in a table?",
                    "A primary key has a unique, non-null value for every row.",
                    1, "A foreign key", "A primary key", "An index", "A view"),
                new("How is a many-to-many relationship between students and courses usually implemented?",
                    "A linking table holds pairs of foreign keys (StudentId, CourseId) and can store extra data such as the enrolment date.",
                    1, "A comma-separated list of course names in Students", "A linking table such as Enrolments", "Two primary keys in the Courses table", "It cannot be represented in SQL"),
                new("Which JOIN returns only rows that have matching values in both tables?",
                    "INNER JOIN keeps only matching pairs; LEFT JOIN also keeps unmatched rows from the left table.",
                    1, "LEFT JOIN", "INNER JOIN", "FULL OUTER JOIN", "CROSS JOIN"),
                new("A table is in second normal form when it is in first normal form and…",
                    "2NF removes partial dependencies: every non-key column must depend on the whole of a composite primary key.",
                    1, "it contains no NULL values", "every non-key column depends on the whole primary key", "it has at least two indexes", "it only uses integer keys"),
                new("Which clause filters groups after aggregation?",
                    "WHERE filters rows before grouping; HAVING filters the groups produced by GROUP BY.",
                    2, "WHERE", "ORDER BY", "HAVING", "DISTINCT")
            ])
        ]);

    private static SeedCourse WebSecurityBasics() => new(
        Title: "Web Application Security Basics",
        Category: Cybersecurity,
        Difficulty: DifficultyLevel.Intermediate,
        DurationMinutes: 480,
        Instructor: "Mr. Farid Hassan",
        Cover: "web-security",
        ShortDescription: "Recognise the most common web vulnerabilities and apply proven defences in your own applications.",
        Description: """
            Security problems in web applications usually come from a small number of recurring mistakes. This course introduces them using the OWASP Top 10 as a guide.

            You will see how injection, cross-site scripting, cross-site request forgery and broken access control attacks work, and then apply the standard defences: parameterised queries, output encoding, anti-forgery tokens, secure password storage and ownership checks.
            """,
        Outcomes: """
            Explain the OWASP Top 10 risk categories
            Prevent SQL injection with parameterised queries
            Stop cross-site scripting with output encoding and a Content Security Policy
            Protect state-changing requests with anti-forgery tokens
            Check ownership to prevent insecure direct object references
            """,
        IsPublished: true,
        Resources:
        [
            new("Thinking like an attacker", ResourceType.Article, "Where attacks come from and the habits that stop them.", 10, IsPreview: true, Body: """
                ## Every input is untrusted

                Attackers do not use your forms the way you expect. They edit URLs, change hidden fields, replay requests and send data your page could never produce. A secure application assumes **all input is hostile until validated on the server**.

                ## Four questions for every feature

                1. **Who** is allowed to do this? (authentication and authorisation)
                2. **Which** records may they touch? (ownership checks)
                3. **What** data can they send? (validation and length limits)
                4. **Where** does the data end up? (SQL, HTML, files – each needs the right encoding)

                ## Defence in depth

                No single control is perfect, so layers are combined:

                - Razor encodes output **and** a Content Security Policy blocks inline scripts.
                - Account lockout **and** rate limiting slow down password guessing.
                - The UI hides admin links **and** the server rejects non-admin requests.

                > Hiding a button is not security. The server must enforce every rule, because requests can be sent without your page.
                """),
            new("Injection attacks (OWASP Top 10)", ResourceType.Video,
                "A short explanation of injection attacks by F5 DevCentral. Injection is still part of the OWASP Top 10 today.",
                8, Url: Watch("rWHvp7rUka8")),
            new("Preventing XSS, CSRF and IDOR", ResourceType.Article, "Three common vulnerabilities and the defences used in LearnHub.", 15, Body: """
                ## Cross-site scripting (XSS)

                XSS happens when user-supplied text is placed into a page as HTML, so a `<script>` tag written by an attacker runs in other people's browsers.

                - Razor HTML-encodes everything written with `@value`.
                - Never output user content with `Html.Raw`.
                - A Content Security Policy such as `script-src 'self'` blocks inline scripts even if one slips through.

                ## Cross-site request forgery (CSRF)

                A malicious site can make a signed-in user's browser submit a form to your site. ASP.NET Core adds a hidden anti-forgery token to every form and rejects POST requests without a valid token:

                ```
                builder.Services.AddControllersWithViews(options =>
                    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
                ```

                ## Insecure direct object references (IDOR)

                If `/Quizzes/Result/15` shows a result, a curious student will try `/Quizzes/Result/16`. The server must check ownership:

                ```
                var attempt = await db.QuizAttempts
                    .Where(a => a.Id == id && a.UserId == currentUserId)
                    .FirstOrDefaultAsync();
                return attempt is null ? NotFound() : View(attempt);
                ```

                > Returning 404 instead of 403 also avoids confirming that the record exists.
                """),
            new("OWASP Top 10 summary", ResourceType.Pdf,
                "A one-page summary of the OWASP Top 10:2025 categories with a typical example and defence for each.",
                5, File: "owasp-top-10-summary.pdf"),
            new("OWASP Top 10 project", ResourceType.Link,
                "The official OWASP Top 10 project page with detailed descriptions of each risk.",
                15, Url: "https://owasp.org/www-project-top-ten/")
        ],
        Quizzes:
        [
            new("Web security quiz", "Injection, XSS, CSRF, access control and password storage.", 70,
            [
                new("What is the most reliable defence against SQL injection?",
                    "Parameters keep user input as data, so it can never change the structure of the SQL statement.",
                    1, "Hiding database error messages", "Parameterised queries", "Removing quote characters from input", "Using POST instead of GET"),
                new("A comment containing a <script> tag runs in other users' browsers. What vulnerability is this?",
                    "The script is stored on the server and executed when other people view the page – stored cross-site scripting.",
                    1, "Cross-site request forgery", "Stored cross-site scripting", "SQL injection", "Clickjacking"),
                new("What does an anti-forgery token protect against?",
                    "The token proves the form was produced by the site itself, so another site cannot submit it on the user's behalf.",
                    1, "Password guessing", "Cross-site request forgery", "Denial of service", "Session timeout"),
                new("A student changes /Quizzes/Result/15 to /Quizzes/Result/16 and sees another student's result. What is missing?",
                    "The server must check that the requested record belongs to the current user; otherwise it is an insecure direct object reference.",
                    1, "HTTPS", "An ownership check", "An input length limit", "A CAPTCHA"),
                new("How should user passwords be stored?",
                    "A slow, salted hash (such as the PBKDF2 hashing used by ASP.NET Core Identity) cannot be reversed even if the database leaks.",
                    1, "Encrypted with a key stored in the source code", "As a slow, salted hash", "As plain text in a separate table", "Base64 encoded")
            ])
        ]);

    private static SeedCourse NetworkingFundamentals() => new(
        Title: "Networking Fundamentals",
        Category: Networking,
        Difficulty: DifficultyLevel.Beginner,
        DurationMinutes: 540,
        Instructor: "Ms. Priyanka Rao",
        Cover: "networking-fundamentals",
        ShortDescription: "Understand how devices communicate: network models, IP addressing, subnets and core protocols.",
        Description: """
            Every web request travels across networks built on shared rules. This course explains those rules, from the physical cable up to the application.

            You will use the OSI and TCP/IP models to describe communication, calculate IPv4 subnets and learn what DNS, DHCP, TCP, UDP and HTTP each contribute. Practical examples show how to troubleshoot a connection with common command-line tools.
            """,
        Outcomes: """
            Describe the layers of the OSI and TCP/IP models
            Explain the roles of switches, routers and access points
            Calculate IPv4 subnet ranges using CIDR notation
            Compare TCP and UDP and choose the right one for an application
            Troubleshoot connectivity with ping, traceroute and nslookup
            """,
        IsPublished: true,
        Resources:
        [
            new("How data travels across a network", ResourceType.Article, "What happens between typing a web address and seeing the page.", 10, IsPreview: true, Body: """
                ## From a URL to a page

                1. **DNS** translates `learnhub.example` into an IP address.
                2. Your computer opens a **TCP** connection to that address on port 443.
                3. **TLS** encrypts the connection so nobody in between can read it.
                4. The browser sends an **HTTP** request and the server replies with HTML.

                ## Devices along the way

                - A **switch** connects devices on the same local network using MAC addresses.
                - A **router** forwards packets between networks using IP addresses.
                - An **access point** connects wireless devices to the wired network.

                ## Encapsulation

                Each layer wraps the data from the layer above: an HTTP message goes inside a TCP segment, inside an IP packet, inside an Ethernet or Wi-Fi frame. The receiving device unwraps them in reverse order.

                > Try it: run `nslookup learn.microsoft.com` and `tracert` (Windows) or `traceroute` (macOS, Linux) to watch these steps happen.
                """),
            new("The OSI model", ResourceType.Image,
                "Diagram of the seven OSI layers, from Physical at the bottom to Application at the top, with example protocols for each layer.",
                5, File: "osi-model.png"),
            new("Computer networking course", ResourceType.Video,
                "A complete networking course by freeCodeCamp. Start with the chapters on the OSI model and IP addressing.",
                60, Url: Watch("qiQR5rTSshw")),
            new("IP addresses and subnets", ResourceType.Article, "Read CIDR notation and work out the size of a subnet.", 15, Body: """
                ## IPv4 addresses

                An IPv4 address such as `192.168.10.25` has 32 bits, written as four numbers from 0 to 255. Part of the address identifies the **network** and the rest identifies the **host**.

                ## CIDR notation

                `/24` means the first 24 bits are the network part. That leaves 8 bits for hosts:

                - 2⁸ = 256 addresses in the subnet
                - minus the network address and the broadcast address
                - = **254 usable host addresses**

                ## Worked example

                For `192.168.10.0/26`:

                1. Host bits: 32 − 26 = 6, so 2⁶ = 64 addresses per subnet.
                2. Network address: `192.168.10.0`
                3. Usable hosts: `192.168.10.1` to `192.168.10.62`
                4. Broadcast address: `192.168.10.63`

                > Private ranges such as `10.0.0.0/8` and `192.168.0.0/16` are used inside organisations and are not routed on the public internet.
                """),
            new("Subnetting quick reference", ResourceType.Pdf,
                "A table of common CIDR prefixes with subnet masks, address counts and usable hosts.",
                5, File: "subnetting-quick-reference.pdf")
        ],
        Quizzes:
        [
            new("Networking basics quiz", "Models, devices, addressing and protocols.", 60,
            [
                new("At which OSI layer do routers forward packets using IP addresses?",
                    "Routers make forwarding decisions with IP addresses at layer 3, the Network layer.",
                    1, "Layer 2 – Data Link", "Layer 3 – Network", "Layer 4 – Transport", "Layer 7 – Application"),
                new("How many usable host addresses are in a /24 IPv4 subnet?",
                    "A /24 has 256 addresses; the network and broadcast addresses cannot be assigned, leaving 254.",
                    1, "256", "254", "255", "128"),
                new("Which protocol translates a domain name into an IP address?",
                    "DNS resolves names to IP addresses. DHCP assigns addresses to devices, and ARP maps IP addresses to MAC addresses.",
                    1, "DHCP", "DNS", "ARP", "FTP"),
                new("Why is UDP often used for live video calls?",
                    "UDP skips connection set-up and retransmission, so late packets are dropped instead of delaying the stream.",
                    1, "It guarantees delivery of every packet", "It has lower overhead and latency", "It encrypts data automatically", "Browsers require it for video"),
                new("Which device connects computers within the same local network using MAC addresses?",
                    "A switch forwards frames between devices on the same LAN using their MAC addresses.",
                    1, "Router", "Switch", "Modem", "Firewall")
            ])
        ]);

    private static SeedCourse DeployingWebApplications() => new(
        Title: "Deploying Web Applications",
        Category: SoftwareEngineering,
        Difficulty: DifficultyLevel.Intermediate,
        DurationMinutes: 300,
        Instructor: "Mr. Lucas Bennett",
        Cover: "cloud-foundations",
        ShortDescription: "Package an ASP.NET Core app in a container and run it on a cloud platform with persistent storage.",
        Description: """
            A web application is only useful once people can reach it. This course follows the path LearnHub itself takes from a laptop to the internet.

            You will compare hosting models, package an application as a Docker image, configure it with environment variables instead of committed secrets, keep a SQLite database on a persistent volume, and publish a static landing page on a content delivery network.
            """,
        Outcomes: """
            Explain IaaS, PaaS and static hosting with real examples
            Describe what a container image contains and why it helps deployment
            Configure an application through environment variables and keep secrets out of Git
            Keep application data on a persistent volume that survives redeployments
            Choose between a server-side host and a static host for each part of a system
            """,
        IsPublished: true,
        Resources:
        [
            new("Where web applications run", ResourceType.Article, "Hosting models, and why a server-side app and a static site need different hosts.", 10, IsPreview: true, Body: """
                ## Renting computers as a service

                Cloud platforms let a team run software without buying servers. You pay for what you use and the provider looks after the hardware.

                ## Hosting models

                - **IaaS** (Infrastructure as a Service) – you rent virtual machines and manage the operating system yourself.
                - **PaaS** (Platform as a Service) – you hand over code or a container image and the platform runs it, restarts it and gives it a web address. Railway, Render and Heroku are examples.
                - **Static hosting** – files such as HTML, CSS and images are copied to a content delivery network (CDN) and served as they are. Firebase Hosting, Netlify and GitHub Pages are examples.

                ## Why one system can use two hosts

                An ASP.NET Core MVC application runs C# on the server for every request: it checks the login cookie, queries the database and renders Razor views. A static host cannot run that code, so it cannot host the application itself.

                A static host is still ideal for a fast landing page that describes the project and links to the running application. LearnHub uses exactly this split: the MVC app runs on a PaaS, and its presentation site is a static page on a CDN.

                ## Shared responsibility

                The provider secures its datacentres and platform. You remain responsible for your code, your configuration, your users' data and who can access it.
                """),
            new("Docker crash course", ResourceType.Video,
                "TechWorld with Nana explains images, containers and Dockerfiles for complete beginners.",
                60, Url: Watch("pg19Z8LL06w")),
            new("From Dockerfile to a running container", ResourceType.Article, "How a multi-stage Dockerfile builds a small, secure image for ASP.NET Core.", 15, Body: """
                ## Build once, run anywhere

                A **container image** bundles the published application with the exact runtime it needs. The same image runs on a laptop, in CI and on the hosting platform, so "it works on my machine" problems disappear.

                ## A multi-stage Dockerfile

                ```
                FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
                WORKDIR /src
                COPY . .
                RUN dotnet publish src/LearnHub/LearnHub.csproj -c Release -o /app

                FROM mcr.microsoft.com/dotnet/aspnet:10.0
                WORKDIR /app
                COPY --from=build /app .
                USER $APP_UID
                ENTRYPOINT ["dotnet", "LearnHub.dll"]
                ```

                1. The **build stage** uses the large SDK image to compile and publish the app.
                2. The **final stage** starts from the much smaller runtime image and copies in only the published output.
                3. `USER $APP_UID` runs the app as a normal user instead of root, which limits the damage if it is ever compromised.

                ## Listening on the right port

                Platforms tell the container which port to use through a `PORT` environment variable. The application must listen on that port, on all network interfaces, or the platform's health check will never reach it.
                """),
            new("Configuration, secrets and persistent data", ResourceType.Article, "Environment variables, volumes and a deployment checklist.", 12, Body: """
                ## Configuration belongs to the environment

                The same image must work in development, testing and production, so settings that change between environments come from **environment variables**, not from files in the image.

                ```
                ASPNETCORE_ENVIRONMENT=Production
                DATABASE_CONNECTION_STRING=Data Source=/data/learnhub.db
                Seed__AdminPassword=(set in the platform, never in Git)
                ```

                > A double underscore (`Seed__AdminPassword`) represents the nested setting `Seed:AdminPassword` in ASP.NET Core configuration.

                ## Secrets never go into Git

                Passwords, API keys and connection strings with credentials are entered in the hosting platform's variables screen. The repository contains only a `.env.example` file with placeholder values, and `.gitignore` stops real `.env` files from being committed.

                ## Data must outlive the container

                Containers are replaced on every deployment and anything written inside them is lost. A **persistent volume** is storage mounted into the container, for example at `/data`, that survives redeployments. Put the SQLite database file, uploaded files and ASP.NET Core Data Protection keys there.

                ## Deployment checklist

                - The health check endpoint (for example `/health`) returns 200.
                - Database migrations run once on start-up.
                - HTTPS is enforced and cookies are marked `Secure`.
                - Error pages never show stack traces in production.
                """),
            new("Deployment exercise: plan the environment variables", ResourceType.Exercise, "Decide which settings belong in the platform and which in the repository.", 10,
                Body: """
                    ## Your task

                    A classmate wants to deploy a course-booking app. Their `appsettings.json` currently contains:

                    - the log level
                    - the SQLite connection string `Data Source=/data/booking.db`
                    - the administrator's password
                    - the name of the site shown in the footer

                    For each setting, decide whether it should stay in `appsettings.json` (committed to Git) or move to an environment variable set in the hosting platform. Give a one-sentence reason for each.
                    """,
                Solution: """
                    - **Log level** – can stay in `appsettings.json` as a default; production can override it with an environment variable if needed.
                    - **Connection string** – move to an environment variable, because the database path depends on where the platform mounts its volume.
                    - **Administrator password** – must be an environment variable (a secret). Anything committed to Git should be treated as public.
                    - **Site name** – can stay in `appsettings.json`; it is not secret and rarely changes between environments.
                    """),
            new("Railway documentation", ResourceType.Link,
                "Official guides for deploying services, adding volumes and setting variables on Railway.",
                15, Url: "https://docs.railway.com/"),
            new("Firebase Hosting documentation", ResourceType.Link,
                "Google's guide to publishing static sites on Firebase Hosting's global CDN.",
                10, Url: "https://firebase.google.com/docs/hosting")
        ],
        Quizzes:
        [
            new("Deployment concepts quiz", "Hosting models, containers, configuration and persistent data.", 60,
            [
                new("Why can't Firebase Hosting run an ASP.NET Core MVC application by itself?",
                    "Static hosts serve files as they are. MVC renders pages with C# code on the server for every request, which needs a server-side host.",
                    1, "It does not support HTTPS", "It serves static files and cannot execute server-side C#", "It only works with Java applications", "It cannot serve HTML files") { Points = 2 },
                new("What is the main benefit of a multi-stage Dockerfile?",
                    "The SDK is only used to build; the final image contains just the runtime and the published app, so it is smaller and has less attack surface.",
                    0, "The final image is smaller because build tools are left behind", "The application runs faster on every request", "It removes the need for a database", "It makes the container run as root"),
                new("Where should the production administrator password be stored?",
                    "Secrets are entered as environment variables in the hosting platform, so they never reach the Git history.",
                    1, "In appsettings.json", "In an environment variable set in the hosting platform", "In the README as a reminder", "In a JavaScript file") { Points = 2 },
                new("What happens to a SQLite file written inside a container without a volume when the app is redeployed?",
                    "A redeployment replaces the container, so files written inside it are lost unless they live on a persistent volume.",
                    2, "It is copied to the new container", "It is uploaded to GitHub", "It is lost with the old container", "It is converted to SQL Server"),
                new("Which environment variable does a platform such as Railway use to tell the app which port to listen on?",
                    "Railway sets PORT, and the application must listen on it for the platform's routing and health checks to work.",
                    3, "HOST", "URL", "APP_PORT_NUMBER", "PORT")
            ])
        ]);
}
