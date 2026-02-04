export class Komentari {
    constructor(user) {
        this.user = user;
    }

    async openComments(movieId) {
        const overlay = document.createElement("div");
        overlay.className = "modal-overlay";

        const modal = document.createElement("div");
        modal.className = "comments-modal";

        modal.innerHTML = `
        <div class="modal-content">
          <h3>Komentari za film</h3>
          <div class="comments-modal-body">
            <div id="commentsContainer" class="comments-container">
              <div class="loading">Učitavanje komentara...</div>
            </div>
            <div class="comment-form">
              <textarea id="newComment" placeholder="Napišite komentar..." rows="4"></textarea>
              <button id="submitComment">Postavi komentar</button>
            </div>
          </div>
          <button id="closeModal">Zatvori</button>
        </div>
      `;

        overlay.appendChild(modal);
        document.body.appendChild(overlay);

        await this.fetchComments(movieId);

        document.getElementById("closeModal").addEventListener("click", () => overlay.remove());

        overlay.addEventListener("click", (e) => {
            if (e.target === overlay) overlay.remove();
        });

        document.getElementById("submitComment").addEventListener("click", async () => {
            const newComment = document.getElementById("newComment").value.trim();
            if (newComment) {
                await this.addComment(movieId, this.user.id, newComment);
                await this.fetchComments(movieId);
                document.getElementById("newComment").value = "";
            } else {
                alert("Molimo unesite komentar.");
            }
        });
    }

    async fetchComments(movieId) {
        try {
            const response = await fetch(`https://localhost:5001/api/Komentar/getKomentariFilma/${movieId}`);
            if (!response.ok) throw new Error("Failed to fetch comments");
    
            const comments = await response.json();
            const commentsContainer = document.getElementById("commentsContainer");
            commentsContainer.innerHTML = "";
    
            if (comments.length === 0) {
                commentsContainer.innerHTML = "<p class='no-comments'>Nema komentara.</p>";
            } else {
                // Za svaki komentar, uzmi korisničko ime iz baze
                //promise.all znaci da istovremeno se dohvataju svi korinici umesto da se prave serijiski zahtevi
                const commentsWithUsers = await Promise.all(comments.map(async (comment) => {
                    const userResponse = await fetch(`https://localhost:5001/User/${comment.userId}`);
                    if (userResponse.ok) {
                        const user = await userResponse.json();
                        comment.korisnickoIme = user.korisnickoIme;
                    } else {
                        comment.korisnickoIme = "Nepoznat korisnik";
                    }
                    return comment;
                }));
    
                commentsWithUsers.forEach((comment) => {
                    const commentElement = document.createElement("div");
                    commentElement.className = "comment";
                    commentElement.innerHTML = `
                        <div class="comment-author"><strong>${comment.korisnickoIme}</strong></div>
                        <div class="comment-text">${comment.text}</div>`;
                    commentsContainer.appendChild(commentElement);
                });
            }
        } catch (error) {
            console.error("Greška pri pribavljanju komentara:", error);
            alert("Došlo je do greške prilikom pribavljanja komentara.");
        }
    }
    

    async addComment(movieId, userId, commentText) {
        try {
            const response = await fetch(`https://localhost:5001/api/Komentar/createComment`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    userId,
                    movieId,
                    text: commentText,
                }),
            });

            if (!response.ok) throw new Error("Failed to add comment");
        } catch (error) {
            console.error("Greška pri dodavanju komentara:", error);
            alert("Došlo je do greške prilikom dodavanja komentara.");
        }
    }
}
