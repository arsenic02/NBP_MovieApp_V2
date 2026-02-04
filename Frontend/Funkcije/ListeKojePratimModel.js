import { Movie } from "./Movie.js";

export class ListaKojuPratimModel {
  constructor(id, ime, movies) {
    this.vlasnikID = id;
    this.ime = ime;
    this.filmovi = movies.map((m) => {
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
  }
  async fetchMovieImage() {
    if (this.filmovi.length > 0) {
      for (let i = 0; i < this.filmovi.length; i++) {
        await this.filmovi[i].fetchMovieImage();
      }
    }
  }
}
