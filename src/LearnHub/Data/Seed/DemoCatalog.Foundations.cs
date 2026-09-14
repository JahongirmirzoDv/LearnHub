using LearnHub.Models;

namespace LearnHub.Data.Seed;

internal static partial class DemoCatalog
{
    private static SeedCourse DiscreteMathematics() => new(
        Title: "Discrete Mathematics for Computing",
        Category: Mathematics,
        Difficulty: DifficultyLevel.Beginner,
        DurationMinutes: 360,
        Instructor: "Dr. Nadia Rahim",
        Cover: "discrete-maths",
        ShortDescription: "Number bases, logic and sets: the maths behind every program and database query.",
        Description: """
            Computers store everything as binary numbers and make every decision with logic. This course teaches the discrete mathematics that programmers use daily, with examples taken from code and databases rather than abstract proofs.

            You will convert between binary, decimal and hexadecimal, evaluate logical expressions with truth tables, and use set operations to reason about queries. Short exercises let you practise each idea and check your answer.
            """,
        Outcomes: """
            Convert numbers between binary, decimal and hexadecimal
            Evaluate AND, OR and NOT expressions with truth tables
            Apply De Morgan's laws to simplify conditions in code
            Use union, intersection and difference to describe query results
            """,
        IsPublished: true,
        Resources:
        [
            new("Number bases: binary, decimal and hexadecimal", ResourceType.Article, "Why computers count in twos and how to convert between bases.", 12, IsPreview: true, Body: """
                ## Place value in any base

                In decimal (base 10) each position is worth ten times the one to its right: 345 = 3×100 + 4×10 + 5×1.

                Binary (base 2) works the same way with powers of two. Each digit is a **bit**.

                ```
                Position value:  128  64  32  16   8   4   2   1
                Binary digits:     0   1   1   0   1   0   0   1
                ```

                01101001 in binary = 64 + 32 + 8 + 1 = **105** in decimal.

                ## Decimal to binary

                Repeatedly divide by 2 and write down the remainders, then read them from bottom to top.

                - 13 ÷ 2 = 6 remainder **1**
                - 6 ÷ 2 = 3 remainder **0**
                - 3 ÷ 2 = 1 remainder **1**
                - 1 ÷ 2 = 0 remainder **1**

                Reading upwards gives 13 = **1101** in binary.

                ## Hexadecimal

                Hexadecimal (base 16) uses the digits 0–9 and A–F, where A = 10 and F = 15. One hex digit represents exactly four bits, which is why colours such as `#1B2438` and memory addresses are written in hex.

                > 1111 in binary = F in hex = 15 in decimal.
                """),
            new("Maths for programmers: sets and logic", ResourceType.Video,
                "freeCodeCamp's course on the sets and logic used in programming. The first 40 minutes match this course.",
                45, Url: Watch("2SpuBqvNjHI")),
            new("Exercise: convert between bases", ResourceType.Exercise, "Practise binary, decimal and hexadecimal conversions.", 10,
                Body: """
                    ## Your task

                    Work these out on paper before revealing the solution:

                    1. Convert **45** from decimal to binary.
                    2. Convert **10110110** from binary to decimal.
                    3. Convert **10110110** from binary to hexadecimal.
                    4. An 8-bit unsigned number can hold values from 0 up to what maximum?
                    """,
                Solution: """
                    1. 45 = 32 + 8 + 4 + 1, so the binary is **101101**.
                    2. 128 + 32 + 16 + 4 + 2 = **182**.
                    3. Split into groups of four bits: 1011 = B and 0110 = 6, so the hex is **B6**.
                    4. All eight bits set is 11111111 = 128 + 64 + 32 + 16 + 8 + 4 + 2 + 1 = **255**, so the range is 0–255.
                    """),
            new("Logic, truth tables and De Morgan's laws", ResourceType.Article, "Evaluate and simplify the conditions you write in if statements.", 12, Body: """
                ## The three basic operators

                - **AND** (`&&`) is true only when both sides are true.
                - **OR** (`||`) is true when at least one side is true.
                - **NOT** (`!`) flips true and false.

                ## Truth tables

                A truth table lists every combination of inputs.

                ```
                A      B      A AND B   A OR B
                false  false  false     false
                false  true   false     true
                true   false  false     true
                true   true   true      true
                ```

                ## De Morgan's laws

                - NOT (A AND B) = (NOT A) OR (NOT B)
                - NOT (A OR B) = (NOT A) AND (NOT B)

                They make negated conditions easier to read:

                ```
                if (!(isEnrolled && hasPassed))   // hard to read
                if (!isEnrolled || !hasPassed)    // same meaning
                ```

                ## Sets and queries

                A **set** is a collection of distinct items. Database queries are set operations: `UNION` combines results, `INTERSECT` keeps rows found in both, and `EXCEPT` removes rows found in the second result.
                """),
            new("Exercise: simplify a condition", ResourceType.Exercise, "Use a truth table and De Morgan's laws on real code.", 10,
                Body: """
                    ## Your task

                    A quiz page shows a warning with this condition:

                    ```
                    if (!(score >= passMark || attempts < 3))
                    ```

                    1. Rewrite the condition without the outer `!` using De Morgan's laws.
                    2. Describe in plain English when the warning appears.
                    """,
                Solution: """
                    1. NOT (A OR B) = (NOT A) AND (NOT B), so the condition becomes:

                    ```
                    if (score < passMark && attempts >= 3)
                    ```

                    2. The warning appears when the student has **not** reached the pass mark **and** has already used three or more attempts.
                    """),
            new("MIT OpenCourseWare: Mathematics for Computer Science", ResourceType.Link,
                "Free lecture notes, videos and problem sets from MIT's course, for students who want to go further.",
                20, Url: "https://ocw.mit.edu/courses/6-042j-mathematics-for-computer-science-fall-2010/")
        ],
        Quizzes:
        [
            new("Discrete maths checkpoint", "Number bases, logic and sets. Harder questions are worth two points.", 60,
            [
                new("What is the binary number 1010 in decimal?",
                    "1010 = 8 + 2 = 10.",
                    2, "5", "8", "10", "12"),
                new("What is the largest value an 8-bit unsigned number can store?",
                    "Eight bits all set to 1 is 128 + 64 + 32 + 16 + 8 + 4 + 2 + 1 = 255.",
                    1, "128", "255", "256", "1024"),
                new("Which hexadecimal digit represents the binary group 1111?",
                    "1111 = 8 + 4 + 2 + 1 = 15, and 15 is written as F in hexadecimal.",
                    3, "A", "E", "1", "F"),
                new("By De Morgan's laws, NOT (A AND B) is equivalent to…",
                    "NOT (A AND B) = (NOT A) OR (NOT B).",
                    0, "(NOT A) OR (NOT B)", "(NOT A) AND (NOT B)", "A OR B", "NOT A AND B") { Points = 2 },
                new("Students taking both Programming and Mathematics are described by which set operation?",
                    "The intersection contains only the items that belong to both sets.",
                    2, "Union", "Difference", "Intersection", "Complement") { Points = 2 }
            ])
        ]);

    private static SeedCourse StudySkills() => new(
        Title: "Effective Online Study Skills",
        Category: Other,
        Difficulty: DifficultyLevel.Beginner,
        DurationMinutes: 120,
        Instructor: "Ms. Grace Lim",
        Cover: "study-skills",
        ShortDescription: "Evidence-based habits for learning online: active recall, spaced practice and planning.",
        Description: """
            Online courses give you freedom, and freedom makes it easy to fall behind. This short course introduces study techniques backed by research on memory, and shows how to use them with the lessons, exercises and quizzes on LearnHub.

            You will replace re-reading with active recall, spread practice over time, and build a weekly plan you can actually keep.
            """,
        Outcomes: """
            Use active recall instead of re-reading notes
            Plan spaced practice sessions across a week
            Break a course into small, trackable goals
            """,
        IsPublished: true,
        Resources:
        [
            new("How memory works when you study", ResourceType.Article, "Active recall and spaced practice, and why re-reading feels better than it works.", 10, IsPreview: true, Body: """
                ## Re-reading feels productive, but is not

                Reading your notes again makes the material feel familiar, and familiarity is easily mistaken for understanding. Research on memory repeatedly finds two habits that work better.

                ## Active recall

                Close the lesson and try to explain it from memory, or answer questions about it. Each successful retrieval makes the memory stronger. Quizzes are not only a test: taking one is itself a way to learn.

                ## Spaced practice

                Reviewing a topic a day later, then a few days later, then a week later beats one long session. Each gap lets you forget a little, and recalling it again strengthens it.

                ## Put it into practice on LearnHub

                - Try each exercise before revealing its solution.
                - Take the course quiz, read the explanations, and retake it a few days later.
                - Use the Progress page to see which courses need your next session.
                """),
            new("Exercise: plan your study week", ResourceType.Exercise, "Turn one course into a realistic weekly plan.", 15,
                Body: """
                    ## Your task

                    Pick one course you are enrolled in and write a plan for the next seven days:

                    1. Choose three short sessions of 25–40 minutes on different days.
                    2. For each session, name the lesson you will study **and** the recall activity you will do at the end (for example: explain the lesson aloud, or answer the quiz questions from memory).
                    3. Leave at least one day between sessions on the same topic.
                    """,
                Solution: """
                    A good plan could look like this:

                    - **Monday (30 min)** – read "Number bases"; finish by converting three numbers without looking at the lesson.
                    - **Wednesday (30 min)** – do the base conversion exercise; then write the division method from memory.
                    - **Saturday (40 min)** – read "Logic and truth tables", then take the checkpoint quiz and review every explanation.

                    What makes it work: sessions are short and specific, each ends with recall rather than re-reading, and the gaps between them give spaced practice.
                    """),
            new("Learning How to Learn (Coursera)", ResourceType.Link,
                "A popular free course by Barbara Oakley and Terrence Sejnowski on the science of learning.",
                20, Url: "https://www.coursera.org/learn/learning-how-to-learn")
        ],
        Quizzes:
        [
            new("Study skills check", "Four questions on active recall, spacing and planning.", 75,
            [
                new("Which activity is an example of active recall?",
                    "Active recall means retrieving information from memory, such as answering questions without looking at the notes.",
                    2, "Highlighting the lesson", "Re-reading your notes twice", "Answering questions from memory", "Copying the lesson word for word"),
                new("What is spaced practice?",
                    "Spaced practice spreads reviews of the same topic over several days instead of one long session.",
                    0, "Reviewing a topic in several sessions spread over time", "Studying for as long as possible in one sitting", "Leaving a blank line between notes", "Studying only the night before an exam"),
                new("Why can re-reading be misleading?",
                    "Familiar material feels understood even when you could not yet recall or apply it.",
                    1, "It takes too little time", "Familiarity feels like understanding", "It uses too much paper", "It always causes forgetting"),
                new("When should you reveal an exercise solution?",
                    "Attempting the task first is what builds memory; the solution is for checking and correcting your own work.",
                    3, "Before reading the task", "Immediately, to save time", "Never", "After making your own attempt") { Points = 2 }
            ])
        ]);
}
