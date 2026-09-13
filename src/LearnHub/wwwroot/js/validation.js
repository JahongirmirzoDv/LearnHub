// Client-side counterparts of LearnHub's custom server validation attributes
// (MustBeTrueAttribute, MaxFileSizeAttribute, AllowedExtensionsAttribute) plus
// accessibility wiring for ASP.NET Core's unobtrusive validation messages.
// Loaded after jquery.validate.unobtrusive.js, before the document-ready parse runs.
(function ($) {
    "use strict";

    if (!$ || !$.validator || !$.validator.unobtrusive) {
        return;
    }

    $.validator.addMethod("mustbetrue", function (value, element) {
        return element.checked === true;
    });
    $.validator.unobtrusive.adapters.addBool("mustbetrue");

    $.validator.addMethod("filesize", function (value, element, maxBytes) {
        const files = element.files;
        return this.optional(element) || !files || files.length === 0 || files[0].size <= Number(maxBytes);
    });
    $.validator.unobtrusive.adapters.addSingleVal("filesize", "max");

    $.validator.addMethod("fileext", function (value, element, extensions) {
        if (this.optional(element)) {
            return true;
        }
        const allowed = String(extensions).toLowerCase().split(",");
        const name = String(value).toLowerCase();
        return allowed.some(function (extension) { return name.endsWith(extension.trim()); });
    });
    $.validator.unobtrusive.adapters.addSingleVal("fileext", "extensions");

    // Validate while typing once a field has been touched, and link each message to its field.
    $.validator.setDefaults({
        onkeyup: function (element) {
            if (element.name in this.submitted || element.classList.contains("input-validation-error")) {
                this.element(element);
            }
        }
    });

    $(function () {
        $("[data-valmsg-for]").each(function () {
            const fieldName = this.getAttribute("data-valmsg-for");
            const messageId = (fieldName.replace(/[^A-Za-z0-9_-]/g, "_") + "-error");
            this.id = this.id || messageId;
            this.setAttribute("aria-live", "polite");

            const field = document.querySelector("[name=\"" + CSS.escape(fieldName) + "\"]");
            if (field) {
                const describedBy = (field.getAttribute("aria-describedby") || "").split(" ").filter(Boolean);
                if (describedBy.indexOf(this.id) === -1) {
                    describedBy.push(this.id);
                    field.setAttribute("aria-describedby", describedBy.join(" "));
                }
                if (this.classList.contains("field-validation-error")) {
                    field.setAttribute("aria-invalid", "true");
                }
            }
        });

        // Move focus to the summary or first invalid field after a failed client-side submit.
        $("form").on("invalid-form.validate", function (event, validator) {
            const first = validator.errorList.length > 0 ? validator.errorList[0].element : null;
            if (first) {
                window.setTimeout(function () { first.focus(); }, 0);
            }
        });
    });
})(window.jQuery);
