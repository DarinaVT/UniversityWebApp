document.addEventListener("DOMContentLoaded", () => {
    const confirmMessage = document.body.dataset.confirmRemoveFavourite || 
                          "Are you sure you want to remove this university from your favorites?";
    
    document.addEventListener("click", async (e) => {
        const removeBtn = e.target.closest(".remove-favorite-btn");
        if (!removeBtn) return;

        e.preventDefault();
        e.stopPropagation();

        const universityId = removeBtn.dataset.universityId;
        if (!universityId) return;

        if (!confirm(confirmMessage)) {
            return;
        }

        try {
            const response = await fetch("/Favourite/Toggle", {
                method: "POST",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded",
                    "RequestVerificationToken": document.querySelector('input[name="__RequestVerificationToken"]')?.value || ""
                },
                body: `universityId=${universityId}`
            });

            if (!response.ok) {
                if (response.status === 401) {
                    alert("Please log in to manage favorites");
                    window.location.href = "/Identity/Account/Login";
                    return;
                }
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();

            if (!data.isFavourite) {
                const cardContainer = removeBtn.closest(".favorites-card-container");
                if (cardContainer) {
                    const parentCol = cardContainer.closest(".col-12");
                    const cardToRemove = parentCol || cardContainer;
                    
                    cardToRemove.style.transition = "opacity 0.3s ease, transform 0.3s ease";
                    cardToRemove.style.opacity = "0";
                    cardToRemove.style.transform = "translateX(-20px)";
                    
                    setTimeout(() => {
                        cardToRemove.remove();
                        
                        const remainingCards = document.querySelectorAll(".favorites-card-container");
                        if (remainingCards.length === 0) {
                            const resultsDiv = document.getElementById("catalogResults");
                            if (resultsDiv) {
                                resultsDiv.innerHTML = `
                                    <div class="alert alert-info text-center">
                                        <i class="bi bi-info-circle me-2"></i>No favorites found.
                                    </div>
                                `;
                            }
                        }
                    }, 300);
                }
            }
        } catch (error) {
            console.error("Error removing favorite:", error);
            alert("An error occurred. Please try again.");
        }
    });
});

