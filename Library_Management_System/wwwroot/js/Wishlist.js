document.addEventListener("DOMContentLoaded", function () {

  const wishlistButtons =
    document.querySelectorAll(".wishlist-btn");

  wishlistButtons.forEach(button => {

    button.addEventListener("click", function (e) {

      e.preventDefault();

      const btn = this;

      const bookId = btn.dataset.bookid;

      // CSRF token — Razor renders a hidden __RequestVerificationToken field
      // in any view containing a form. Grab it so the POST passes
      // [ValidateAntiForgeryToken] on the controller.
      const tokenInput = document.querySelector(
        'input[name="__RequestVerificationToken"]'
      );
      const token = tokenInput ? tokenInput.value : '';

      fetch('/Member/Wishlist/ToggleWishlist', {

        method: 'POST',

        headers: {
          'Content-Type':
            'application/x-www-form-urlencoded'
        },

        body: `bookId=${bookId}&__RequestVerificationToken=${encodeURIComponent(token)}`

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
