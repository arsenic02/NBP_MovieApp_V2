import { ListaKojuPratimModel } from "./ListeKojePratimModel.js";
import { Movie } from "./Movie.js";
import { MovieInfo } from "./MovieInfo.js";
import { OcenjivanjeFilma } from "./OcenjivanjeFilma.js";
import { errorMessage } from "./ErrorMessage.js";
export class ListeKojePratim {
  constructor(container, user) {
    this.container = container;
    this.user = user;
    this.myLists = null;
    this.totalCount = 0;
    this.currentPage = 0;
    this.pageSize = 0;
  }

  async fetchLists(page = 1, size = 2) {
    try {
      const response = await fetch(
        `https://localhost:5001/PratiListu/followed-list/${this.user.id}?page=${page}&size=${size}`
      );
      if (!response.ok) {
        if (response.status === 404) {
          throw new Error("Ne pratite nijednu listu.");
        } else {
          throw new Error("Greška na serveru. Pokušajte ponovo kasnije.");
        }
      }

      const data = await response.json();
      this.totalCount = data.totalResults;
      this.pageSize = data.pageSize;
      this.page = data.page;
      let liste = data.lista;
      let listeKojePratim = liste.map((l) => {
        return new ListaKojuPratimModel(
          l.vlasnikID,
          l.imeListe,
          l.listaFilmova
        );
      });
      this.myLists = listeKojePratim;
      //return data;
    } catch (error) {
      errorMessage(error, this.container);
      return null;
    }
  }

  async renderLKPPage(page = 1, size = 6) {
    //LKP - ListeKojePratim
    await this.fetchLists(page, size);

    for (let i = 0; i < this.myLists.length; i++) {
      await this.myLists[i].fetchMovieImage();
    }

    this.container.innerHTML = "";

    const header = document.createElement("h1");
    header.className = "page-header";
    this.container.appendChild(header);

    this.myLists.forEach((list) => {
      const filmList = document.createElement("div");
      filmList.className = "film-list";
      const naslovContainer = document.createElement("div");
      naslovContainer.className = "header-container";
      const naslov = document.createElement("h1");
      naslov.textContent = `Lista ${list.ime}`;
      naslov.classList.add("film-list-header");
      naslovContainer.appendChild(naslov);
      this.container.appendChild(naslovContainer);
      list.filmovi.forEach(async (movie) => {
        const movieCard = document.createElement("div");
        movieCard.className = "movie-card";

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

        const ocenjivanjeFilma = new OcenjivanjeFilma(this.user);
        // Rating
        const rating = document.createElement("p");
        const averageRating = await ocenjivanjeFilma.fetchAverageRating(
          movie.id
        );
        rating.textContent = `Prosečna ocena: ${averageRating}`;
        //rating.textContent = `Ocena: ${movie.prOcena || "N/A"}`;
        rating.className = "movie-rating";
        movieCard.appendChild(rating);

        filmList.appendChild(movieCard);
      });
      this.container.appendChild(filmList);
    });

    const pagination = document.createElement("div");
    pagination.className = "pagination";

    const totalPages = Math.ceil(this.totalCount / this.pageSize);

    for (let i = 1; i <= totalPages; i++) {
      const pageLink = document.createElement("button");
      pageLink.textContent = i;
      pageLink.className = i === this.currentPage ? "active" : "";

      pageLink.addEventListener("click", async () => {
        await this.renderHomePage(i, pageSize);
      });

      pagination.appendChild(pageLink);
    }

    this.container.appendChild(pagination);
  }
}
