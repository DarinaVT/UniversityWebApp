let map;
let geoJsonLayer;

const markersByCountry = {};

let activeCountryLayer = null;
let activeCountryName = null;
let activeCountryColor = null;

const DEFAULT_FILL = "#d3d3d3";

const RANDOM_COLORS = [
    "#4caf50", "#2196f3", "#ff9800", "#e91e63", "#9c27b0",
    "#00bcd4", "#ff5722", "#ffc107", "#8bc34a", "#673ab7",
    "#03a9f4", "#cddc39", "#ff6f00", "#d32f2f", "#7b1fa2",
    "#009688", "#795548", "#607d8b", "#f44336", "#3f51b5"
];

const universityCache = {};

function getRandomColor() {
    return RANDOM_COLORS[Math.floor(Math.random() * RANDOM_COLORS.length)];
}

async function prefetchUniversityData(countryName) {
    if (universityCache[countryName]) {
        return universityCache[countryName];
    }
    
    try {
        const response = await fetch(`/Home/TopUniversitiesByCountry?country=${encodeURIComponent(countryName)}`);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const data = await response.json();
        universityCache[countryName] = data;
        return data;
    } catch (err) {
        console.error("Error prefetching data for", countryName, ":", err);
        return [];
    }
}

function loadUniversityPins(countryName) {
    if (markersByCountry[countryName]) {
        return;
    }

    const group = L.layerGroup().addTo(map);
    markersByCountry[countryName] = group;

    if (universityCache[countryName]) {
        const pins = universityCache[countryName];
        addMarkersToMap(pins, group, countryName);
    } else {
        fetch(`/Home/TopUniversitiesByCountry?country=${encodeURIComponent(countryName)}`)
            .then(r => {
                if (!r.ok) {
                    console.error("Failed to fetch pins for", countryName, "Status:", r.status);
                    throw new Error(`HTTP error! status: ${r.status}`);
                }
                return r.json();
            })
            .then(pins => {
                console.log("Loaded pins for", countryName, ":", pins.length);
                universityCache[countryName] = pins;
                addMarkersToMap(pins, group, countryName);
            })
            .catch(err => {
                console.error("Pin load error for", countryName, ":", err);
            });
    }
}

function addMarkersToMap(pins, group, countryName) {
    if (!pins || pins.length === 0) {
        console.log("No pins to add for", countryName);
        return;
    }

    const mapElement = document.getElementById("worldMap");
    const ratingLabel = mapElement ? mapElement.getAttribute("data-rating-label") || "Rating" : "Rating";

    pins.forEach(p => {
        if (!p.latitude || !p.longitude) {
            console.warn("Pin missing coordinates:", p);
            return;
        }

        const lat = parseFloat(p.latitude);
        const lng = parseFloat(p.longitude);

        if (isNaN(lat) || isNaN(lng)) {
            console.warn("Invalid coordinates for pin:", p);
            return;
        }

        if (lat < -90 || lat > 90 || lng < -180 || lng > 180) {
            console.warn("Coordinates out of range for pin:", p);
            return;
        }

        if (Math.abs(lat) > 90) {
            console.warn("Coordinates appear to be swapped for pin:", p, "Skipping...");
            return;
        }

        const marker = L.marker([lat, lng], { 
            title: p.name,
            opacity: 0
        });

        marker.bindPopup(
            `<strong>${p.name}</strong><br>
             ${p.country || ""}<br>
             ${p.rating ? `${ratingLabel}: ${p.rating}` : `${ratingLabel}: N/A`}`
        );

        group.addLayer(marker);

        requestAnimationFrame(() => {
            marker.setOpacity(1);
        });
    });
    
    console.log("Added", pins.length, "markers for", countryName);
}

function removeCountryPins(countryName) {
    const group = markersByCountry[countryName];
    if (!group) return;

    map.removeLayer(group);
    delete markersByCountry[countryName];
}

function initMap() {
    const mapElement = document.getElementById("worldMap");
    if (!mapElement) {
        console.error("Map element not found");
        return false;
    }

    if (typeof L === 'undefined') {
        console.error("Leaflet library not loaded");
        return false;
    }

    const rect = mapElement.getBoundingClientRect();
    if (rect.width === 0 || rect.height === 0) {
        return false;
    }

    try {
        if (map) {
            map.remove();
        }

        map = L.map("worldMap", {
            preferCanvas: false,
            zoomAnimation: true,
            fadeAnimation: true,
            markerZoomAnimation: true,
            zoomSnap: 1,
            zoomDelta: 1,
            wheelPxPerZoomLevel: 80,
            inertia: false
        }).setView([20, 0], 2);

        const tileLayer = L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            attribution: "&copy; OpenStreetMap contributors",
            maxZoom: 18,
            minZoom: 2,
            updateWhenIdle: true,
            updateWhenZooming: false,
            keepBuffer: 2,
            crossOrigin: true
        });

        tileLayer.addTo(map);

        tileLayer.on("tileerror", function (e) {
            console.error("Tile load error:", e);
        });

        setTimeout(() => {
            map.invalidateSize();
        }, 100);

        return true;
    } catch (error) {
        console.error("Error initializing map:", error);
        return false;
    }
}

async function loadMapData() {
    if (!map) return;

    try {
        const res = await fetch("/data/world-countries.geo.json");
        if (!res.ok) {
            console.error("GeoJSON not found");
            return;
        }

        const geoData = await res.json();

        geoJsonLayer = L.geoJSON(geoData, {
            style: (feature) => ({
                color: "#666",
                weight: 1,
                fillColor: DEFAULT_FILL,
                fillOpacity: 0.5,
                smoothFactor: 2.0
            }),
            onEachFeature: (feature, layer) => {
                const countryName = feature.properties.name || feature.properties.ADMIN;
                
                if (!countryName) {
                    return;
                }

                layer.on("mouseover", function(e) {
                    prefetchUniversityData(countryName);
                    
                    if (this !== activeCountryLayer) {
                        this.setStyle({
                            fillOpacity: 0.85,
                            weight: 2
                        });
                    }
                });

                layer.on("mouseout", function() {
                    if (this !== activeCountryLayer) {
                        this.setStyle({
                            fillOpacity: 0.5,
                            weight: 1
                        });
                    }
                });

                layer.on("click", (e) => {
                    L.DomEvent.stopPropagation(e);

                    if (activeCountryLayer === layer) {
                        return;
                    }

                    if (activeCountryLayer) {
                        activeCountryLayer.setStyle({ 
                            fillColor: DEFAULT_FILL,
                            fillOpacity: 0.5,
                            weight: 1
                        });
                        removeCountryPins(activeCountryName);
                    }

                    activeCountryLayer = layer;
                    activeCountryName = countryName;
                    activeCountryColor = getRandomColor();
                    
                    layer.setStyle({ 
                        fillColor: activeCountryColor,
                        fillOpacity: 0.8,
                        weight: 2
                    });

                    console.log("Loading pins for country:", countryName);
                    loadUniversityPins(countryName);
                });
            }
        }).addTo(map);

    } catch (error) {
        console.error("Error loading map data:", error);
    }
}

document.addEventListener("DOMContentLoaded", async () => {
    let retries = 0;
    const maxRetries = 10;

    const tryInit = () => {
        if (initMap()) {
            loadMapData();
        } else if (retries < maxRetries) {
            retries++;
            setTimeout(tryInit, 200);
        } else {
            console.error("Failed to initialize map after", maxRetries, "attempts");
        }
    };

    tryInit();
});
