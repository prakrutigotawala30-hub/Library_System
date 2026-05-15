document.addEventListener("DOMContentLoaded", function () {

    const buttons =
        document.querySelectorAll(".wishlist-btn");

    buttons.forEach(button => {

        button.addEventListener("click", function (e) {

            e.preventDefault();

            const bookId =
                this.dataset.bookid;

            fetch("/Wishlist/Add", {

                method: "POST",

                headers: {
                    "Content-Type": "application/json"
                },

                body: JSON.stringify({
                    bookId: bookId
                })

            })
                .then(response => {

                    if (response.ok) {

                        showToast(
                            "Book added to wishlist ❤️"
                        );

                    }
                    else {

                        showToast(
                            "Something went wrong ❌"
                        );

                    }

                });

        });

    });

});

// TOAST

function showToast(message) {

    const toast =
        document.createElement("div");

    toast.innerText = message;

    toast.style.position = "fixed";
    toast.style.bottom = "30px";
    toast.style.right = "30px";
    toast.style.background = "#7e22ce";
    toast.style.color = "white";
    toast.style.padding = "14px 22px";
    toast.style.borderRadius = "12px";
    toast.style.zIndex = "9999";

    document.body.appendChild(toast);

    setTimeout(() => {

        toast.remove();

    }, 3000);

}
