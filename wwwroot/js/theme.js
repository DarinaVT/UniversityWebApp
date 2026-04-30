document.addEventListener("DOMContentLoaded", function() {
    const body = document.body;
    const toggle = document.getElementById("themeToggle");
    const themeIcon = document.getElementById("themeIcon");

    function applyTheme(theme) {
        if (theme === "dark") {
            body.classList.add("dark");
            if (themeIcon) {
                themeIcon.className = "bi bi-sun-fill";
            }
        } else {
            body.classList.remove("dark");
            if (themeIcon) {
                themeIcon.className = "bi bi-moon-fill";
            }
        }
    }

    if (toggle) {
        toggle.addEventListener("click", () => {
            const isDark = body.classList.contains("dark");
            const theme = isDark ? "light" : "dark";

            document.cookie = `theme=${theme};path=/;max-age=31536000`;
            applyTheme(theme);
        });
    }

    const cookieTheme = document.cookie
        .split("; ")
        .find(x => x.startsWith("theme="))
        ?.split("=")[1];

    applyTheme(cookieTheme || "light");
});
