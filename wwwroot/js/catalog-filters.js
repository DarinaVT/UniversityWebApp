let selectedCountry = null;
let selectedCity = null;


document.addEventListener("DOMContentLoaded", async () => {
    loadTopCountries();

    document.getElementById("countrySearch")
        .addEventListener("input", onCountrySearch);
});

async function loadTopCountries() {
    const res = await fetch("/Catalog/TopCountries");
    const countries = await res.json();
    renderCountryTags(countries);
}

async function onCountrySearch(e) {
    const q = e.target.value.trim();

    if (q.length === 0) {
        loadTopCountries();
        return;
    }

    const res = await fetch(`/Catalog/SearchCountries?q=${q}`);
    const countries = await res.json();
    renderCountryTags(countries);
}

function renderCountryTags(countries) {
    const container = document.getElementById("countryTags");
    container.innerHTML = "";

    countries.forEach(c => {
        const tag = document.createElement("span");
        tag.className = "badge bg-secondary country-tag";
        tag.style.cursor = "pointer";
        tag.innerText = c;

        tag.onclick = () => selectCountry(c);

        container.appendChild(tag);
    });
}

async function selectCountry(country) {
    selectedCountry = country;
    selectedCity = null;

    document.getElementById("cityFilter").classList.remove("d-none");
    document.getElementById("citySearch").value = "";

    loadTopCities();
    loadCatalog(1);
}


async function loadTopCities() {
    const res = await fetch(`/Catalog/TopCitiesByCountry?country=${selectedCountry}`);
    const cities = await res.json();
    renderCityTags(cities);
}

document.getElementById("citySearch")?.addEventListener("input", async e => {
    const q = e.target.value.trim();

    if (q.length === 0) {
        loadTopCities();
        return;
    }

    const res = await fetch(`/Catalog/SearchCities?country=${selectedCountry}&q=${q}`);
    const cities = await res.json();
    renderCityTags(cities);
});
function renderCityTags(cities) {
    const container = document.getElementById("cityTags");
    container.innerHTML = "";

    cities.forEach(c => {
        const tag = document.createElement("span");
        tag.className = "badge bg-light border text-dark";
        tag.style.cursor = "pointer";
        tag.innerText = c;

        tag.onclick = () => {
            selectedCity = c;
            loadCatalog(1);
        };

        container.appendChild(tag);
    });
}


async function loadCatalog(page = 1) {
    const params = new URLSearchParams({
        country: selectedCountry ?? "",
        city: selectedCity ?? "",
        page
    });

    const res = await fetch(`/Catalog/Index?${params}`);
    const html = await res.text();

    document.getElementById("catalogResults").innerHTML = html;
}
