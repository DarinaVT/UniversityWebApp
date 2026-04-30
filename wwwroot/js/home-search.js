(function() {
    'use strict';
    
    let searchInput, dropdown, debounceTimer, selectedIndex = -1;
    
    function init() {
        searchInput = document.getElementById("heroSearchInput");
        dropdown = document.getElementById("autocompleteDropdown");
        
        if (!searchInput) {
            console.warn("Search input not found - heroSearchInput");
            return;
        }
        
        if (!dropdown) {
            console.warn("Dropdown not found - autocompleteDropdown");
            return;
        }
        
        if (dropdown.parentElement !== document.body) {
            document.body.appendChild(dropdown);
        }
        
        console.log("Search autocomplete initialized");
        setupEventListeners();
    }
    
    function setupEventListeners() {
        searchInput.addEventListener("input", handleInput);
        
        searchInput.addEventListener("focus", handleFocus);
        
        searchInput.addEventListener("keydown", handleKeyDown);
        
        document.addEventListener("click", handleDocumentClick);
        
        window.addEventListener("scroll", handleReposition, true);
        window.addEventListener("resize", handleReposition);
    }
    
    function handleInput(e) {
        const query = e.target.value.trim();
        const searchCard = document.querySelector('.hero-search-card');
        
        clearTimeout(debounceTimer);
        
        if (query.length < 2) {
            hideDropdown();
            if (searchCard) searchCard.classList.remove('active');
            return;
        }
        
        if (searchCard) searchCard.classList.add('active');
        
        debounceTimer = setTimeout(() => {
            fetchSuggestions(query);
        }, 150);
    }
    
    function handleFocus() {
        const query = searchInput.value.trim();
        if (query.length >= 2) {
            fetchSuggestions(query);
        } else if (dropdown.classList.contains("show")) {
            positionDropdown();
        }
    }
    
    function handleKeyDown(e) {
        const items = dropdown.querySelectorAll(".autocomplete-item");
        
        if (!dropdown.classList.contains("show") || items.length === 0) {
            if (e.key === "Enter") {
                return;
            }
            return;
        }
        
        switch (e.key) {
            case "ArrowDown":
                e.preventDefault();
                selectedIndex = Math.min(selectedIndex + 1, items.length - 1);
                updateSelection(items);
                break;
            case "ArrowUp":
                e.preventDefault();
                selectedIndex = Math.max(selectedIndex - 1, -1);
                updateSelection(items);
                break;
            case "Enter":
                e.preventDefault();
                if (selectedIndex >= 0 && items[selectedIndex]) {
                    items[selectedIndex].click();
                } else if (items.length > 0) {
                    items[0].click();
                }
                break;
            case "Escape":
                e.preventDefault();
                hideDropdown();
                break;
        }
    }
    
    function handleDocumentClick(e) {
        if (!searchInput.contains(e.target) && 
            !dropdown.contains(e.target) && 
            !e.target.closest('.search-button')) {
            hideDropdown();
        }
    }
    
    function handleReposition() {
        if (dropdown.classList.contains("show")) {
            positionDropdown();
        }
    }
    
    async function fetchSuggestions(query) {
        try {
            const response = await fetch(`/Home/SearchSuggestions?q=${encodeURIComponent(query)}`);
            if (!response.ok) {
                console.error("Failed to fetch suggestions:", response.status, response.statusText);
                hideDropdown();
                return;
            }
            
            const data = await response.json();
            console.log("Suggestions data:", data); 
            renderSuggestions(data);
        } catch (error) {
            console.error("Error fetching suggestions:", error);
            hideDropdown();
        }
    }
    
    function renderSuggestions(data) {
        console.log("Rendering suggestions:", data); 
        
        if (!data || (!data.universities?.length && !data.countries?.length && !data.cities?.length)) {
            console.log("No suggestions to show");
            hideDropdown();
            return;
        }
        
        dropdown.innerHTML = "";
        selectedIndex = -1;
        
        if (data.universities?.length > 0) {
            const section = createSection("Universities");
            data.universities.forEach(item => {
                const itemEl = createItem(
                    item.name,
                    `${item.city}, ${item.country}`,
                    "bi-mortarboard-fill",
                    () => {
                        searchInput.value = item.name;
                        hideDropdown();
                        searchInput.form.submit();
                    }
                );
                section.appendChild(itemEl);
            });
            dropdown.appendChild(section);
        }
        
        if (data.countries?.length > 0) {
            const section = createSection("Countries");
            data.countries.forEach(country => {
                const itemEl = createItem(
                    country,
                    null,
                    "bi-globe",
                    () => {
                        searchInput.value = country;
                        hideDropdown();
                        searchInput.form.submit();
                    }
                );
                section.appendChild(itemEl);
            });
            dropdown.appendChild(section);
        }
        
        if (data.cities?.length > 0) {
            const section = createSection("Cities");
            data.cities.forEach(city => {
                const itemEl = createItem(
                    city.name,
                    city.country,
                    "bi-geo-alt-fill",
                    () => {
                        searchInput.value = city.name;
                        hideDropdown();
                        searchInput.form.submit();
                    }
                );
                section.appendChild(itemEl);
            });
            dropdown.appendChild(section);
        }
        
        positionDropdown();
        
        dropdown.classList.add("show");
        dropdown.style.visibility = "visible";
        dropdown.style.opacity = "1";
        
        void dropdown.offsetHeight;
    }
    
    function createSection(title) {
        const section = document.createElement("div");
        section.className = "autocomplete-section";
        const header = document.createElement("div");
        header.className = "autocomplete-section-header";
        header.textContent = title;
        section.appendChild(header);
        return section;
    }
    
    function createItem(title, subtitle, icon, onClick) {
        const item = document.createElement("div");
        item.className = "autocomplete-item";
        item.innerHTML = `
            <i class="bi ${icon} me-2"></i>
            <div class="flex-grow-1">
                <div class="autocomplete-item-title">${escapeHtml(title)}</div>
                ${subtitle ? `<div class="autocomplete-item-subtitle">${escapeHtml(subtitle)}</div>` : ''}
            </div>
        `;
        item.addEventListener("click", (e) => {
            e.preventDefault();
            e.stopPropagation();
            onClick();
        });
        return item;
    }
    
    function updateSelection(items) {
        items.forEach((item, index) => {
            item.classList.toggle("selected", index === selectedIndex);
        });
        
        if (selectedIndex >= 0 && items[selectedIndex]) {
            items[selectedIndex].scrollIntoView({ block: "nearest", behavior: "smooth" });
        }
    }
    
    function positionDropdown() {
        if (!searchInput || !dropdown) return;
        
        const inputRect = searchInput.getBoundingClientRect();
        const searchWrapper = searchInput.closest('.search-input-wrapper');
        const searchCard = searchInput.closest('.hero-search-card');
        const wrapperRect = searchWrapper ? searchWrapper.getBoundingClientRect() : inputRect;
        
        dropdown.style.position = "fixed";
        
        dropdown.style.top = `${inputRect.bottom + 4}px`;
        dropdown.style.left = `${wrapperRect.left}px`;
        dropdown.style.width = `${wrapperRect.width}px`;
        dropdown.style.zIndex = "999999";
        dropdown.style.maxWidth = `${wrapperRect.width}px`;
        
        if (searchCard) {
            searchCard.style.zIndex = "1000";
        }
    }
    
    function hideDropdown() {
        if (dropdown) {
            dropdown.classList.remove("show");
            dropdown.style.visibility = "hidden";
            dropdown.style.opacity = "0";
            dropdown.innerHTML = "";
            selectedIndex = -1;
        }
    }
    
    function escapeHtml(text) {
        if (!text) return "";
        const div = document.createElement("div");
        div.textContent = text;
        return div.innerHTML;
    }
    
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
