document.addEventListener("DOMContentLoaded", () => {
    if (typeof Chart === 'undefined') {
        console.error("Chart.js library not loaded");
        return;
    }
    
    if (typeof L === 'undefined') {
        console.error("Leaflet library not loaded");
        return;
    }
    
    initCircleCharts();
    
    setTimeout(() => {
        initAdminMap();
    }, 100);
});

function initCircleCharts() {
    const data = window.viewsData || { today: 0, week: 0, month: 0, total: 1 };
    const todayPercent = data.total > 0 ? (data.today / data.total) * 100 : 0;
    const weekPercent = data.total > 0 ? (data.week / data.total) * 100 : 0;
    const monthPercent = data.total > 0 ? (data.month / data.total) * 100 : 0;

    const todayCtx = document.getElementById('viewsTodayChart');
    if (todayCtx) {
        new Chart(todayCtx, {
            type: 'doughnut',
            data: {
                datasets: [{
                    data: [todayPercent, 100 - todayPercent],
                    backgroundColor: ['#2563EB', '#E5E7EB'],
                    borderWidth: 0
                }]
            },
            options: {
                cutout: '75%',
                plugins: {
                    legend: { display: false },
                    tooltip: { enabled: false }
                },
                maintainAspectRatio: false
            }
        });
    }

    const weekCtx = document.getElementById('viewsWeekChart');
    if (weekCtx) {
        new Chart(weekCtx, {
            type: 'doughnut',
            data: {
                datasets: [{
                    data: [weekPercent, 100 - weekPercent],
                    backgroundColor: ['#10B981', '#E5E7EB'],
                    borderWidth: 0
                }]
            },
            options: {
                cutout: '75%',
                plugins: {
                    legend: { display: false },
                    tooltip: { enabled: false }
                },
                maintainAspectRatio: false
            }
        });
    }

    const monthCtx = document.getElementById('viewsMonthChart');
    if (monthCtx) {
        new Chart(monthCtx, {
            type: 'doughnut',
            data: {
                datasets: [{
                    data: [monthPercent, 100 - monthPercent],
                    backgroundColor: ['#7C3AED', '#E5E7EB'],
                    borderWidth: 0
                }]
            },
            options: {
                cutout: '75%',
                plugins: {
                    legend: { display: false },
                    tooltip: { enabled: false }
                },
                maintainAspectRatio: false
            }
        });
    }
}

let adminMap;
let adminMarkersLayer;

async function initAdminMap() {
    const mapElement = document.getElementById('adminWorldMap');
    if (!mapElement) {
        console.log("Map element not found");
        return;
    }

    if (typeof L === 'undefined') {
        console.error("Leaflet library not loaded");
        return;
    }

    try {
        const rect = mapElement.getBoundingClientRect();
        if (rect.width === 0 || rect.height === 0) {
            console.warn("Map container has no dimensions, waiting...");
            setTimeout(() => initAdminMap(), 200);
            return;
        }

        console.log("Initializing admin map...");
        adminMap = L.map('adminWorldMap').setView([20, 0], 2);

        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            attribution: "&copy; OpenStreetMap contributors",
            maxZoom: 19
        }).addTo(adminMap);

        adminMarkersLayer = L.layerGroup().addTo(adminMap);

        try {
            const res = await fetch("/data/world-countries.geo.json");
            if (res.ok) {
                const geoData = await res.json();
                
                L.geoJSON(geoData, {
                    style: {
                        color: "#777",
                        weight: 1,
                        fillColor: "#d3d3d3",
                        fillOpacity: 0.3
                    }
                }).addTo(adminMap);
            } else {
                console.log("GeoJSON not found, continuing without country boundaries");
            }
        } catch (geoError) {
            console.log("Could not load GeoJSON, continuing without country boundaries");
        }
        
        await loadAdminUniversities();
        
        console.log("Admin map initialized successfully");
    } catch (error) {
        console.error("Error initializing map:", error);
    }
}

async function loadAdminUniversities() {
    if (!adminMarkersLayer) return;

    try {
        const res = await fetch("/AdminDashboard/GetAllUniversitiesForMap");
        if (!res.ok) {
            console.error("Failed to load universities");
            return;
        }

        const universities = await res.json();

        universities.forEach(u => {
            if (!u.latitude || !u.longitude) return;

            const marker = L.marker([u.latitude, u.longitude])
                .bindPopup(`
                    <strong>${u.name}</strong><br/>
                    ⭐ Rating: ${u.rating || 'N/A'}<br/>
                    <a href="/University/Details/${u.id}" target="_blank">View details</a>
                `);

            adminMarkersLayer.addLayer(marker);
        });
    } catch (error) {
        console.error("Error loading universities:", error);
    }
}

