document.addEventListener("DOMContentLoaded", () => {
    const favBtn = document.getElementById("favBtn");
    if (!favBtn) return;

    const isAuthenticated = favBtn.dataset.isAuthenticated === "true";

    favBtn.addEventListener("click", async (e) => {
        e.preventDefault();
        
        if (!isAuthenticated) {
            const modalElement = document.getElementById("loginRegisterModal");
            if (!modalElement) {
                console.error("Login modal not found");
                alert("Please log in to add favorites");
                window.location.href = "/Identity/Account/Login";
                return;
            }
            
            function closeModal() {
                if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                    const modal = bootstrap.Modal.getInstance(modalElement);
                    if (modal) {
                        modal.hide();
                    } else {
                        modalElement.classList.remove("show");
                        setTimeout(() => {
                            modalElement.style.display = "none";
                            document.body.classList.remove("modal-open");
                            const backdrop = document.getElementById("modalBackdrop");
                            if (backdrop) {
                                backdrop.classList.remove("show");
                                setTimeout(() => backdrop.remove(), 150);
                            }
                        }, 150);
                    }
                } else {
                    modalElement.classList.remove("show");
                    setTimeout(() => {
                        modalElement.style.display = "none";
                        document.body.classList.remove("modal-open");
                        const backdrop = document.getElementById("modalBackdrop");
                        if (backdrop) {
                            backdrop.classList.remove("show");
                            setTimeout(() => backdrop.remove(), 150);
                        }
                    }, 150);
                }
            }
            
            function showModal() {
                if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                    const modal = bootstrap.Modal.getOrCreateInstance(modalElement, {
                        backdrop: true,
                        keyboard: true
                    });
                    modal.show();
                } else {
                    modalElement.style.display = "block";
                    document.body.classList.add("modal-open");
                    
                    let backdrop = document.getElementById("modalBackdrop");
                    if (!backdrop) {
                        backdrop = document.createElement("div");
                        backdrop.className = "modal-backdrop fade";
                        backdrop.id = "modalBackdrop";
                        backdrop.onclick = (e) => {
                            if (e.target === backdrop) {
                                closeModal();
                            }
                        };
                        document.body.appendChild(backdrop);
                    }
                    
                    requestAnimationFrame(() => {
                        modalElement.classList.add("show");
                        backdrop.classList.add("show");
                    });
                    
                    modalElement.onclick = (e) => {
                        if (e.target === modalElement) {
                            closeModal();
                        }
                    };
                }
            }
            
            const closeBtn = modalElement.querySelector('[data-bs-dismiss="modal"], .btn-close');
            if (closeBtn && !closeBtn.hasAttribute('data-listener-added')) {
                closeBtn.setAttribute('data-listener-added', 'true');
                closeBtn.addEventListener('click', closeModal);
            }
            
            showModal();
            return;
        }

        const universityId = favBtn.dataset.universityId;
        
        if (!universityId) {
            console.error("University ID not found");
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
                    const modalElement = document.getElementById("loginRegisterModal");
                    if (modalElement) {
                        if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                            const modal = bootstrap.Modal.getOrCreateInstance(modalElement, {
                                backdrop: true,
                                keyboard: true
                            });
                            modal.show();
                        } else {
                            modalElement.style.display = "block";
                            document.body.classList.add("modal-open");
                            
                            let backdrop = document.getElementById("modalBackdrop");
                            if (!backdrop) {
                                backdrop = document.createElement("div");
                                backdrop.className = "modal-backdrop fade";
                                backdrop.id = "modalBackdrop";
                                backdrop.onclick = () => {
                                    modalElement.style.display = "none";
                                    modalElement.classList.remove("show");
                                    document.body.classList.remove("modal-open");
                                    backdrop.classList.remove("show");
                                    setTimeout(() => backdrop.remove(), 150);
                                };
                                document.body.appendChild(backdrop);
                            }
                            
                            requestAnimationFrame(() => {
                                modalElement.classList.add("show");
                                backdrop.classList.add("show");
                            });
                        }
                    } else {
                        alert("Please log in to add favorites");
                        window.location.href = "/Identity/Account/Login";
                    }
                    return;
                }
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();

            favBtn.classList.toggle("btn-danger", data.isFavourite);
            favBtn.classList.toggle("btn-outline-danger", !data.isFavourite);
            
            const favText = document.getElementById("favText");
            if (favText) {
                const addText = favBtn.dataset.addText || "Add to favourites";
                const removeText = favBtn.dataset.removeText || "Remove from favourites";
                favText.innerText = data.isFavourite ? removeText : addText;
            }
        } catch (error) {
            console.error("Error toggling favorite:", error);
            alert("An error occurred. Please try again.");
        }
    });
});