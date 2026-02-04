import { GledanjeFilmova } from "./GledanjeFilmova.js";

export class SvidjanjeFilma {
    constructor(movie, korisnikID) {
        this.movie = movie;
        this.korisnikID = korisnikID;
    }
    
    async createLikeIcon() {
        const likeIcon = document.createElement("i");
        likeIcon.style.cursor = "pointer";
        likeIcon.style.color = "red";

        const lajkovano = await this.proveriDaLiJeLajkovao(this.korisnikID, this.movie.id);
        console.log(`Film ${this.movie.id} lajkovan od ${this.korisnikID}:`, lajkovano);

        likeIcon.className = lajkovano ? "fa fa-heart like-icon" : "fa fa-regular fa-heart";
        console.log("Početna klasa:", likeIcon.className);//fa fa-heart like-icon je puno srce, fa fa-regular fa-heart je prazno srce
       
        likeIcon.addEventListener("click", async () => {
            console.log("Pre klika, klasa je:", likeIcon.className);
            if (likeIcon.classList.contains("fa-heart") && !likeIcon.classList.contains("fa-regular")) {
                console.log("Brisanje lajka...");
                await this.obrisiSvidjaMuSe();
                likeIcon.className = "fa fa-regular fa-heart";
            } else {
                console.log("Dodavanje lajka...");
                await this.handleLike();
                likeIcon.className = "fa fa-heart like-icon"; // Prikaz punog srca
            }
        });

        return likeIcon;
    }


    async handleLike() {
        console.log(`Proveravam da li je korisnik (${this.korisnikID}) gledao film ${this.movie.id}...`);
        const gledanjeFilmova = new GledanjeFilmova();
        const jeGledao = await gledanjeFilmova.proveriGledao(this.korisnikID, this.movie.id);

        if (!jeGledao) {
            console.log("Korisnik nije gledao film, dodavanje u odgledane...");
            await gledanjeFilmova.dodajGledao(this.korisnikID, this.movie.id, "Odgledan");
        }

        console.log("Dodavanje filma u omiljene...");
        await this.dodajSvidjaMuSe();
    }
    
    async dodajSvidjaMuSe() {
        try {            
            const overlay = document.createElement("div");
            overlay.classList.add("modal-container-svidjanje");
    
            const modalSvidjanje = document.createElement("div");
            modalSvidjanje.classList.add("modal-svidjanje");
    
            const text = document.createElement("p");
            text.textContent = "Da li želite da ovaj film bude javan?";
    
            const btnPublic = document.createElement("button");
            btnPublic.textContent = "Javni";
            btnPublic.classList.add("btn-public");
    
            const btnPrivate = document.createElement("button");
            btnPrivate.textContent = "Privatni";
            btnPrivate.classList.add("btn-private");
                
            modalSvidjanje.appendChild(text);
            modalSvidjanje.appendChild(btnPublic);
            modalSvidjanje.appendChild(btnPrivate);
            overlay.appendChild(modalSvidjanje);
            document.body.appendChild(overlay);
               
            const izbor = await new Promise((resolve) => {
                btnPublic.addEventListener("click", () => {
                    document.body.removeChild(overlay);
                    resolve(true);
                });
    
                btnPrivate.addEventListener("click", () => {
                    document.body.removeChild(overlay);
                    resolve(false);
                });
            });
               
            const response = await fetch(`https://localhost:5001/SvidjaMuSe/svidjaMuSe`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    KorisnikID: this.korisnikID,
                    FilmID: this.movie.id,
                    javna: izbor,
                }),
            });
    
            const data = await response.json();
            if (response.ok) {
                console.log("Film uspešno dodat u omiljene:", data);
            } else {
                console.error("Greška pri dodavanju u omiljene:", data);
                throw new Error(data.Message);
            }
        } catch (error) {
            console.error("Greška na serveru pri dodavanju u omiljene:", error);
        }
    }
    
    async proveriDaLiJeLajkovao(korisnikID, filmID) {
        try {
            const response = await fetch(`https://localhost:5001/SvidjaMuSe/${korisnikID}/${filmID}`);
            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.Message);
            }

            const data = await response.json();
            console.log("Da li je lajkovano:", data.lajkovano);

            return data.lajkovano;
        } catch (error) {
            console.error("Greška pri proveri lajka:", error.message);
            return false;
        }
    }

    async obrisiSvidjaMuSe() {
        try {
            const response = await fetch(`https://localhost:5001/SvidjaMuSe?korisnikID=${this.korisnikID}&filmID=${this.movie.id}`, {
                method: "DELETE",
                headers: {
                    "Content-Type": "application/json",
                }             
            });

            const data = await response.json();
            if (response.ok) {
                console.log("Film uspešno uklonjen iz omiljenih:", data);
            } else {
                console.error("Greška pri uklanjanju iz omiljenih:", data);
                return response.json().then(err => { throw new Error(err.Message); });
            }
        } catch (error) {
            console.error("Greška na serveru pri uklanjanju iz omiljenih:", error);
        }
    }
}
