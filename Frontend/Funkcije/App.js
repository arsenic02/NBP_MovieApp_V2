import { User } from "./User.js";
import { Movie } from "./Movie.js";
import { Home } from "./Home.js";
import { MyMovies } from "./MyMovies.js";
import { ListeKojePratim } from "./ListeKojePratim.js";
import { MovieInfo } from "./MovieInfo.js";
import { PreporuceneListe } from "./PreporuceneListe.js";
export class App {
  constructor(container, result, poruke) {
    this.user = new User(result.id, result.korisnickoIme, result.mejl);
   
    this.container = container;
    this.createTopBar();

    this.mainContent = document.createElement("div");
    this.mainContent.className = "main-content";
    container.appendChild(this.mainContent);

    this.renderAllMovies();
    this.poruke = poruke;
  }

  createTopBar() {
  
    const topBar = document.createElement("div");
    topBar.className = "top-bar";

    const logo = document.createElement("div");
    logo.className = "logo";
    logo.textContent = "🎬 MovieApp";
 
    topBar.appendChild(logo);

    const navLinks = document.createElement("nav");
    navLinks.className = "nav-links";

    const homeLink = document.createElement("div");
    homeLink.className = "dropdown";
    const homeAnchor = document.createElement("a");
    homeAnchor.href = "#home";
    homeAnchor.textContent = "Home";
    homeAnchor.className = "dropdown-toggle";

    const dropdownMenu = document.createElement("div");
    dropdownMenu.className = "dropdown-menu";

    const subLinks = [
      {
        text: "Svi filmovi",
        href: "#all-movies",
        action: () => this.renderAllMovies(),
      },
      {
        text: "Preporučeni filmovi",
        href: "#recomended-movies",
        action: () => this.renderRecommendedMovies(),
      },
      {
        text: "Preporučene liste",
        href: "#recomended-lists",
        action: () => this.renderRecommendedLists(),
      },
    ];

    subLinks.forEach((subLink) => {
      const subAnchor = document.createElement("a");
      subAnchor.href = subLink.href;
      subAnchor.textContent = subLink.text;

      subAnchor.addEventListener("click", (event) => {
        event.preventDefault();
        subLink.action();
      });

      dropdownMenu.appendChild(subAnchor);
    });

    
    homeLink.appendChild(homeAnchor);
    homeLink.appendChild(dropdownMenu);
    navLinks.appendChild(homeLink);

    const links = [
      {
        text: "Moji Filmovi",
        href: "#moji-filmovi",
        action: () => this.renderMyMoviesPage(),
      },
      {
        text: "Liste koje pratim",
        href: "#liste",
        action: () => this.renderFollowedListsPage(),
      },
    ];

    links.forEach((link) => {
      const divA = document.createElement("div");
      divA.classList.add("divA");
      const a = document.createElement("a");
      a.href = link.href;
      a.textContent = link.text;
      divA.appendChild(a);
      // Dodavanje akcije za klik
      a.addEventListener("click", (event) => {
        event.preventDefault();
        link.action();
      });
      navLinks.appendChild(divA);
    });
    topBar.appendChild(navLinks);

    const searchContainer = document.createElement("div");
    searchContainer.className = "search-container";

    const searchInput = document.createElement("input");
    searchInput.type = "text";
    searchInput.placeholder = "🔍 Pretraži filmove...";
    searchInput.className = "search-input";

    const suggestionsList = document.createElement("div");
    suggestionsList.className = "search-suggestions";

    searchContainer.appendChild(searchInput);
    searchContainer.appendChild(suggestionsList);
    topBar.appendChild(searchContainer);

    // Prikaz Inboxa
    const korisnikDiv = document.createElement("nav");
    korisnikDiv.className = "nav-links";
    const inbox = document.createElement("a");
    inbox.classList.add("nav-links");
    inbox.href = "#inbox";
    inbox.textContent = " ✉ Inbox";
    inbox.action = () => this.renderInbox(this.poruke);
    inbox.addEventListener("click", (event) => {
      event.preventDefault();
      inbox.action();
    });
    korisnikDiv.appendChild(inbox);

    //Prikaz korisnickog imena
    const korisnikProfil = document.createElement("p");
    korisnikProfil.textContent = `👤 ${this.user.KorisnickoIme}`;
    korisnikProfil.className = "korisnik-profil";
    korisnikDiv.appendChild(korisnikProfil);
    korisnikDiv.classList.add("NavbarUser");
    topBar.appendChild(korisnikDiv);

    // Log Out dugme
    const logoutLink = document.createElement("a");
    logoutLink.href = "#";
    logoutLink.textContent = "🚪 Log Out";
    korisnikDiv.appendChild(logoutLink);
    logoutLink.addEventListener("click", (event) => {
      event.preventDefault();
      this.logoutUser();
    });

    this.container.prepend(topBar);

    this.setupSearch(searchInput, suggestionsList);
  }

  setupSearch(searchInput, suggestionsList) {
    let debounceTimeout = null;

    searchInput.addEventListener("input", () => {
      clearTimeout(debounceTimeout);

      const query = searchInput.value.trim();
      if (query.length < 2) {
        suggestionsList.innerHTML = "";
        return;
      }

      debounceTimeout = setTimeout(async () => {
        const suggestions = await this.fetchMovieSuggestions(query);
        this.displaySuggestions(suggestions, suggestionsList, searchInput);
      }, 300);
    });

    document.addEventListener("click", (event) => {
      if (
        !suggestionsList.contains(event.target) &&
        event.target !== searchInput
      ) {
        suggestionsList.innerHTML = "";
      }
    });
  }
  async fetchMovieSuggestions(query) {
    try {
      const response = await fetch(
        `https://localhost:5001/Film/autocomplete/${query}`
      );
      // console.log(await response.json());
      if (!response.ok) {
        return [];
      }
      return await response.json();
    } catch (error) {
      return [];
    }
  }

  displaySuggestions(suggestions, suggestionsList, searchInput) {
    suggestionsList.innerHTML = "";

    if (suggestions.length === 0) {
      return;
    }

    suggestions.forEach((movie) => {
      const suggestionItem = document.createElement("div");
      suggestionItem.className = "suggestion-item";
      suggestionItem.textContent = movie; 

      suggestionItem.addEventListener("click", () => {
        searchInput.value = movie;
        suggestionsList.innerHTML = "";
        this.renderMovieDetails(movie); 
      });

      suggestionsList.appendChild(suggestionItem);
    });
  }
  async renderMovieDetails(movieName) {
    try {
      const result = await fetch(
        `https://localhost:5001/Film/by-title/${movieName}`
      );
      if (!result.ok) {
        throw new Error("Ne postoji film sa tim naslovom! ");
      }
      const movie = await result.json();
      console.log(movie);
      const MovieJS = new Movie(
        movie.id,
        movie.naslov,
        movie.godinaIzlaska,
        movie.zanr,
        movie.prosecnaOcena,
        movie.kategorija,
        movie.slikaURL
      );
      await MovieJS.fetchMovieImage();
      const movieInfo = new MovieInfo(this.mainContent, this.user);
      await movieInfo.openMovieDetails(MovieJS);
    } catch (ex) {
      console.error(ex);
    }
  }
  logoutUser() {
    console.log("Odjavljivanje korisnika...");
    window.location.reload(); 
  }

  renderHomePage() {
    this.clearMainContent();
    const homePage = new Home(this.mainContent, this.user);
    homePage.renderHomePage();
  }

  renderMyMoviesPage() {
    this.clearMainContent();
    const myMovies = new MyMovies(this.mainContent, this.user);
    myMovies.fetchWatchedMovies();
  }

 
  renderFollowedListsPage() {
    this.clearMainContent();
    const followedListsPage = document.createElement("div");
    const LKP = new ListeKojePratim(followedListsPage, this.user);
    LKP.renderLKPPage();
    this.mainContent.appendChild(followedListsPage);
  }

  renderInbox(poruke) {
    this.clearMainContent();
    const Inbox = document.createElement("div");
    Inbox.className = "inbox-container";

    poruke.forEach((p) => {
      const messageContainer = document.createElement("div");
      messageContainer.className = "poruka-container";
      const por = document.createElement("p");
      por.textContent = `${p}`;
      messageContainer.appendChild(por);
      Inbox.appendChild(messageContainer);
    });
    this.mainContent.appendChild(Inbox);
  }

  clearMainContent() {
    this.mainContent.innerHTML = "";
  }

  renderAllMovies() {
    this.clearMainContent();

    const title = document.createElement("h2");
    title.textContent = "Svi Filmovi";
    this.mainContent.appendChild(title);
    
    const moviesContainer = document.createElement("div");
    moviesContainer.className = "movies-container";
    this.mainContent.appendChild(moviesContainer);

    const home = new Home(moviesContainer, this.user);
    home.renderHomePage();
  }

  renderRecommendedMovies() {
    this.clearMainContent();

    const title = document.createElement("h2");
    title.textContent = "Preporučeni filmovi";
    this.mainContent.appendChild(title);

    const recommendedMoviesPage = document.createElement("div");
    recommendedMoviesPage.className = "movies-container";
    this.mainContent.appendChild(recommendedMoviesPage);

    const home = new Home(recommendedMoviesPage, this.user);
    home.renderHomePage(
      `https://localhost:5001/Film/recommendations?korisnikID=${this.user.id}&`
    );
  }

  async renderRecommendedLists() {
    this.clearMainContent();

    const recommendedListsPage = document.createElement("div");
    recommendedListsPage.classList.add("recommended-lists-container");

    this.mainContent.appendChild(recommendedListsPage);

    const preporuceneListe = new PreporuceneListe(
      recommendedListsPage,
      this.user
    );
    await preporuceneListe.renderRecommendedLists(1, 10);
  }
}
