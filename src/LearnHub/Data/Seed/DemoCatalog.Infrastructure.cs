using LearnHub.Models;

namespace LearnHub.Data.Seed;

internal static partial class DemoCatalog
{
    private static SeedCourse RelationalDatabaseDesign() => new(
        Title: "Relational Database Design with SQL",
        Category: Databases,
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
                "A one-page summary of the OWASP Top 10 (2021) categories with a typical example and defence for each.",
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

    private static SeedCourse CloudFoundations() => new(
        Title: "Cloud Computing Foundations",
        Category: CloudComputing,
        Difficulty: DifficultyLevel.Beginner,
        DurationMinutes: 360,
        Instructor: "Mr. Lucas Bennett",
        Cover: "cloud-foundations",
        ShortDescription: "Learn core cloud concepts and deploy a web application to Microsoft Azure step by step.",
        Description: """
            Cloud platforms let teams run applications without buying and maintaining servers. This course explains the essential ideas behind cloud computing and applies them on Microsoft Azure.

            You will compare IaaS, PaaS and SaaS, understand regions, scaling and the shared responsibility model, and consider costs. The practical lesson walks through deploying an ASP.NET Core application to Azure App Service with an Azure SQL database.
            """,
        Outcomes: """
            Explain IaaS, PaaS and SaaS with real examples
            Describe regions, availability and elastic scaling
            Apply the shared responsibility model to security decisions
            Deploy an ASP.NET Core application to Azure App Service
            Keep secrets out of source code with application settings
            """,
        IsPublished: true,
        Resources:
        [
            new("What the cloud actually is", ResourceType.Article, "Service models, regions and who is responsible for what.", 10, IsPreview: true, Body: """
                ## Someone else's computers, as a service

                Cloud computing means renting computing resources over the internet and paying for what you use, instead of buying hardware up front.

                ## Service models

                - **IaaS** (Infrastructure as a Service) – you rent virtual machines and manage the operating system. Example: Azure Virtual Machines.
                - **PaaS** (Platform as a Service) – you deploy code and the provider runs the servers. Example: Azure App Service.
                - **SaaS** (Software as a Service) – you use finished software. Example: Microsoft 365.

                ## Regions and scaling

                A **region** is a set of datacentres in one geographic area, such as Southeast Asia. Choosing a region near your users reduces latency. **Elastic scaling** adds capacity when demand rises and removes it when demand falls.

                ## Shared responsibility

                The provider secures the physical datacentres and the platform. You remain responsible for your data, user accounts, configuration and application code.
                """),
            new("Azure Fundamentals (AZ-900) course", ResourceType.Video,
                "freeCodeCamp's preparation course for the Azure Fundamentals certification. The first hour covers cloud concepts.",
                60, Url: Watch("NKEFWyqJ5XA")),
            new("Deploying a web app to Azure App Service", ResourceType.Article, "The steps used to put an ASP.NET Core application such as LearnHub online.", 15, Body: """
                ## The architecture

                - **Azure App Service** runs the ASP.NET Core application.
                - **Azure SQL Database** stores the data.
                - **GitHub Actions** builds, tests and deploys every change to the main branch.

                ## Step by step

                1. Create a resource group, an App Service plan and a web app with the .NET 10 runtime.
                2. Create an Azure SQL server and database, and allow access from Azure services.
                3. In the web app's settings, add the connection string and set `ASPNETCORE_ENVIRONMENT` to `Production`.
                4. Store the deployment credentials as GitHub repository secrets.
                5. Push to `main`; the workflow publishes the app and runs a health check.

                ```
                az webapp config appsettings set --name learnhub-app --resource-group learnhub-rg --settings Database__Provider=SqlServer
                ```

                > Configuration keys use double underscores (`Database__Provider`) in environment variables to represent the nested `Database:Provider` setting.

                ## Keep secrets out of Git

                Passwords and connection strings belong in App Service settings or Azure Key Vault, never in `appsettings.json` or source code.
                """),
            new("Azure App Service documentation", ResourceType.Link,
                "Microsoft's overview of Azure App Service: features, pricing tiers and quickstarts.",
                15, Url: "https://learn.microsoft.com/en-us/azure/app-service/overview")
        ],
        Quizzes:
        [
            new("Cloud concepts quiz", "Service models, responsibility, scaling and configuration.", 60,
            [
                new("Azure App Service, where you deploy code and Azure manages the servers, is an example of…",
                    "Platform as a Service provides a managed runtime: you supply the application and the provider manages the servers and operating system.",
                    1, "IaaS", "PaaS", "SaaS", "On-premises hosting"),
                new("Under the shared responsibility model, who is responsible for the data you store in a cloud application?",
                    "Providers secure the platform, but customers always remain responsible for their data, accounts and access.",
                    1, "Only the cloud provider", "You, the customer", "Nobody", "The internet service provider"),
                new("What does elastic scaling mean?",
                    "Elasticity adds capacity during busy periods and removes it when demand drops, so you pay for what you use.",
                    0, "Resources grow and shrink with demand", "Servers are moved between regions every night", "Prices stay fixed regardless of usage", "Data is copied to every region"),
                new("Where should a production database password for an App Service app be stored?",
                    "App Service application settings or Azure Key Vault inject values at runtime, so they are never committed to source control.",
                    1, "In appsettings.json in the Git repository", "In App Service application settings or Azure Key Vault", "In a JavaScript file", "In the README"),
                new("What is an Azure region?",
                    "A region is a geographic area containing one or more datacentres; choosing one close to users reduces latency.",
                    1, "A pricing plan", "A set of datacentres in a geographic area", "A virtual network", "A group of users")
            ])
        ]);
}
