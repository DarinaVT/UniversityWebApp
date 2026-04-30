document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-language]").forEach(button => {
        button.addEventListener("click", async (e) => {
            e.preventDefault();
            e.stopPropagation();
            
            const dropdown = button.closest('.dropdown-menu');
            if (dropdown) {
                dropdown.classList.remove('show');
                const toggle = dropdown.previousElementSibling || document.querySelector(`[aria-labelledby="${dropdown.getAttribute('aria-labelledby')}"]`);
                if (toggle) {
                    toggle.setAttribute('aria-expanded', 'false');
                }
            }
            
            const culture = button.dataset.language;
            
            if (button.disabled) return;
            button.disabled = true;
            
            await setLanguage(culture);
            
            setTimeout(() => {
                button.disabled = false;
            }, 1000);
        });
    });
});

async function setLanguage(culture) {
    try {
        const buttons = document.querySelectorAll("[data-language]");
        buttons.forEach(btn => {
            btn.style.opacity = "0.5";
            btn.style.pointerEvents = "none";
        });

        const response = await fetch("/Localization/Set", {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
                "X-Requested-With": "XMLHttpRequest"
            },
            body: `culture=${culture}`
        });

        if (!response.ok) {
            throw new Error("Failed to set language");
        }

        const result = await response.json();
        if (result.success) {
            const currentUrl = window.location.pathname + window.location.search;
            
            const pageResponse = await fetch(currentUrl, {
                headers: {
                    "X-Requested-With": "XMLHttpRequest",
                    "Accept": "text/html"
                },
                credentials: "same-origin"
            });
            
            if (pageResponse.ok) {
                const html = await pageResponse.text();
                const parser = new DOMParser();
                const doc = parser.parseFromString(html, "text/html");
                
                const newMain = doc.querySelector("main") || doc.querySelector(".container");
                const currentMain = document.querySelector("main") || document.querySelector(".container");
                
                if (newMain && currentMain) {
                    currentMain.innerHTML = newMain.innerHTML;
                }
                
                const newTitle = doc.querySelector("title");
                if (newTitle) {
                    document.title = newTitle.textContent;
                }
                
                const newNav = doc.querySelector("nav");
                const currentNav = document.querySelector("nav");
                if (newNav && currentNav) {
                    const oldNavHTML = currentNav.innerHTML;
                    currentNav.innerHTML = newNav.innerHTML;
                    
                    document.querySelectorAll("[data-language]").forEach(button => {
                        button.addEventListener("click", async (e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            
                            const dropdown = button.closest('.dropdown-menu');
                            if (dropdown) {
                                dropdown.classList.remove('show');
                                const toggle = dropdown.previousElementSibling || document.querySelector(`[aria-labelledby="${dropdown.getAttribute('aria-labelledby')}"]`);
                                if (toggle) {
                                    toggle.setAttribute('aria-expanded', 'false');
                                }
                            }
                            
                            const btnCulture = button.dataset.language;
                            
                            if (button.disabled) return;
                            button.disabled = true;
                            
                            await setLanguage(btnCulture);
                            
                            setTimeout(() => {
                                button.disabled = false;
                            }, 1000);
                        });
                    });
                }
                
                buttons.forEach(btn => {
                    btn.style.opacity = "1";
                    btn.style.pointerEvents = "auto";
                });
                
                const activeButtons = document.querySelectorAll(`[data-language="${culture}"]`);
                activeButtons.forEach(btn => {
                    btn.classList.add("active");
                });
                
                const inactiveButtons = document.querySelectorAll(`[data-language]:not([data-language="${culture}"])`);
                inactiveButtons.forEach(btn => {
                    btn.classList.remove("active");
                });
                
                const event = new Event("DOMContentLoaded");
                document.dispatchEvent(event);
                
                if (typeof window.initPageScripts === "function") {
                    window.initPageScripts();
                }
            } else {
                window.location.reload();
            }
        } else {
            window.location.reload();
        }
        
    } catch (error) {
        console.error("Error setting language:", error);
        window.location.reload();
    }
}

