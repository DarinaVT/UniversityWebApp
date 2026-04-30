document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll(".btn-approve").forEach(btn => {
        btn.addEventListener("click", async function() {
            const id = this.dataset.id;
            try {
                const response = await fetch(`/AdminReview/Approve?id=${id}`, {
                    method: "POST",
                    headers: {
                        "X-Requested-With": "XMLHttpRequest"
                    }
                });
                if (response.ok) {
                    const row = document.getElementById(`review-${id}`);
                    if (row) {
                        row.style.transition = "opacity 0.3s";
                        row.style.opacity = "0";
                        setTimeout(() => row.remove(), 300);
                    }
                } else {
                    alert("Error approving review");
                }
            } catch (error) {
                console.error("Error approving review:", error);
                alert("Error approving review");
            }
        });
    });

    document.querySelectorAll(".btn-reject").forEach(btn => {
        btn.addEventListener("click", async function() {
            const id = this.dataset.id;
            try {
                const response = await fetch(`/AdminReview/Reject?id=${id}`, {
                    method: "POST",
                    headers: {
                        "X-Requested-With": "XMLHttpRequest"
                    }
                });
                if (response.ok) {
                    const row = document.getElementById(`review-${id}`);
                    if (row) {
                        row.style.transition = "opacity 0.3s";
                        row.style.opacity = "0";
                        setTimeout(() => row.remove(), 300);
                    }
                } else {
                    alert("Error rejecting review");
                }
            } catch (error) {
                console.error("Error rejecting review:", error);
                alert("Error rejecting review");
            }
        });
    });
});
