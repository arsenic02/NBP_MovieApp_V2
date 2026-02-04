export class GledanjeFilmova {
    // constructor(user) {
    // }

    async proveriGledao(korisnikID, filmID) {
        try {
            const response = await fetch(`https://localhost:5001/gledao/proveri/${korisnikID}/${filmID}`);
            if (response.ok) {
                const data = await response.json();
                console.log("Film je gledan: " + data);
                return true; // Film je gledan
            } else {
                console.log("Film nije gledan.");
                return false; // Film nije gledan
            }
        } catch (error) {
            console.error("Greška prilikom provere:", error);
            return false;
        }
    }

    async azurirajStatus(korisnikID, filmID, status) {
        try {

            console.log("Unutar metode azuriraj status, korisnik ID: " + korisnikID, " film ID: " + filmID, "status: " + status);
            const response = await fetch(`https://localhost:5001/gledao/update`, {
                method: "PATCH",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    KorisnikID: korisnikID,
                    FilmID: filmID,
                    Status: status
                }),
            });

            const data = await response.json();
            if (response.ok) {
                console.log("Status uspešno ažuriran:", data);
            } else {
                console.error("Greška prilikom ažuriranja statusa:", data);
            }
        } catch (error) {
            console.error("Greška na serveru prilikom ažuriranja statusa:", error);
        }
    }

    async dodajGledao(korisnikID, filmID, status) {
        try {
            const response = await fetch(`https://localhost:5001/gledao/create`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    KorisnikID: korisnikID,
                    FilmID: filmID,
                    Status: status
                }),
            });

            const data = await response.json();
            if (response.ok) {
                console.log("Film uspešno dodat na listu odgledanih:");
            } else {
                console.error("Greška prilikom dodavanja filma:", data);
            }
        } catch (error) {
            console.error("Greška na serveru prilikom dodavanja filma:", error);
        }
    }
}