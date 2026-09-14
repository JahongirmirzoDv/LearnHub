// LearnHub client-side enhancements (no dependencies).
// Every feature here is progressive enhancement: pages work without JavaScript
// because all rules are enforced again on the server.
(function () {
    "use strict";

    const onReady = (callback) => {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", callback);
        } else {
            callback();
        }
    };

    // Show UTC timestamps in the visitor's own time zone.
    function formatLocalTimes(root) {
        const dateOnly = new Intl.DateTimeFormat(undefined, { day: "numeric", month: "short", year: "numeric" });
        const dateTime = new Intl.DateTimeFormat(undefined, { day: "numeric", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
        root.querySelectorAll("time[data-local]").forEach((element) => {
            const value = new Date(element.getAttribute("datetime"));
            if (Number.isNaN(value.getTime())) {
                return;
            }
            const formatter = element.dataset.local === "datetime" ? dateTime : dateOnly;
            element.textContent = formatter.format(value);
            element.title = dateTime.format(value);
        });
    }

    // Live "x / max characters" counters for fields with a length rule.
    function initCharacterCounters() {
        document.querySelectorAll("[data-char-counter]").forEach((field) => {
            const max = Number(field.getAttribute("data-val-length-max") || field.getAttribute("maxlength"));
            if (!max) {
                return;
            }
            const counter = document.createElement("div");
            counter.className = "char-counter";
            counter.setAttribute("aria-live", "polite");
            field.insertAdjacentElement("afterend", counter);

            const update = () => {
                const length = field.value.length;
                counter.textContent = `${length} / ${max} characters`;
                counter.classList.toggle("is-near-limit", length > max * 0.9);
            };
            field.addEventListener("input", update);
            update();
        });
    }

    // Password strength feedback on registration and password change forms.
    function initPasswordMeters() {
        document.querySelectorAll("[data-password-meter]").forEach((input) => {
            const meter = document.getElementById(input.dataset.passwordMeter);
            if (!meter) {
                return;
            }
            const label = meter.nextElementSibling;
            const names = ["", "Weak", "Fair", "Good", "Strong"];

            input.addEventListener("input", () => {
                const value = input.value;
                let score = 0;
                if (value.length >= 8) score++;
                if (/[a-z]/.test(value) && /[A-Z]/.test(value)) score++;
                if (/\d/.test(value)) score++;
                if (value.length >= 12 || /[^A-Za-z0-9]/.test(value)) score++;
                if (value.length === 0) score = 0;

                meter.dataset.score = String(score);
                if (label) {
                    label.textContent = score === 0 ? "" : `Password strength: ${names[score]}`;
                }
            });
        });
    }

    // Preview a chosen image before uploading (data: URL keeps the CSP strict).
    function initImagePreviews() {
        document.querySelectorAll("input[type=file][data-image-preview]").forEach((input) => {
            const preview = document.getElementById(input.dataset.imagePreview);
            if (!preview) {
                return;
            }
            input.addEventListener("change", () => {
                const file = input.files && input.files[0];
                if (!file || !/^image\/(jpeg|png|webp)$/.test(file.type)) {
                    preview.hidden = true;
                    return;
                }
                const reader = new FileReader();
                reader.addEventListener("load", () => {
                    const image = preview.querySelector("img");
                    image.src = reader.result;
                    preview.hidden = false;
                });
                reader.readAsDataURL(file);
            });
        });
    }

    // Quiz: answered counter, warning about unanswered questions, no double submission.
    function initQuizzes() {
        document.querySelectorAll("form[data-quiz]").forEach((form) => {
            const questions = Array.from(form.querySelectorAll("fieldset[data-question]"));
            const counter = form.querySelector("[data-quiz-counter]");
            const submit = form.querySelector("button[type=submit]");

            const answeredCount = () => questions.filter((q) => q.querySelector("input:checked")).length;
            const update = () => {
                if (counter) {
                    counter.textContent = `${answeredCount()} of ${questions.length} answered`;
                }
            };

            form.addEventListener("change", update);
            form.addEventListener("submit", (event) => {
                const unanswered = questions.length - answeredCount();
                if (unanswered > 0) {
                    const noun = unanswered === 1 ? "question is" : "questions are";
                    if (!window.confirm(`${unanswered} ${noun} unanswered and will be marked incorrect. Submit anyway?`)) {
                        event.preventDefault();
                        const firstOpen = questions.find((q) => !q.querySelector("input:checked"));
                        firstOpen?.querySelector("input")?.focus();
                        return;
                    }
                }
                if (submit) {
                    submit.disabled = true;
                    submit.textContent = "Submitting…";
                }
            });
            update();
        });
    }

    // Home page sample question: instant feedback in the browser. Nothing is sent to the server or saved.
    function initSampleQuestion() {
        document.querySelectorAll("[data-sample-question]").forEach((panel) => {
            const button = panel.querySelector("[data-sample-check]");
            const feedback = panel.querySelector("[data-sample-feedback]");
            const explanation = panel.querySelector("[data-sample-explanation]");
            if (!button || !feedback) {
                return;
            }

            button.addEventListener("click", () => {
                const chosen = panel.querySelector("input[type=radio]:checked");
                panel.querySelectorAll(".quiz-option").forEach((option) => option.classList.remove("is-correct", "is-wrong"));
                if (!chosen) {
                    feedback.className = "sample-feedback mt-3 mb-0";
                    feedback.textContent = "Choose an answer first.";
                    panel.querySelector("input[type=radio]")?.focus();
                    return;
                }

                const correct = chosen.getAttribute("data-correct") === "true";
                chosen.closest(".quiz-option")?.classList.add(correct ? "is-correct" : "is-wrong");
                feedback.className = `sample-feedback mt-3 mb-0 ${correct ? "is-correct" : "is-wrong"}`;
                feedback.textContent = correct ? "Correct." : "Not quite. Try another answer.";
                if (explanation) {
                    explanation.hidden = !correct;
                }
            });
        });
    }

    // Ask before quick destructive actions that have no separate confirmation page.
    function initConfirmations() {
        document.addEventListener("submit", (event) => {
            const form = event.target;
            if (form instanceof HTMLFormElement && form.dataset.confirm && !window.confirm(form.dataset.confirm)) {
                event.preventDefault();
            }
        });
    }

    // Filter drop-downs apply immediately; the visible "Apply" button remains for keyboard and no-JS users.
    function initAutoSubmit() {
        document.querySelectorAll("select[data-auto-submit]").forEach((select) => {
            select.addEventListener("change", () => select.form?.requestSubmit());
        });
    }

    // Admin resource form: show only the fields that apply to the chosen resource type.
    function initResourceTypeFields() {
        const typeSelect = document.querySelector("select[data-resource-type]");
        if (!typeSelect) {
            return;
        }
        const sections = document.querySelectorAll("[data-types]");
        const fileInput = document.querySelector("input[type=file][data-accept-by-type]");
        const accept = { Pdf: ".pdf", Image: ".jpg,.jpeg,.png,.webp" };

        const update = () => {
            const type = typeSelect.value;
            sections.forEach((section) => {
                section.hidden = !section.dataset.types.split(" ").includes(type);
            });
            if (fileInput && accept[type]) {
                fileInput.setAttribute("accept", accept[type]);
            }
        };
        typeSelect.addEventListener("change", update);
        update();
    }

    // Admin question form: highlight the answer marked as correct.
    function initOptionEditor() {
        const editor = document.querySelector("[data-option-editor]");
        if (!editor) {
            return;
        }
        const update = () => {
            editor.querySelectorAll(".option-row").forEach((row) => {
                row.classList.toggle("is-correct", !!row.querySelector("input[type=radio]:checked"));
            });
        };
        editor.addEventListener("change", update);
        update();
    }

    onReady(() => {
        formatLocalTimes(document);
        initCharacterCounters();
        initPasswordMeters();
        initImagePreviews();
        initQuizzes();
        initSampleQuestion();
        initConfirmations();
        initAutoSubmit();
        initResourceTypeFields();
        initOptionEditor();
    });
})();
