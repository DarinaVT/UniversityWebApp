document.addEventListener("DOMContentLoaded", async () => {
    let retries = 0;
    while (typeof Chart === 'undefined' && retries < 10) {
        await new Promise(resolve => setTimeout(resolve, 100));
        retries++;
    }
    
    if (typeof Chart === 'undefined') {
        console.error("Chart.js is not loaded");
        return;
    }
    
    await new Promise(resolve => setTimeout(resolve, 300));
    
    const uniId = document.getElementById("universityId")?.value;
    const chartCanvas = document.getElementById("compareChart");
    const chartContainer = document.querySelector('.university-comparison-chart-container');
    
    console.log("Comparison chart check:", { 
        uniId, 
        hasCanvas: !!chartCanvas,
        hasContainer: !!chartContainer,
        canvasId: chartCanvas?.id,
        ChartAvailable: typeof Chart !== 'undefined'
    });
    
    if (!uniId) {
        console.warn("University ID not found");
        return;
    }
    
    if (!chartCanvas) {
        console.warn("Chart canvas not found");
        return;
    }
    
    if (chartContainer) {
        chartContainer.style.minHeight = "500px";
        chartContainer.style.height = "500px";
    }
    
    console.log("Loading comparison chart for university:", uniId);

    try {
        const res = await fetch(`/University/CompareChartData?id=${uniId}`);
        if (!res.ok) {
            console.error("Failed to fetch comparison data:", res.status, res.statusText);
            return;
        }

        const data = await res.json();
        console.log("Comparison data received:", data);
        
        if (!data || data.length === 0) {
            console.warn("No comparison data available");
            return;
        }
        
        if (!data[0] || !data[0].name) {
            console.error("Invalid data structure:", data);
            return;
        }

        if (window.compareChart && typeof window.compareChart.destroy === 'function') {
            try {
                window.compareChart.destroy();
            } catch (e) {
                console.warn("Error destroying existing chart:", e);
            }
        }

        const labels = data.map(x => {
            const name = x.name || x.Name || "Unknown";
            return name;
        }).filter(name => name !== "Unknown");
        
        const ratings = data.map(x => {
            const rating = x.rating !== undefined ? x.rating : (x.Rating !== undefined ? x.Rating : 0);
            return parseFloat(rating) || 0;
        });
        
        const gpas = data.map(x => {
            const gpa = x.averageGpa !== undefined ? x.averageGpa : (x.AverageGpa !== undefined ? x.AverageGpa : 0);
            return parseFloat(gpa) || 0;
        });

        console.log("Processed chart data:", { labels, ratings, gpas });

        if (labels.length === 0 || ratings.length === 0) {
            console.warn("No valid data for chart");
            return;
        }

        const ratingLabel = chartCanvas.dataset.ratingLabel || "Rating";
        const gpaLabel = chartCanvas.dataset.gpaLabel || "Average GPA";

        window.compareChart = new Chart(chartCanvas, {
            type: "bar",
            data: {
                labels: labels,
                datasets: [
                    { 
                        label: ratingLabel, 
                        data: ratings,
                        backgroundColor: "rgba(37, 99, 235, 0.6)",
                        borderColor: "rgba(37, 99, 235, 1)",
                        borderWidth: 1
                    },
                    { 
                        label: gpaLabel, 
                        data: gpas,
                        backgroundColor: "rgba(124, 58, 237, 0.6)",
                        borderColor: "rgba(124, 58, 237, 1)",
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true
                    }
                },
                plugins: {
                    legend: {
                        display: true,
                        position: 'top'
                    }
                }
            }
        });
        
        console.log("Chart created successfully with", labels.length, "items");
    } catch (error) {
        console.error("Error loading comparison chart:", error);
    }
});