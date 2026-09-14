using LearnHub.Models;

namespace LearnHub.Data.Seed;

internal static partial class DemoCatalog
{
    private static SeedCourse CSharpFundamentals() => new(
        Title: "C# Programming Fundamentals",
        Category: Programming,
        Difficulty: DifficultyLevel.Beginner,
        DurationMinutes: 480,
        Instructor: "Dr. Hannah Lim",
        Cover: "csharp-fundamentals",
        ShortDescription: "Learn the building blocks of C# – types, decisions, loops, methods and classes – by writing small console programs.",
        Description: """
            C# is the main language of the .NET platform and is used for web applications, desktop software, cloud services and games. This course starts from zero and focuses on writing correct, readable code.

            You will install the .NET SDK, create console applications and learn how variables, operators, conditions and loops work. The second half introduces methods and classes so you can organise larger programs.

            Every lesson ends with a short practice task. When you finish, you are ready for the ASP.NET Core MVC in Practice course.
            """,
        Outcomes: """
            Create, build and run a console application with the .NET CLI
            Choose suitable types such as int, decimal, bool and string
            Control program flow with if, switch, foreach and while
            Split code into well-named methods with parameters and return values
            Model real concepts with classes, properties and constructors
            Read user input safely with TryParse
            """,
        IsPublished: true,
        Resources:
        [
            new("Your first C# program", ResourceType.Article, "Install the SDK, create a console app and run it from the terminal.", 10, IsPreview: true, Body: """
                ## What you need

                - The .NET SDK (version 10 or later) from dotnet.microsoft.com
                - A code editor such as Visual Studio, Visual Studio Code or JetBrains Rider

                ## Create a console app

                Open a terminal and run these commands:

                ```
                dotnet new console -o HelloLearnHub
                cd HelloLearnHub
                dotnet run
                ```

                The template creates **Program.cs**, which contains a single statement:

                ```
                Console.WriteLine("Hello, LearnHub!");
                ```

                `dotnet run` compiles the project and runs it. Change the message, save the file and run it again.

                > Tip: `dotnet watch` rebuilds and restarts the program every time you save a file.

                ## What happens when the program runs

                1. The C# compiler turns your code into Intermediate Language (IL).
                2. The .NET runtime loads the IL and compiles it to machine code while it runs.
                3. The output appears in the terminal.

                ## Practice

                Change the program so it asks for your name with `Console.ReadLine()` and greets you by name.
                """),
            new("C# full course for beginners", ResourceType.Video,
                "A complete beginner walkthrough by freeCodeCamp. Watch the chapters on variables, data types, if statements and loops, then come back to the next lesson.",
                60, Url: Watch("GhQdlIFylQ8")),
            new("Variables, types and decisions", ResourceType.Article, "Store values in variables, make decisions and repeat work with loops.", 15, Body: """
                ## Variables hold values

                A variable has a **type**, a **name** and a **value**:

                ```
                int enrolledCourses = 3;
                decimal averageScore = 82.5m;
                bool hasPassed = true;
                string courseTitle = "C# Programming Fundamentals";
                ```

                Use `decimal` for money and scores that must not suffer from rounding errors, and `double` for scientific measurements.

                ## Making decisions

                ```
                if (averageScore >= 70)
                {
                    Console.WriteLine("Passed");
                }
                else
                {
                    Console.WriteLine("Keep practising");
                }
                ```

                A `switch` expression is clearer when there are several cases:

                ```
                string grade = averageScore switch
                {
                    >= 85 => "Distinction",
                    >= 70 => "Pass",
                    _ => "Not yet"
                };
                ```

                ## Repeating work with loops

                - `for` repeats a known number of times.
                - `foreach` visits every item in a collection.
                - `while` repeats until a condition becomes false.

                ```
                string[] lessons = ["Variables", "Loops", "Methods"];
                foreach (string lesson in lessons)
                {
                    Console.WriteLine($"Next lesson: {lesson}");
                }
                ```

                ## Reading numbers safely

                `int.Parse` throws an exception when the text is not a number. `int.TryParse` returns `false` instead, so your program can ask again:

                ```
                Console.Write("How many hours did you study? ");
                if (int.TryParse(Console.ReadLine(), out int hours))
                {
                    Console.WriteLine($"{hours} hours logged.");
                }
                else
                {
                    Console.WriteLine("Please enter a whole number.");
                }
                ```
                """),
            new("Methods and classes", ResourceType.Article, "Organise code into methods and model real concepts with classes.", 15, Body: """
                ## Methods keep code organised

                A method groups statements under a name. Parameters pass data in, and the return type describes what comes back.

                ```
                static decimal CalculateAverage(int[] scores)
                {
                    if (scores.Length == 0)
                    {
                        return 0;
                    }

                    return (decimal)scores.Sum() / scores.Length;
                }
                ```

                Good method names start with a verb and describe one job: `CalculateAverage`, `PrintReport`, `IsValidEmail`.

                ## Classes model real things

                ```
                public class Course
                {
                    public Course(string title, int durationMinutes)
                    {
                        Title = title;
                        DurationMinutes = durationMinutes;
                    }

                    public string Title { get; }
                    public int DurationMinutes { get; }

                    public string FormattedDuration() => $"{DurationMinutes / 60} h {DurationMinutes % 60} min";
                }
                ```

                Create objects with `new` and call their members:

                ```
                var course = new Course("C# Programming Fundamentals", 480);
                Console.WriteLine(course.FormattedDuration());
                ```

                > LearnHub itself works this way: every course, lesson and quiz is a C# class that Entity Framework Core stores in the database.

                ## Practice

                Write a `Student` class with a `FullName` property and a method that returns the student's initials.
                """),
            new("Exercise: a grade calculator", ResourceType.Exercise, "Combine variables, decisions and a method in one small program.", 15,
                Body: """
                    ## Your task

                    Write a method `string GradeFor(int score)` that turns a quiz score (0–100) into a grade:

                    - 80 or more → `"Distinction"`
                    - 65 to 79 → `"Credit"`
                    - 50 to 64 → `"Pass"`
                    - below 50 → `"Fail"`

                    Then call it for the scores 92, 70, 50 and 12 and print each result. Scores outside 0–100 should throw an `ArgumentOutOfRangeException`.

                    Try it yourself before revealing the solution.
                    """,
                Solution: """
                    ```
                    static string GradeFor(int score)
                    {
                        if (score is < 0 or > 100)
                        {
                            throw new ArgumentOutOfRangeException(nameof(score), "Scores run from 0 to 100.");
                        }

                        return score switch
                        {
                            >= 80 => "Distinction",
                            >= 65 => "Credit",
                            >= 50 => "Pass",
                            _ => "Fail"
                        };
                    }

                    foreach (var score in new[] { 92, 70, 50, 12 })
                    {
                        Console.WriteLine($"{score}: {GradeFor(score)}");
                    }
                    ```

                    The `switch` expression checks the patterns from top to bottom, so each band only needs its lower limit.
                    """),
            new("C# syntax cheat sheet", ResourceType.Pdf, "A two-page reference of the syntax used in this course. Keep it open while you practise.", 5, File: "csharp-cheat-sheet.pdf")
        ],
        Quizzes:
        [
            new("C# fundamentals check", "Five questions on the core syntax from this course.", 60,
            [
                new("Which type is the best choice for storing a course fee such as 1250.50?",
                    "decimal stores base-10 values exactly, so fees and money are not affected by binary rounding errors.",
                    2, "int", "double", "decimal", "string"),
                new("What does int.TryParse return when the text is not a valid number?",
                    "TryParse returns false and sets the out variable to 0 instead of throwing an exception.",
                    1, "It throws a FormatException", "It returns false", "It returns true and sets the value to 0", "It returns null"),
                new("Which loop is designed to visit every item in a collection?",
                    "foreach iterates over each element of a collection without managing an index.",
                    1, "for", "foreach", "while", "do … while"),
                new("What is printed by Console.WriteLine(7 / 2);?",
                    "Both operands are int, so integer division discards the remainder and prints 3.",
                    1, "3.5", "3", "4", "3.0"),
                new("Which command compiles and runs a .NET console project?",
                    "dotnet run builds the project when needed and then starts it; dotnet build only compiles.",
                    2, "dotnet build", "dotnet new console", "dotnet run", "dotnet publish")
            ])
        ]);

    private static SeedCourse PythonProblemSolving() => new(
        Title: "Python for Problem Solving",
        Category: Programming,
        Difficulty: DifficultyLevel.Beginner,
        DurationMinutes: 420,
        Instructor: "Mr. Arjun Pillai",
        Cover: "python-problem-solving",
        ShortDescription: "Use Python to break problems into steps, work with lists and dictionaries, and write reusable functions.",
        Description: """
            Python's clear syntax makes it a popular first language for data analysis, automation and scripting. In this course you use it to practise computational thinking: understanding a problem, planning a solution and checking the result.

            You will write programs that make decisions, repeat work with loops, store data in lists and dictionaries, and organise logic into functions. Every topic is explained with short, practical examples.
            """,
        Outcomes: """
            Break a problem into clear, testable steps
            Work with numbers, strings and Boolean values
            Store and process data with lists and dictionaries
            Write functions with parameters and return values
            Handle errors with try and except
            """,
        IsPublished: true,
        Resources:
        [
            new("Thinking like a programmer", ResourceType.Article, "A four-step method for turning a problem into a program.", 8, IsPreview: true, Body: """
                ## Programs start before the code

                Experienced developers spend a surprising amount of time understanding a problem before typing. A simple four-step method works for almost any task.

                1. **Understand** – restate the problem in your own words and list the inputs and expected outputs.
                2. **Plan** – write the steps in plain language (pseudocode).
                3. **Build** – translate one step at a time into code and run it often.
                4. **Check** – test normal values, edge cases and invalid input.

                ## Example: average quiz score

                The problem: *given a list of quiz scores, print the average and whether the student passed with 60 or more.*

                ```
                scores = [72, 58, 90]
                average = sum(scores) / len(scores)
                print(f"Average: {average:.1f}")
                print("Passed" if average >= 60 else "Not yet")
                ```

                > Check the edge case: what happens when the list is empty? Dividing by `len(scores)` would fail, so a real program must handle it first.

                ## Practice

                Plan, then write, a program that counts how many scores are above the average.
                """),
            new("Python full course for beginners", ResourceType.Video,
                "A long-form freeCodeCamp tutorial. Focus on the sections about lists, functions and dictionaries.",
                60, Url: Watch("rfscVS0vtbw")),
            new("Lists, dictionaries and functions", ResourceType.Article, "The three tools you will use in almost every Python program.", 15, Body: """
                ## Lists keep items in order

                ```
                modules = ["Programming", "Databases", "Networking"]
                modules.append("Cloud Computing")
                for module in modules:
                    print(module)
                ```

                ## Dictionaries map keys to values

                ```
                student = {"name": "Aisyah", "programme": "Software Engineering", "year": 2}
                print(student["name"])
                student["year"] = 3
                ```

                Use a dictionary when you look items up by a meaningful key instead of a position.

                ## Functions package reusable logic

                ```
                def letter_grade(score):
                    if score >= 85:
                        return "A"
                    if score >= 70:
                        return "B"
                    if score >= 50:
                        return "C"
                    return "F"

                print(letter_grade(78))
                ```

                ## Handling bad input

                ```
                try:
                    hours = int(input("Hours studied: "))
                except ValueError:
                    print("Please enter a whole number.")
                ```
                """),
            new("Official Python tutorial", ResourceType.Link,
                "The Python Software Foundation's own tutorial. Chapters 3 to 5 match this course.",
                30, Url: "https://docs.python.org/3/tutorial/")
        ],
        Quizzes:
        [
            new("Python basics quiz", "Check your understanding of lists, dictionaries, strings and functions.", 60,
            [
                new("What does len([4, 8, 15]) return?",
                    "len returns the number of items in the list, which is 3.",
                    1, "2", "3", "15", "27"),
                new("Which data structure stores values under unique keys?",
                    "A dictionary maps unique keys to values, for example {\"code\": \"CT050\"}.",
                    2, "list", "tuple", "dictionary", "range"),
                new("What is printed by print(\"Learn\" + \"Hub\")?",
                    "The + operator joins two strings without adding a space.",
                    1, "Learn Hub", "LearnHub", "Learn+Hub", "An error"),
                new("Which keyword defines a named function in Python?",
                    "def starts a function definition; lambda creates small anonymous functions.",
                    1, "function", "def", "func", "lambda"),
                new("Which block handles an exception raised inside try?",
                    "Python handles exceptions in except blocks. finally always runs, and catch is not a Python keyword.",
                    2, "else", "finally", "except", "catch")
            ])
        ]);

    private static SeedCourse GitForTeams() => new(
        Title: "Git and GitHub for Team Projects",
        Category: SoftwareEngineering,
        Difficulty: DifficultyLevel.Beginner,
        DurationMinutes: 240,
        Instructor: "Mr. Arjun Pillai",
        Cover: "git-for-teams",
        ShortDescription: "Collaborate on code with branches, pull requests and a clean commit history.",
        Description: """
            Group projects go smoothly when everyone works from a single source of truth. This course shows how to use Git and GitHub as a team: committing small changes, working on feature branches, reviewing pull requests and resolving merge conflicts.

            The course is still being prepared, so it is saved as a draft and hidden from students.
            """,
        Outcomes: """
            Commit focused changes with meaningful messages
            Work on feature branches and open pull requests
            Resolve simple merge conflicts
            Keep secrets out of repositories with .gitignore
            """,
        IsPublished: false,
        Resources:
        [
            new("Why version control matters", ResourceType.Article, "What Git records and why teams rely on it.", 8, Body: """
                ## A history you can trust

                Git records snapshots of a project called **commits**. Each commit stores who changed what and why, so a team can review changes, find when a bug appeared and undo mistakes safely.

                ## A simple team workflow

                1. Pull the latest `develop` branch.
                2. Create a feature branch such as `feature/quiz-results`.
                3. Commit small, focused changes with clear messages.
                4. Push the branch and open a pull request.
                5. A teammate reviews it, then it is merged.

                > Never commit passwords, connection strings or API keys. Add them to user secrets or environment variables and list local files in `.gitignore`.
                """),
            new("Git and GitHub crash course", ResourceType.Video,
                "A freeCodeCamp crash course covering commits, branches and pull requests.",
                60, Url: Watch("RGOj5yH7evk")),
            new("Pro Git book", ResourceType.Link, "The free, official Git book. Chapters 2 and 3 cover everyday commands and branching.", 30,
                Url: "https://git-scm.com/book/en/v2")
        ],
        Quizzes: []);
}
