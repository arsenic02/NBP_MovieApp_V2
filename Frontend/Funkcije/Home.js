import { Movie } from "./Movie.js";
import { MovieInfo } from "./MovieInfo.js";
import { OcenjivanjeFilma } from "./OcenjivanjeFilma.js";
import { errorMessage } from "./ErrorMessage.js";
export class Home {
  constructor(container, user) {
    this.container = container;
    this.apiUrl = "https://localhost:5001/Film/all";
    this.user = user;
    this.myMovies = null;
  }

  async fetchFilms(page = 1, size = 6, api) {
    try {
      console.log(`${api}page=${page}&size=${size}`);
      const response = await fetch(`${api}page=${page}&size=${size}`);
      if (!response.ok) {
        if (response.status === 404) {
          throw new Error("Nije moguce pribaviti filmove");
        } else {
          throw new Error("Greška na serveru. Pokušajte ponovo kasnije.");
        }
      }

      const data = await response.json();
      console.log(data);
      return data;
    } catch (error) {
      errorMessage(error, this.container);
      return null;
    }
  }

  async renderHomePage(
    api = "https://localhost:5001/Film/all?",
    page = 1,
    size = 6
  ) {
    const filmsData = await this.fetchFilms(page, size, api);
    try {
      if (!filmsData || !Array.isArray(filmsData.movies)) {
        errorMessage("Nije moguce pribaviti filmove", this.container);
        return;
      }

      let mojiFilmovi = filmsData.movies.map(
        (m) =>
          new Movie(
            m.id,
            m.naslov,
            m.godinaIzlaska,
            m.zanr,
            m.prosecnaOcena,
            m.kategorija,
            m.slikaURL
          )
      );

      this.myMovies = mojiFilmovi;
    
      await Promise.all(this.myMovies.map((movie) => movie.fetchMovieImage()));

      const ocenjivanjeFilma = new OcenjivanjeFilma(this.user);
      const ocene = await Promise.all(
        this.myMovies.map((movie) =>
          ocenjivanjeFilma.fetchAverageRating(movie.id)
        )
      );

      this.myMovies.forEach((movie, index) => {
        movie.averageRating = ocene[index] || 0;
      });

      this.myMovies.sort((a, b) => (b.godina || 0) - (a.godina || 0));

      this.renderMovies(this.myMovies);
      this.renderPagination(filmsData.totalCount, page, size);
    } catch (error) {}
  }

  renderMovies(sortedMovies) {
    this.container.innerHTML = ""; 

    const filmList = document.createElement("div");
    filmList.className = "film-list";

    sortedMovies.forEach((movie) => {
      const movieCard = document.createElement("div");
      movieCard.className = "movie-card";

      const movieInfo = new MovieInfo(this.container, this.user);
      movieCard.addEventListener("click", () => {
        movieInfo.openMovieDetails(movie);
      });

      // Poster
      const poster = document.createElement("img");
      poster.src = movie.url;
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
      rating.textContent = `Prosečna ocena: ${
        movie.averageRating > 0 ? movie.averageRating.toFixed(1) : "N/A"
      }`;
      rating.className = "movie-rating";
      movieCard.appendChild(rating);

      filmList.appendChild(movieCard);
    });

    this.container.appendChild(filmList);
  }

  renderPagination(totalCount, currentPage, pageSize) {
    const pagination = document.createElement("div");
    pagination.className = "pagination";

    const totalPages = Math.ceil(totalCount / pageSize);

    for (let i = 1; i <= totalPages; i++) {
      const pageLink = document.createElement("button");
      pageLink.textContent = i;
      pageLink.className = i === currentPage ? "active" : "";

      pageLink.addEventListener("click", async () => {
        await this.renderHomePage(
          "https://localhost:5001/Film/all?",
          i,
          pageSize
        );
      });

      pagination.appendChild(pageLink);
    }

    this.container.appendChild(pagination);
  }

  async likeMovie(movieId) {
    try {
      const response = await fetch(`https://localhost:5001/SvidjaMuSe`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          korisnikID: this.user.id,
          filmID: movieId,
          javna: true,
        }),
      });

      if (!response.ok) {
        throw new Error("Failed to like the movie");
      }
      const result = await response.json();
      alert("Film je uspešno dodat u vašu listu omiljenih!");
    } catch (error) {
      console.log("UserId je " + korisnikId);
      console.log("movieId je " + filmId);
      console.error("Error liking the movie:", error);
      alert("Došlo je do greške prilikom dodavanja filma.");
    }
  }

  async openComments(movieId) {
    console.log("Ulaz u open comments metodu");
  }

}
