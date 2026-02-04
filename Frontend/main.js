import { App } from "./Funkcije/App.js";
//Login
//Register
let div = document.createElement("div");
div.classList.add("ContainerFoForm");

let form = createForm("login");
div.appendChild(form);
document.body.appendChild(div);

function createForm(type) {
  
  const formContainer = document.createElement("div");
  formContainer.className = "form-container";

  const title = document.createElement("h2");
  title.textContent = type === "login" ? "Login Form" : "Register Form";
  formContainer.appendChild(title);

  const usernameLabel = document.createElement("label");
  usernameLabel.textContent = "Username:";
  usernameLabel.setAttribute("for", "username");
  formContainer.appendChild(usernameLabel);

  const usernameInput = document.createElement("input");
  usernameInput.type = "text";
  usernameInput.id = "username";
  usernameInput.placeholder = "Unesite korisničko ime";
  usernameInput.className = "form-input-username";
  formContainer.appendChild(usernameInput);

  let email, username;
  if (type === "register") {
    
    const emailLabel = document.createElement("label");
    emailLabel.textContent = "Email:";
    emailLabel.setAttribute("for", "email");
    formContainer.appendChild(emailLabel);

    const emailInput = document.createElement("input");
    emailInput.type = "email";
    emailInput.id = "email";
    emailInput.placeholder = "Unesite email";
    emailInput.className = "form-input-email";
    formContainer.appendChild(emailInput);
    
  }

  const submitButton = document.createElement("button");
  submitButton.textContent = type === "login" ? "Prijava" : "Registracija";
  submitButton.className = "form-button";
  if (type == "login") {
    submitButton.onclick = () => LoginUserClick();
  } else {
    submitButton.onclick = () => RegisterUserClick();
  }
  formContainer.appendChild(submitButton);

  const toggleButton = document.createElement("button");
  toggleButton.textContent = type === "login" ? "Register" : "Login";
  toggleButton.className = "form-toggle";
  toggleButton.addEventListener("click", () => ToggleClick(type));
  formContainer.appendChild(toggleButton);

  
  return formContainer;
}

function ToggleClick(currentType) {
  const newFormType = currentType === "login" ? "register" : "login";
  
  let oldForm = document.querySelector(".form-container");
  let parent = document.querySelector(".ContainerFoForm");
  const newForm = createForm(newFormType);

  parent.replaceChild(newForm, oldForm);
}

async function RegisterUserClick() {
  let email = document.querySelector(".form-input-email").value;
  let username = document.querySelector(".form-input-username").value;

  console.log("Usao u registraciju...");

  try {
    let response = await fetch("https://localhost:5001/User", {
      method: "POST",
      body: JSON.stringify({
        korisnickoIme: username,
        mejl: email,
      }),
      headers: {
        "Content-Type": "application/json",
      },
    });

    let result = await response.json();
    console.log(result);

    if (response.ok) {
      console.log("Registracija uspešna!");
      
      let userResponse = await fetch(`https://localhost:5001/User/by-username/${username}`);
      let userData = await userResponse.json();

      console.log(userData);

      while (document.body.firstChild) {
        document.body.removeChild(document.body.firstChild);
      }

      let korisnikData = userData.korisnik;
      let poruke = userData.poruke;
      let home = new App(document.body, korisnikData, poruke);
    } else {
      console.error("Greška pri registraciji:", result);
      alert(result.Message || "Neuspešna registracija!");
    }
  } catch (error) {
    console.error("Došlo je do greške:", error);
    alert("Greška pri registraciji. Pokušajte ponovo.");
  }
}

async function LoginUserClick() {
  let username = document.querySelector(".form-input-username").value;
  console.log(username);
  let res = await fetch(
    `https://localhost:5001/User/by-username/${username}`
  ).then((res) => res.json());

  console.log(res);

  while (document.body.firstChild) {
    document.body.removeChild(document.body.firstChild);
  }
  let korisnikData = res.korisnik;
  let poruke = res.poruke;
  let home = new App(document.body, korisnikData, poruke);
}
