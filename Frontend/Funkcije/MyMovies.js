import { Movie } from "./Movie.js";
import { errorMessage } from "./ErrorMessage.js";
export class MyMovies {
  constructor(container, user) {
    this.container = container;
    this.userId = user.id;
    this.myMovies = null;
    this.divSubCont = document.createElement("div");
    this.divSubCont.className = "div-sub-container";
    this.divPom = document.createElement("div");
    this.divPom.className = "div-pom";
    this.divSubCont.appendChild(this.divPom);
    this.divBody = document.createElement("div");
    this.divSubCont.appendChild(this.divBody);
    this.divBody.className = "main-content-div";
    let sidebar = this.createSidebar();
    this.divSubCont.appendChild(sidebar);
    // console.log("User u MyMovies: " + this.userId); //dobro loguje id usera
  }

  async fetchWatchedMovies(
    api = `https://localhost:5001/Gledao/filter?korisnikID=${this.userId}`
  ) {
    try {
      const response = await fetch(api);
      if (!response.ok) {
        if (response.status === 404) {
          throw new Error("Niste odgledali nijedan film.");
        } else {
          throw new Error("Greška na serveru. Pokušajte ponovo kasnije.");
        }
      }
      const movies = await response.json();
      let mojiFilmovi = movies.map((m) => {
        return new Movie(
          m.id,
          m.naslov,
          m.godinaIzlaska,
          m.zanr,
          m.prosecnaOcena,
          m.kategorija,
          m.slikaURL
        );
      });

      this.myMovies = mojiFilmovi;
      for (let i = 0; i < this.myMovies.length; i++) {
        await this.myMovies[i].fetchMovieImage();
      }
      this.renderMovies(movies);
    } catch (error) {
      errorMessage(error.message, this.container);
    }
  }

  async renderMovies(movies) {
    this.divBody.replaceChildren();

    const title = document.createElement("h2");
    title.textContent = "Moji gledani filmovi";
    title.className = "page-header";
    this.divBody.appendChild(title);

    const moviesList = document.createElement("div");
    moviesList.className = "film-list";

    this.myMovies.forEach((movie) => {
      const movieCard = document.createElement("div");
      movieCard.className = "movie-card";

      // Poster
      const poster = document.createElement("img");
      poster.src = movie.url || "https://via.placeholder.com/150";
      poster.alt = `${movie.naslov || "Movie Poster"}`;
      poster.className = "movie-poster";
      movieCard.appendChild(poster);

      // Title
      const title = document.createElement("h3");
      title.textContent = movie.naslov || "No title";
      title.className = "movie-title";
      movieCard.appendChild(title);

      // Year
      const year = document.createElement("p");
      year.textContent = `Godina: ${movie.godina || "Nepoznata"}`;
      year.className = "movie-year";
      movieCard.appendChild(year);

      // Genre
      const genre = document.createElement("p");
      genre.textContent = `Žanr: ${movie.zanr || "Nepoznat"}`;
      genre.className = "movie-genre";
      movieCard.appendChild(genre);

      // Rating
      const rating = document.createElement("p");
      rating.textContent = `Ocena: ${movie.prOcena || "N/A"}`;
      rating.className = "movie-rating";
      movieCard.appendChild(rating);

      // Remove from Favorites Button
      const removeButton = document.createElement("button");
      removeButton.textContent = "Ukloni iz omiljenih";
      removeButton.className = "remove-button";
      removeButton.addEventListener("click", async () => {
        console.log(`Removing movie with ID ${movie.id}`);
        await this.removeFromFavorites(movie.id);
      });
      movieCard.appendChild(removeButton);

      moviesList.appendChild(movieCard);
    });
    this.divBody.appendChild(moviesList);
    this.container.appendChild(this.divSubCont);
  }

  // renderError(message) {
  //   this.container.innerHTML = ""; // Očistimo sadržaj
  //   const errorDiv = document.createElement("div");
  //   errorDiv.className = "error-message";
  //   errorDiv.textContent = message;
  //   this.container.appendChild(errorDiv);
  // }

  async removeFromFavorites(movieId) {
    try {
      console.log("User id za delete svidja mu se: " + this.userId);
      console.log("Movie id za delete svidja mu se: " + movieId);

      const response = await fetch(
        `https://localhost:5001/SvidjaMuSe?korisnikID=${this.userId}&filmID=${movieId}`,
        {
          method: "DELETE",
          headers: {
            "Content-Type": "application/json",
          },
        }
      );

      if (!response.ok) {
        throw new Error("Failed to remove the movie from favorites");
      }

      alert("Film je uspešno uklonjen iz omiljenih.");
      // Ponovo učitaj omiljene filmove
      await this.fetchWatchedMovies();
    } catch (error) {
      console.error("Error removing the movie:", error);
      alert("Došlo je do greške prilikom uklanjanja filma.");
    }
  }
  createSidebar() {
    // Provera da li sidebar već postoji
    if (document.getElementById("filterSidebar")) return;

    // Kreiranje sidebar div-a
    const sidebar = document.createElement("div");
    sidebar.id = "filterSidebar";
    sidebar.classList.add("sidebar"); // Stilizacija iz eksternog CSS-a

    // Naslov
    const title = document.createElement("h2");
    title.textContent = "Filteri";
    sidebar.appendChild(title);

    // Funkcija za dodavanje filtera
    function createFilter(labelText, inputType, inputId) {
      const container = document.createElement("div");
      container.classList.add("filter-item");

      const label = document.createElement("label");
      label.setAttribute("for", inputId);
      label.textContent = labelText;

      const input = document.createElement("input");
      input.type = inputType;
      input.id = inputId;

      container.appendChild(label);
      container.appendChild(input);
      sidebar.appendChild(container);
    }

    // Dodavanje filter polja
    createFilter("Godina izlaska:", "number", "godinaIzlaska");
    createFilter("Minimalna ocena:", "number", "ocena");

    const statuses = [
      "All",
      "Odgledano",
      "Planirano",
      "Trenutno gledam",
      "Odustao",
    ];
    const containStatus = document.createElement("div");
    const labelStatus = document.createElement("label");
    labelStatus.textContent = "Status:";
    containStatus.classList.add("filter-item");

    const selectStatus = document.createElement("select");
    selectStatus.id = "status";
    // Kreiraj opcije za svaki žanr
    statuses.forEach((status) => {
      const option = document.createElement("option");
      option.value = status;
      option.textContent = status;
      selectStatus.appendChild(option);
    });

    containStatus.appendChild(labelStatus);
    containStatus.appendChild(selectStatus);
    sidebar.appendChild(containStatus);

    // lista zanrova
    const genres = [
      "All",
      "Action",
      "Adventure",
      "Animation",
      "Biography",
      "Comedy",
      "Crime",
      "Documentary",
      "Drama",
      "Family",
      "Fantasy",
      "Game-Show",
      "History",
      "Horror",
      "Music",
      "Musical",
      "Mystery",
      "Romance",
      "Sci-Fi",
      "Sport",
      "Thriller",
      "War",
      "Western",
    ];

    const containZanr = document.createElement("div");
    const labelZanr = document.createElement("label");
    labelZanr.textContent = "Žanr:";
    containZanr.classList.add("filter-item");

    const selectZanr = document.createElement("select");
    selectZanr.id = "zanr";
   
    genres.forEach((genre) => {
      const option = document.createElement("option");
      option.value = genre;
      option.textContent = genre;
      selectZanr.appendChild(option);
    });

    containZanr.appendChild(labelZanr);
    containZanr.appendChild(selectZanr);
    sidebar.appendChild(containZanr);

    const sortContainer = document.createElement("div");
    sortContainer.classList.add("filter-item");

    const sortLabel = document.createElement("label");
    sortLabel.textContent = "Sortiraj po:";

    const sortSelect = document.createElement("select");
    sortSelect.id = "parametarSortiranja";

    const options = [
      " ",
      "Godina Izlaska",
      "Prosecna Ocena",
      "Datum Dodavanja",
      "Status",
    ].map((opt) => {
      const option = document.createElement("option");
      option.value = opt;
      option.textContent = opt;
      return option;
    });
    const correlationMap = {
      "Godina Izlaska": "GodinaIzlaska",
      "Prosecna Ocena": "ProsecnaOcena",
      "Datum Dodavanja": "DatumPromeneStatusa",
      Status: "Status",
    };
    options.forEach((option) => sortSelect.appendChild(option));

    sortContainer.appendChild(sortLabel);
    sortContainer.appendChild(sortSelect);
    sidebar.appendChild(sortContainer);

    const orderContainer = document.createElement("div");
    orderContainer.classList.add("filter-item");

    const orderLabel = document.createElement("label");
    orderLabel.textContent = "Rastući:";

    const orderInput = document.createElement("input");
    orderInput.type = "checkbox";
    orderInput.id = "rastuci";

    orderContainer.appendChild(orderLabel);
    orderContainer.appendChild(orderInput);
    sidebar.appendChild(orderContainer);

    const applyButton = document.createElement("button");
    applyButton.textContent = "Primeni filter";
    applyButton.classList.add("filter-button");

    applyButton.addEventListener("click", () => {
      let upit = `?korisnikID=${this.userId}`;
      const godinaIzlaska = document.getElementById("godinaIzlaska").value;
      if (godinaIzlaska != null && godinaIzlaska != "") {
        upit = `${upit}&godinaIzlaska=${godinaIzlaska}`;
      }
      const ocena = document.getElementById("ocena").value;
      if (ocena != null && ocena != "") {
        upit = `${upit}&ocena=${ocena}`;
      }
      const status = document.getElementById("status").value;
      if (status != null && status != "" && status != "All") {
        upit = `${upit}&status=${status}`;
      }
      const zanr = document.getElementById("zanr").value;
      if (zanr != null && zanr != "" && zanr != "All") {
        upit = `${upit}&zanr=${zanr}`;
      }
      const parametarSortiranja = document.getElementById(
        "parametarSortiranja"
      ).value;
      if (
        parametarSortiranja != null &&
        parametarSortiranja != "" &&
        parametarSortiranja != " "
      ) {
        upit = `${upit}&parametarSortiranja=${correlationMap[parametarSortiranja]}`;
      }
      let koSeSoritira = "";
      if (
        parametarSortiranja != null &&
        parametarSortiranja != "" &&
        parametarSortiranja != " "
      ) {
        if (
          parametarSortiranja == "Godina Izlaska" ||
          parametarSortiranja == "Prosecna Ocena"
        ) {
          koSeSoritira = "film";
        } else if (
          parametarSortiranja == "Datum Dodavanja" ||
          parametarSortiranja == "Status"
        ) {
          koSeSoritira = "rel";
        }
      }
      if (
        koSeSoritira != "" &&
        parametarSortiranja != null &&
        parametarSortiranja != "" &&
        parametarSortiranja != " "
      ) {
        upit = `${upit}&VlasnikParametar=${koSeSoritira}`;
      }
      const rastuci = document.getElementById("rastuci").checked;
      if (
        koSeSoritira != "" &&
        parametarSortiranja != null &&
        parametarSortiranja != "" &&
        parametarSortiranja != " "
      ) {
        upit = `${upit}&rastuci=${rastuci}`;
      }
      console.log(upit);

      this.fetchWatchedMovies(`https://localhost:5001/Gledao/filter${upit}`);
    });

    sidebar.appendChild(applyButton);
   
    return sidebar;
  }
}
