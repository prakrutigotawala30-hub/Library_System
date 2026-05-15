// SEARCH FILTER

const searchInput =
    document.getElementById("searchInput");

if (searchInput) {

    searchInput.addEventListener("keyup", function () {

        const value =
            this.value.toLowerCase();

        const cards =
            document.querySelectorAll(".book-card");

        cards.forEach(card => {

            const title =
                card.innerText.toLowerCase();

            if (title.includes(value)) {
                card.style.display = "block";
            }
            else {
                card.style.display = "none";
            }

        });

    });

}

// SORT DROPDOWN

const sortSelect =
    document.getElementById("sortSelect");

if (sortSelect) {

    sortSelect.addEventListener("change", function () {

        const selected =
            this.value;

        console.log("Sort By:", selected);

    });

}

// PAGINATION BUTTONS

const pageButtons =
    document.querySelectorAll(".page-btn");

pageButtons.forEach(btn => {

    btn.addEventListener("click", function () {

        pageButtons.forEach(x =>
            x.classList.remove("active"));

        this.classList.add("active");

    });

});
