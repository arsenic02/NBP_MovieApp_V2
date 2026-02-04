import { Movie } from "./Movie.js";
import { OcenjivanjeFilma } from "./OcenjivanjeFilma.js";
import { MovieInfo } from "./MovieInfo.js";
import { errorMessage } from "./ErrorMessage.js";

export class PreporuceneListe {
  constructor(container, user) {
    this.container = container;
    this.user = user;
    this.recommendedLists = null;
    this.totalCount = 0;
    this.currentPage = 1;
    this.pageSize = 10;
  }

  async fetchRecommendedLists(page = 1, size = 10) {
    try {
      const response = await fetch(
        `https://localhost:5001/PratiListu/recommended-lists?korisnikID=${this.user.id}&page=${page}&size=${size}`
      );
      if (!response.ok) {
        if (response.status === 404) {
          throw new Error("Nije moguce pribaviti preporucene liste.");
        } else {
          throw new Error("Greška na serveru. Pokušajte ponovo kasnije.");
        }
      }

      const data = await response.json();
      console.log("Received data:", data);

      this.totalCount = data.totalResults;
      this.pageSize = data.pageSize;
      this.currentPage = data.page;

      this.recommendedLists = data.recommendedMovies || [];
    } catch (error) {
      errorMessage(error, this.container);
    }
  }

  async renderRecommendedLists(page = 1, size = 10) {
    try {
      await this.fetchRecommendedLists(page, size);
      if (!this.recommendedLists) {
        throw new Error("Nije moguce pribaviti preporucene liste");
      }

      this.container.innerHTML = "";

      const header = document.createElement("h1");
      header.textContent = "Preporučene liste filmova";
      header.className = "page-header";
      this.container.appendChild(header);

      this.recommendedLists.forEach(async (list) => {
        const listContainer = document.createElement("div");
        listContainer.className = "recommended-list";

        const listTitle = document.createElement("h2");
        listTitle.textContent = `Preporučena lista od: ${list.najslicnijiKorisnici[0].korisnickoIme}`;
        listTitle.classList.add("recommended-list-header");
        listContainer.appendChild(listTitle);

        const filmList = document.createElement("div");
        filmList.className = "film-list";

        list.preporuceniFilmovi.forEach(async (movieData) => {
          const movie = new Movie(
            movieData.id,
            movieData.naslov,
            movieData.godinaIzlaska,
            movieData.zanr,
            movieData.prOcena,
            movieData.kategorija,
            movieData.url
          );

          const movieCard = document.createElement("div");
          movieCard.className = "movie-card";
         
          await movie.fetchMovieImage(); 
          const url = movie.url || "https://via.placeholder.com/150";

          // Poster
          const poster = document.createElement("img");
          poster.src = url;
          poster.alt = movie.naslov || "Movie Poster";
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
          const ocenjivanjeFilma = new OcenjivanjeFilma(this.user);
          const rating = document.createElement("p");
          const averageRating = await ocenjivanjeFilma.fetchAverageRating(
            movie.id
          );
          rating.textContent = `Prosečna ocena: ${averageRating || "N/A"}`;
          rating.className = "movie-rating";
          movieCard.appendChild(rating);

          const movieInfo = new MovieInfo(this.container, this.user);
          movieCard.addEventListener("click", () => {
            movieInfo.openMovieDetails(movie);
          });

          filmList.appendChild(movieCard);
        });

        listContainer.appendChild(filmList);
        this.container.appendChild(listContainer);
      });

      this.renderPagination();
    } catch (error) {
      errorMessage(error, this.container);
    }
  }

  renderPagination() {
    const pagination = document.createElement("div");
    pagination.className = "pagination";

    const totalPages = Math.ceil(this.totalCount / this.pageSize);

    for (let i = 1; i <= totalPages; i++) {
      const pageLink = document.createElement("button");
      pageLink.textContent = i;
      pageLink.className = i === this.currentPage ? "active" : "";

      pageLink.addEventListener("click", async () => {
        await this.renderRecommendedLists(i, this.pageSize);
      });

      pagination.appendChild(pageLink);
    }
    this.container.appendChild(pagination);
  }
}
