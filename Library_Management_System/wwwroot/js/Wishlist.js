document.addEventListener("DOMContentLoaded", function () {

  document.querySelectorAll(".wishlist-btn").forEach(btn => {

    btn.addEventListener("click", async function () {

      const bookId = this.dataset.bookid ? parseInt(this.dataset.bookid) : null;
      if (!bookId) return;

      const body = new URLSearchParams();
      body.append("bookId", bookId);

      try {

        const response = await fetch('/Member/Wishlist/Toggle', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
          },
          body: body
        });

        const result = await response.json();

        if (!result.success) {
          alert(result.message || "Login required");
          return;
        }

        // ⭐ UPDATE ALL BUTTON STATES USING RETURNED wishlistIds
        document.querySelectorAll(".wishlist-btn").forEach(b => {

          const id = parseInt(b.dataset.bookid);

          const icon = b.querySelector("i");

          const isActive = result.wishlistIds.includes(id);

          b.classList.toggle("active", isActive);

          if (icon) {
            if (isActive) {
              icon.classList.remove("fa-regular");
              icon.classList.add("fa-solid");
            } else {
              icon.classList.add("fa-regular");
              icon.classList.remove("fa-solid");
            }
          }

        });

      } catch (err) {
        console.log(err);
        alert("Something went wrong");
      }

    });

  });

});
