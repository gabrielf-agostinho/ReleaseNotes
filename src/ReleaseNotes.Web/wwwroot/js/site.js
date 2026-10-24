// Progressive enhancements only: every page works without this file.
(() => {
    "use strict";

    // Show a loading state while a filter request is in flight.
    document.querySelectorAll("[data-busy-form]").forEach((form) => {
        form.addEventListener("submit", () => {
            const target = document.querySelector("[data-busy-target]");
            target?.setAttribute("aria-busy", "true");

            // Drop empty fields so shared URLs stay short: /changelog/?product=lumen
            form.querySelectorAll("select, input").forEach((field) => {
                if (!field.value || (field.type === "checkbox" && !field.checked)) {
                    field.disabled = true;
                }
            });
        });
    });

    // Restore the page when it comes back from the back/forward cache.
    window.addEventListener("pageshow", () => {
        document.querySelectorAll("[aria-busy='true']").forEach((element) => element.removeAttribute("aria-busy"));
        document.querySelectorAll("[data-busy-form] [disabled]").forEach((field) => { field.disabled = false; });
    });

    // "Copy link" buttons are hidden in the markup and revealed when the Clipboard API is available.
    if (navigator.clipboard) {
        document.querySelectorAll("[data-copy-url]").forEach((button) => {
            const label = button.querySelector("[data-copy-label]");
            const original = label?.textContent;
            button.hidden = false;
            button.addEventListener("click", async () => {
                try {
                    await navigator.clipboard.writeText(window.location.href.split("#")[0]);
                    button.setAttribute("data-copied", "");
                    if (label) label.textContent = "Link copied";
                } catch {
                    if (label) label.textContent = "Copy failed";
                }
                window.setTimeout(() => {
                    button.removeAttribute("data-copied");
                    if (label) label.textContent = original;
                }, 2000);
            });
        });
    }
})();
