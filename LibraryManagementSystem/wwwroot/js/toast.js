document.addEventListener("DOMContentLoaded", function () {

    const toast = document.querySelector(".show-toast");

    if (toast) {

        setTimeout(() => {
            toast.classList.add("toast-hide");
        }, 3500);

        const closeBtn = toast.querySelector(".toast-close");

        closeBtn.addEventListener("click", function () {
            toast.classList.add("toast-hide");
        });
    }

});