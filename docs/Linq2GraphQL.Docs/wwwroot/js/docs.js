window.l2gDocs = (() => {
    const themeKey = "l2g-theme";
    let updateScheduled = false;

    function getTheme() {
        return document.documentElement.getAttribute("data-bs-theme") === "dark" ? "dark" : "light";
    }

    function setTheme(theme) {
        document.documentElement.setAttribute("data-bs-theme", theme);
        try {
            localStorage.setItem(themeKey, theme);
        } catch {
        }
    }

    function readHeadings() {
        return Array.from(document.querySelectorAll(".doc-content h2[id]"))
            .map(heading => ({ id: heading.id, text: headingText(heading) }));
    }

    function headingText(heading) {
        const copy = heading.cloneNode(true);
        copy.querySelectorAll(".step-number, .badge").forEach(element => element.remove());
        return copy.textContent.trim();
    }

    function markActiveHeading(id) {
        document.querySelectorAll(".docs-toc a[data-heading]").forEach(link => {
            link.classList.toggle("active", link.dataset.heading === id);
        });
    }

    function updateActiveHeading() {
        const headings = Array.from(document.querySelectorAll(".doc-content h2[id]"));
        if (headings.length === 0) {
            return;
        }

        const atBottom = window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 2;
        const passed = headings.filter(heading => heading.getBoundingClientRect().top <= 120);
        const current = atBottom ? headings[headings.length - 1] : passed[passed.length - 1] ?? headings[0];
        markActiveHeading(current.id);
    }

    function onScroll() {
        if (!updateScheduled) {
            updateScheduled = true;
            requestAnimationFrame(() => {
                updateScheduled = false;
                updateActiveHeading();
            });
        }
    }

    function observeHeadings() {
        window.removeEventListener("scroll", onScroll);
        window.addEventListener("scroll", onScroll, { passive: true });
        updateActiveHeading();
    }

    function scrollToHeading(id) {
        const heading = document.getElementById(id);
        if (heading) {
            heading.scrollIntoView({ behavior: "smooth", block: "start" });
            history.replaceState(null, "", `${location.pathname}#${id}`);
            markActiveHeading(id);
        }
    }

    function scrollToTop() {
        window.scrollTo({ top: 0 });
    }

    return { getTheme, setTheme, readHeadings, observeHeadings, scrollToHeading, scrollToTop };
})();
