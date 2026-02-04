export class OcenjivanjeFilma {
  constructor(user) { }

  async showRatingModal(movieId, userId) {
    console.log("movieId: " + movieId, " userId: " + userId);

    // proveri da li je korisnik vec uneo ocenu
    const userRating = await this.fetchUserRating(movieId, userId);

    const overlay = document.createElement("div");
    overlay.className = "modal-overlay";

    const modal = document.createElement("div");
    modal.className = "rating-modal";

    if (userRating === null) {
      // ako korisnik nije uneo ocenu, prikazi meni za unos ocene
      modal.innerHTML = `
        <div class="modal-content">
          <h3>Unesite vašu ocenu (1-10):</h3>
          <input type="number" id="ratingInput" min="1" max="10" />
          <button id="submitRating">Potvrdi</button>
          <button id="closeModal">Otkaži</button>
        </div>
      `;
    } else {
      // ako korisnik već ima ocenu, prikazi poruku
      modal.innerHTML = `
        <div class="modal-content">
          <h3>Već ste ocenili ovaj film!</h3>
          <p>Uneli ste ocenu: <strong>${userRating}</strong></p>
          <button id="closeModal">Zatvori</button>
        </div>
      `;
    }

    overlay.appendChild(modal);
    document.body.appendChild(overlay);

    const closeModal = () => overlay.remove();

    if (userRating === null) {
      document
        .getElementById("submitRating")
        .addEventListener("click", async () => {
          const ratingValue = parseInt(
            document.getElementById("ratingInput").value
          );

          if (!isNaN(ratingValue) && ratingValue >= 1 && ratingValue <= 10) {
            try {
              await this.rateMovie(movieId, userId, ratingValue);
              alert("Uspešno ste ocenili film!");
              closeModal();
            } catch (error) {
              alert("Došlo je do greške pri ocenjivanju.");
            }
          } else {
            alert("Uneli ste nevažeću ocenu!");
          }
        });
    }

    document.getElementById("closeModal").addEventListener("click", closeModal);

    overlay.addEventListener("click", (e) => {
      if (e.target === overlay) {
        closeModal();
      }
    });
  }

  async fetchUserRating(movieId, userId) {
    try {
      const response = await fetch(
        `https://localhost:5001/api/Ocena/getUnetaOcena/${userId}/${movieId}`
      );

      if (!response.ok) {
        throw new Error("Neuspešno preuzimanje unete ocene");
      }

      const data = await response.json();
      console.log(data.unetaOcena  )
      return data.unetaOcena; 
    } catch (error) {
      console.error("Greška pri preuzimanju ocene:", error);
      return null;
    }
  }

  async rateMovie(movieId, userId, rating) {
    try {
      const response = await fetch(
        `https://localhost:5001/api/Ocena/createOcena`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            UserId: userId,
            MovieId: movieId,
            UnetaOcena: rating,
          }),
        }
      );

      if (!response.ok) {
        throw new Error("Failed to rate the movie");
      }

      const data = await response.json();
      console.log(data);

    } catch (error) {
      console.error("Error rating the movie:", error);
      alert("Došlo je do greške prilikom ocenjivanja.");
    }
  }

  async fetchAverageRating(movieId) {
    try {
      const response = await fetch(
        `https://localhost:5001/api/Ocena/prosecnaOcena/${movieId}`
      );

      if (!response.ok) {
        throw new Error("Neuspešno preuzimanje prosečne ocene");
      }

      const data = await response.json();
      return data.prosecnaOcena || "N/A";
    } catch (error) {
      console.error("Greška pri preuzimanju prosečne ocene:", error);
      return "N/A";
    }
  }
}
