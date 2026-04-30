document.addEventListener("DOMContentLoaded", async () => {
    const canvas = document.getElementById("homeCountryStats");
    if (!canvas) {
        return
    }
    const res = await fetch("/Home/TopCountriesStats");
    if (!res.ok) {
        return
    }
    const data = await res.json();

    const labels = data.map(x => x.country);
    const values = data.map(x => x.universitiesCount);

    const isDark = document.body.classList.contains('dark');
    
    const lightThemeColors = [
        'rgba(79, 70, 229, 0.85)',
        'rgba(124, 58, 237, 0.85)',
        'rgba(99, 102, 241, 0.85)',
        'rgba(129, 140, 248, 0.85)',
        'rgba(79, 70, 229, 0.75)',
        'rgba(124, 58, 237, 0.75)',
        'rgba(99, 102, 241, 0.75)',
        'rgba(129, 140, 248, 0.75)',
        'rgba(79, 70, 229, 0.65)',
        'rgba(124, 58, 237, 0.65)'
    ];
    
    const darkThemeColors = [
        'rgba(199, 210, 254, 0.9)',
        'rgba(221, 214, 254, 0.9)',
        'rgba(243, 240, 255, 0.9)',
        'rgba(224, 231, 255, 0.9)',
        'rgba(199, 210, 254, 0.8)',
        'rgba(221, 214, 254, 0.8)',
        'rgba(243, 240, 255, 0.8)',
        'rgba(224, 231, 255, 0.8)',
        'rgba(199, 210, 254, 0.7)',
        'rgba(221, 214, 254, 0.7)'
    ];
    
    const colors = isDark ? darkThemeColors : lightThemeColors;
    const backgroundColors = values.map((_, index) => colors[index % colors.length]);
    
    const universitiesLabel = canvas.getAttribute("data-universities-label") || "Universities";

    new Chart(canvas.getContext("2d"), {
        type: "bar",
        data:{
            labels,
            datasets: [{
                label: universitiesLabel,
                data: values,
                backgroundColor: backgroundColors,
                borderColor: isDark 
                    ? 'rgba(199, 210, 254, 1)'
                    : 'rgba(79, 70, 229, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: {
                    display: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        color: isDark ? '#A78BFA' : undefined
                    },
                    grid: {
                        color: isDark ? 'rgba(167, 139, 250, 0.2)' : undefined
                    }
                },
                x: {
                    ticks: {
                        color: isDark ? '#A78BFA' : undefined
                    },
                    grid: {
                        color: isDark ? 'rgba(167, 139, 250, 0.2)' : undefined
                    }
                }
            }
        }
    })
})