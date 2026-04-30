let selectedCountry = null;
let selectedCity = null;
let currentSort = "rating-desc";
let currentPage = 1;

let currentSearch = null;
let currentRequest = null;

document.addEventListener("DOMContentLoaded", () => {
    const urlParams = new URLSearchParams(window.location.search);
    selectedCountry = urlParams.get("country") || null;
    selectedCity = urlParams.get("city") || null;
    currentSearch = urlParams.get("search") || null;
    currentSort = urlParams.get("sort") || "rating-desc";
    currentPage = parseInt(urlParams.get("page")) || 1;

    if (selectedCountry) {
        document.getElementById("cityFilter").style.display = "block";
        loadCitiesForCountry(selectedCountry);
    }

    const sortSelect = document.getElementById("sortSelect");
    if (sortSelect) {
        sortSelect.value = currentSort;
    }

    loadTopCountries().then(() => {
        updateActiveStates();
    });

    document.getElementById("countrySearch")?.addEventListener("input", debounce(onCountrySearch, 300));
    document.getElementById("citySearch")?.addEventListener("input", debounce(onCitySearch, 300));
    document.getElementById("sortSelect")?.addEventListener("change", onSortChange);
    document.getElementById("clearFilters")?.addEventListener("click", clearFilters);

    document.addEventListener("click", (e) => {
        const pageLink = e.target.closest(".page-link");
        if (pageLink) {
            e.preventDefault();
            e.stopPropagation();
            const page = parseInt(pageLink.dataset.page);
            if (page && page !== currentPage && page > 0) {
                currentPage = page;
                loadUniversities();
            }
        }
    });

    preloadNextPage();
});

async function loadTopCountries() {
    try {
        const res = await fetch("/Catalog/TopCountries");
        if (!res.ok) return;
        const countries = await res.json();
        renderCountryTags(countries);
    } catch (error) {
        console.error("Error loading countries:", error);
    }
}

function updateActiveStates() {
    ensureSelectedCountryInList();
    ensureSelectedCityInList();
    
    const countryTags = document.querySelectorAll("#countryTags .filter-tag");
    countryTags.forEach(tag => {
        if (tag.textContent === selectedCountry || (!selectedCountry && tag.textContent === (document.getElementById("countryTags")?.dataset.allLabel || "All"))) {
            tag.classList.add("filter-tag-active");
        } else {
            tag.classList.remove("filter-tag-active");
        }
    });
    
    const cityTags = document.querySelectorAll("#cityTags .filter-tag");
    cityTags.forEach(tag => {
        if (tag.textContent === selectedCity || (!selectedCity && tag.textContent === (document.getElementById("cityTags")?.dataset.allLabel || "All"))) {
            tag.classList.add("filter-tag-active");
        } else {
            tag.classList.remove("filter-tag-active");
        }
    });
}

async function onCountrySearch(e) {
    const q = e.target.value.trim();
    
    if (q.length === 0) {
        loadTopCountries();
        return;
    }

    try {
        const res = await fetch(`/Catalog/SearchCountries?q=${encodeURIComponent(q)}`);
        if (!res.ok) return;
        const countries = await res.json();
        renderCountryTags(countries);
        updateActiveStates();
    } catch (error) {
        console.error("Error searching countries:", error);
    }
}

function renderCountryTags(countries) {
    const container = document.getElementById("countryTags");
    if (!container) return;
    
    container.innerHTML = "";

    const allTag = document.createElement("button");
    allTag.type = "button";
    allTag.className = `filter-tag ${!selectedCountry ? "filter-tag-active" : ""}`;
    allTag.textContent = container.dataset.allLabel || "All";
    allTag.addEventListener("click", () => {
        selectedCountry = null;
        selectedCity = null;
        document.getElementById("cityFilter").style.display = "none";
        document.getElementById("citySearch").value = "";
        const countrySearch = document.getElementById("countrySearch");
        if (countrySearch) {
            countrySearch.value = "";
        }
        currentPage = 1;
        loadTopCountries();
        loadUniversities();
    });
    container.appendChild(allTag);

    countries.forEach(country => {
        const tag = document.createElement("button");
        tag.type = "button";
        tag.className = `filter-tag ${selectedCountry === country ? "filter-tag-active" : ""}`;
        tag.textContent = country;
        tag.addEventListener("click", () => {
            const countrySearch = document.getElementById("countrySearch");
            if (countrySearch) {
                countrySearch.value = "";
            }
            selectCountry(country);
        });
        container.appendChild(tag);
    });
    
    updateActiveStates();
}

async function selectCountry(country) {
    selectedCountry = country;
    selectedCity = null; 
    
    const cityFilter = document.getElementById("cityFilter");
    if (cityFilter) {
        cityFilter.style.display = "block";
    }
    
    const citySearch = document.getElementById("citySearch");
    if (citySearch) {
        citySearch.value = "";
    }
    
    const countrySearch = document.getElementById("countrySearch");
    if (countrySearch) {
        countrySearch.value = "";
    }
    
    await loadTopCountries();
    updateActiveStates();
    
    await loadCitiesForCountry(country);
    
    currentPage = 1;
    loadUniversities();
}

function ensureSelectedCountryInList() {
    if (!selectedCountry) return;
    
    const container = document.getElementById("countryTags");
    if (!container) return;
    
    const existingTag = Array.from(container.querySelectorAll(".filter-tag")).find(
        tag => tag.textContent === selectedCountry
    );
    
    if (!existingTag) {
        const tag = document.createElement("button");
        tag.type = "button";
        tag.className = "filter-tag filter-tag-active";
        tag.textContent = selectedCountry;
        tag.addEventListener("click", () => {
            const countrySearch = document.getElementById("countrySearch");
            if (countrySearch) {
                countrySearch.value = "";
            }
            selectCountry(selectedCountry);
        });
        container.insertBefore(tag, container.firstChild.nextSibling);
    }
}

function ensureSelectedCityInList() {
    if (!selectedCity) return;
    
    const container = document.getElementById("cityTags");
    if (!container) return;
    
    const existingTag = Array.from(container.querySelectorAll(".filter-tag")).find(
        tag => tag.textContent === selectedCity
    );
    
    if (!existingTag) {
        const tag = document.createElement("button");
        tag.type = "button";
        tag.className = "filter-tag filter-tag-active";
        tag.textContent = selectedCity;
        tag.addEventListener("click", () => {
            const citySearch = document.getElementById("citySearch");
            if (citySearch) {
                citySearch.value = "";
            }
            selectCity(selectedCity);
        });
        container.insertBefore(tag, container.firstChild.nextSibling);
    }
}

async function loadCitiesForCountry(country) {
    try {
        const res = await fetch(`/Catalog/TopCitiesByCountry?country=${encodeURIComponent(country)}`);
        if (!res.ok) return;
        const cities = await res.json();
        renderCityTags(cities);
        updateActiveStates();
    } catch (error) {
        console.error("Error loading cities:", error);
    }
}

async function onCitySearch(e) {
    const q = e.target.value.trim();
    
    if (!selectedCountry) return;
    
    if (q.length === 0) {
        loadCitiesForCountry(selectedCountry);
        return;
    }

    try {
        const res = await fetch(`/Catalog/SearchCities?country=${encodeURIComponent(selectedCountry)}&q=${encodeURIComponent(q)}`);
        if (!res.ok) return;
        const cities = await res.json();
        renderCityTags(cities);
        updateActiveStates();
    } catch (error) {
        console.error("Error searching cities:", error);
    }
}

function renderCityTags(cities) {
    const container = document.getElementById("cityTags");
    if (!container) return;
    
    container.innerHTML = "";

    const allTag = document.createElement("button");
    allTag.type = "button";
    allTag.className = `filter-tag ${!selectedCity ? "filter-tag-active" : ""}`;
    allTag.textContent = container.dataset.allLabel || "All";
    allTag.addEventListener("click", () => {
        selectedCity = null;
        const citySearch = document.getElementById("citySearch");
        if (citySearch) {
            citySearch.value = "";
        }
        currentPage = 1;
        if (selectedCountry) {
            loadCitiesForCountry(selectedCountry);
        }
        loadUniversities();
    });
    container.appendChild(allTag);

    cities.forEach(city => {
        const tag = document.createElement("button");
        tag.type = "button";
        tag.className = `filter-tag ${selectedCity === city ? "filter-tag-active" : ""}`;
        tag.textContent = city;
        tag.addEventListener("click", () => {
            const citySearch = document.getElementById("citySearch");
            if (citySearch) {
                citySearch.value = "";
            }
            selectCity(city);
        });
        container.appendChild(tag);
    });
    
    updateActiveStates();
}

function selectCity(city) {
    selectedCity = city;
    
    const citySearch = document.getElementById("citySearch");
    if (citySearch) {
        citySearch.value = "";
    }
    
    if (selectedCountry) {
        loadCitiesForCountry(selectedCountry);
    }
    
    currentPage = 1;
    loadUniversities();
}

function onSortChange(e) {
    currentSort = e.target.value;
    currentPage = 1; 
    loadUniversities();
}

function clearFilters() {
    selectedCountry = null;
    selectedCity = null;
    currentSort = "rating-desc";
    currentPage = 1;
    
    document.getElementById("countrySearch").value = "";
    document.getElementById("citySearch").value = "";
    document.getElementById("cityFilter").style.display = "none";
    const sortSelect = document.getElementById("sortSelect");
    if (sortSelect) {
        sortSelect.value = "rating-desc";
    }
    
    loadTopCountries();
    loadUniversities();
}

async function loadUniversities() {
    const resultsContainer = document.getElementById("catalogResults");
    if (!resultsContainer) return;
    
    const isFavoritesPage = window.location.pathname.includes("/Favorites") || 
                           document.body.dataset.page === "Favorites";
    if (isFavoritesPage) {
        window.location.href = "/Universities/Favorites";
        return;
    }
    
    if (currentRequest) {
        currentRequest.abort();
    }
    
    const previousContent = resultsContainer.innerHTML;
    resultsContainer.style.opacity = "0.5";
    resultsContainer.style.pointerEvents = "none";
    resultsContainer.innerHTML = getSkeletonLoader();
    
    const params = new URLSearchParams();
    if (selectedCountry) params.append("country", selectedCountry);
    if (selectedCity) params.append("city", selectedCity);
    if (currentSearch) params.append("search", currentSearch);
    if (currentSort) params.append("sort", currentSort);
    if (currentPage > 1) params.append("page", currentPage);
    
    const controller = new AbortController();
    currentRequest = controller;
    
    try {
        const isCatalogPage = window.location.pathname.includes("/Catalog");
        const endpoint = isCatalogPage ? "/Catalog/Index" : "/Universities/Index";
        const res = await fetch(`${endpoint}?${params.toString()}`, {
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            signal: controller.signal
        });
        
        if (!res.ok) {
            throw new Error("Failed to load universities");
        }
        
        const html = await res.text();
        
        if (currentRequest === controller) {
            resultsContainer.innerHTML = html;
            resultsContainer.style.opacity = "1";
            resultsContainer.style.pointerEvents = "auto";
            
            updateActiveStates();
            
            const scrollPosition = window.pageYOffset || document.documentElement.scrollTop;
            const containerTop = resultsContainer.getBoundingClientRect().top + scrollPosition - 20;
            window.scrollTo({ top: containerTop, behavior: "smooth" });
            
            const newUrl = window.location.pathname + (params.toString() ? `?${params.toString()}` : "");
            window.history.pushState({}, "", newUrl);
            
            preloadNextPage();
        }
        
    } catch (error) {
        if (error.name === 'AbortError') {
            return;
        }
        console.error("Error loading universities:", error);
        if (currentRequest === controller) {
            resultsContainer.innerHTML = previousContent || '<div class="alert alert-danger">Error loading universities. Please try again.</div>';
            resultsContainer.style.opacity = "1";
            resultsContainer.style.pointerEvents = "auto";
        }
    } finally {
        if (currentRequest === controller) {
            currentRequest = null;
        }
    }
}

function getSkeletonLoader() {
    return `
        <div class="mb-3">
            <div class="skeleton-text" style="height: 20px; width: 200px; background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%); background-size: 200% 100%; animation: skeleton-loading 1.5s ease-in-out infinite; border-radius: 4px;"></div>
        </div>
        <div class="row g-4 mb-4">
            ${Array.from({ length: 12 }).map(() => `
                <div class="col-md-6 col-lg-4">
                    <div class="card-white shadow-sm h-100" style="min-height: 300px;">
                        <div class="skeleton-image" style="height: 180px; background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%); background-size: 200% 100%; animation: skeleton-loading 1.5s ease-in-out infinite; border-radius: 8px 8px 0 0;"></div>
                        <div class="card-body p-3">
                            <div class="skeleton-text mb-2" style="height: 24px; width: 80%; background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%); background-size: 200% 100%; animation: skeleton-loading 1.5s ease-in-out infinite; border-radius: 4px;"></div>
                            <div class="skeleton-text mb-2" style="height: 16px; width: 60%; background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%); background-size: 200% 100%; animation: skeleton-loading 1.5s ease-in-out infinite; border-radius: 4px;"></div>
                            <div class="skeleton-text" style="height: 16px; width: 40%; background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%); background-size: 200% 100%; animation: skeleton-loading 1.5s ease-in-out infinite; border-radius: 4px;"></div>
                        </div>
                    </div>
                </div>
            `).join('')}
        </div>
    `;
}

function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

function preloadNextPage() {
    const nextPage = currentPage + 1;
    const params = new URLSearchParams();
    if (selectedCountry) params.append("country", selectedCountry);
    if (selectedCity) params.append("city", selectedCity);
    if (currentSearch) params.append("search", currentSearch);
    if (currentSort) params.append("sort", currentSort);
    params.append("page", nextPage);
    
    const isCatalogPage = window.location.pathname.includes("/Catalog");
    const endpoint = isCatalogPage ? "/Catalog/Index" : "/Universities/Index";
    
    fetch(`${endpoint}?${params.toString()}`, {
        headers: {
            "X-Requested-With": "XMLHttpRequest"
        }
    }).catch(() => {});
}

