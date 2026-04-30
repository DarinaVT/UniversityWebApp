document.addEventListener("DOMContentLoaded", () => {
    const form = document.getElementById("addReviewForm");
    if (!form) {
        console.log("Review form not found");
        return;
    }

    console.log("Review form found, attaching submit handler");

    form.addEventListener("submit", async e => {
        e.preventDefault();
        e.stopPropagation();

        const universityIdInput = form.querySelector('[name="UniversityId"]') || form.querySelector('[name="universityId"]');
        const ratingInput = form.querySelector('[name="Rating"]') || form.querySelector('[name="rating"]');
        const commentInput = form.querySelector('[name="Comment"]') || form.querySelector('[name="comment"]');

        const universityId = universityIdInput?.value;
        const rating = ratingInput?.value;
        const comment = commentInput?.value;

        console.log("Form values:", { universityId, rating, comment });

        if (!universityId || !rating || !comment) {
            const fillAllMsg = form.dataset.pleaseFillAll || "Please fill in all fields";
            alert(fillAllMsg);
            return;
        }

        try {
            const formData = new URLSearchParams();
            formData.append("universityId", universityId);
            formData.append("rating", rating);
            formData.append("comment", comment);

            const res = await fetch("/Review/Add", {
                method: "POST",
                headers: { 
                    "Content-Type": "application/x-www-form-urlencoded"
                },
                body: formData
            });

            console.log("Review submission response:", res.status, res.statusText);

            if (res.ok) {
                let result;
                try {
                    result = await res.json();
                } catch {
                    result = { message: "Review submitted and pending approval." };
                }
                form.reset();
                const submittedMsg = form.dataset.reviewSubmitted || result.message || "Review submitted and pending approval.";
                alert(submittedMsg);
                setTimeout(() => window.location.reload(), 1000);
            } else {
                let errorMessage = form.dataset.errorSubmitting || "Error submitting review. Please try again.";
                try {
                    const errorData = await res.json();
                    errorMessage = errorData.message || errorMessage;
                } catch {
                    const errorText = await res.text();
                    console.error("Error submitting review:", res.status, errorText);
                }
                alert(errorMessage);
            }
        } catch (error) {
            console.error("Error submitting review:", error);
            const errorMsg = form.dataset.errorSubmitting || "Error submitting review. Please try again.";
            alert(errorMsg);
        }
    });
});