document.addEventListener("DOMContentLoaded", function () {

  const wishlistButtons =
    document.querySelectorAll(".wishlist-btn");

  wishlistButtons.forEach(button => {

    button.addEventListener("click", function (e) {

      e.preventDefault();

      const btn = this;

      const bookId = btn.dataset.bookid;

      fetch('/Wishlist/ToggleWishlist', {

        method: 'POST',

        headers: {
          'Content-Type':
            'application/x-www-form-urlencoded'
        },

        body: `bookId=${bookId}`

      })

        .then(response => response.json())

        .then(data => {

          if (data.added) {

            btn.classList.add("active");

            btn.innerHTML =
              '<i class="fa-solid fa-heart"></i>';

          } else {

            btn.classList.remove("active");

            btn.innerHTML =
              '<i class="fa-regular fa-heart"></i>';
          }

        })

        .catch(error => {

          console.log(error);

        });

    });

  });

});
