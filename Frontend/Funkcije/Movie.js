export class Movie {
  constructor(id, naslov, godina, zanr, prOcena, kategorija, url) {
    this.id = id;
    this.naslov = naslov;
    this.godina = godina;
    this.zanr = zanr;
    this.prOcena = prOcena;
    this.kategorija = kategorija;
    this.url = url;
    this.Slika;
    this.Status;
    this.DPS; //Datum Promene Statusa
  }

  async fetchMovieImage() {
    console.log(this.naslov);
    console.log(this.godina);
    const apiUrl = `https://localhost:5001/Film/get-image/${this.naslov}/${this.godina}`;
    // Fetch sliku sa backend-a
    this.Slika = await fetch(apiUrl)
      .then((response) => {
        if (!response.ok) {
          throw new Error(`Image not found. ${this.naslov}, ${this.godina}`);
        }
        return response.blob(); // pretvara odgovor u blob (binarni podatak)
      })
      .then((blob) => {
        this.url = URL.createObjectURL(blob); // kreira URL za sliku iz blob-a
      })
      .catch((error) => {
        console.error("Error fetching image:", error);
      });
  }
  setParam(status, dps) {
    this.Status = status;
    this.DPS = dps;
  }
}
