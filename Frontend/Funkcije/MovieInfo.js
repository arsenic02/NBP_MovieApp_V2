import { GledanjeFilmova } from "./GledanjeFilmova.js";
import { Komentari } from "./Komentari.js";
import { OcenjivanjeFilma } from "./OcenjivanjeFilma.js";
import { SvidjanjeFilma } from "./SvidjanjeFilma.js";

export class MovieInfo {
  constructor(container, user) {
    this.container = container;
    this.user = user;
  }

  async openMovieDetails(movie) {
   
    this.container.innerHTML = "";
    const movieInfo = document.createElement("div");
    movieInfo.className = "movie-info";
   
    const movieDetails = document.createElement("div");
    movieDetails.className = "movie-details";

    // Slika filma
    const poster = document.createElement("img");
    poster.src = movie.url;
    poster.alt = `${movie.naslov || "Movie Poster"}`;
    poster.className = "movie-details-poster";
    movieDetails.appendChild(poster);

    // Naslov
    const title = document.createElement("h1");
    title.textContent = movie.naslov || "No title";
    title.className = "movie-details-title";
    movieInfo.appendChild(title);

    // Godina
    const year = document.createElement("p");
    year.textContent = `Godina: ${movie.godina || "Nepoznata"}`;
    year.className = "movie-details-year";
    movieInfo.appendChild(year);

    // Žanr
    const genre = document.createElement("p");
    genre.textContent = `Žanr: ${movie.zanr || "Nepoznat"}`;
    genre.className = "movie-details-genre";
    movieInfo.appendChild(genre);

    const ocenjivanjeFilma = new OcenjivanjeFilma(this.user);
    // Ocena
    const rating = document.createElement("p");
    const averageRating = await ocenjivanjeFilma.fetchAverageRating(movie.id);
    rating.textContent = `Prosečna ocena: ${averageRating}`;
    
    rating.className = "movie-details-rating";
    movieInfo.appendChild(rating);

    // Status gledanja (combobox)
    const statusContainer = document.createElement("div");
    statusContainer.className = "status-container";

    const statusLabel = document.createElement("p");
    statusLabel.textContent = "Status gledanja:";
    statusContainer.appendChild(statusLabel);

    const statusSelect = document.createElement("select");
    const statuses = ["Odgledano", "Planirano", "Trenutno gledam", "Odustao"];
    statuses.forEach((status) => {
      const option = document.createElement("option");
      option.value = status;
      option.textContent = status;
      statusSelect.appendChild(option);
    });

    statusContainer.appendChild(statusSelect);

    const confirmStatusButton = document.createElement("button");
    confirmStatusButton.textContent = "Potvrdi";

    confirmStatusButton.addEventListener("click", async () => {
      if (!this.user || !this.user.id) {
        console.error("Greška: Korisnik nije definisan!");
        return;
      }

      const korisnikID = this.user.id;
      const filmID = movie.id;
      const status = statusSelect.value;

      console.log(
        `Id korisnika: ${korisnikID}, Id filma: ${filmID}, Status: ${status}`
      );

      const gledanjeFilmova = new GledanjeFilmova();
      const postoji = await gledanjeFilmova.proveriGledao(korisnikID, filmID); //bilo je this.proveriGledao
      console.log("Postoji " + postoji);

      if (postoji) {
        console.log("Film je već u bazi, ažuriraj status.");
        await gledanjeFilmova.azurirajStatus(korisnikID, filmID, status); //bilo this
      } else {
        console.log("Film nije u bazi, kreiraj novi unos.");
        await gledanjeFilmova.dodajGledao(korisnikID, filmID, status); //bilo this
      }
    });

    statusContainer.appendChild(confirmStatusButton);

    movieInfo.appendChild(statusContainer);

    const react = document.createElement("div");
    // Ocenjivanje
    const rateContainer = document.createElement("div");
    rateContainer.className = "rate-container";

    const rateLabel = document.createElement("p");
   
    rateContainer.appendChild(rateLabel);

    const rateButton = document.createElement("i");
    rateButton.className = "fa fa-star rate-button";
    rateButton.style.cursor = "pointer";

    // pozivanje funkcije za ocenjivanje kada je kliknuto
    rateButton.addEventListener("click", async () => {

      const gledanjeFilmova = new GledanjeFilmova();
      const postoji = await gledanjeFilmova.proveriGledao(this.user.id, movie.id); //bilo je this.proveriGledao
      console.log("Postoji " + postoji);

      if (!postoji) {
        console.log("Gledao nije u bazi, kreiraj novu vezu gledao.");
        await gledanjeFilmova.dodajGledao(this.user.id, movie.id, "Odgledano"); 
      }
      await ocenjivanjeFilma.showRatingModal(movie.id, this.user.id);
    });



    rateContainer.appendChild(rateButton);
    movieInfo.appendChild(rateContainer);

    const komentari = new Komentari(this.user);
    
    const commentButton = document.createElement("i");
    commentButton.className = "fa fa-comment comment-button";
    commentButton.style.cursor = "pointer";
    commentButton.addEventListener("click", async () => {
   
      await komentari.openComments(movie.id, this.user.id); 
    });

    rateContainer.appendChild(commentButton);

    console.log(this.user.id);
    const svidjanjeFilma = new SvidjanjeFilma(movie, this.user.id);
    const likeIcon = await svidjanjeFilma.createLikeIcon();
    rateContainer.appendChild(likeIcon);

    movieDetails.appendChild(movieInfo);
 
    this.container.appendChild(movieDetails);
  }
}
