export function errorMessage(page = "", container) {
  const errorMessage = document.createElement("div");
  errorMessage.classList.add("error-message");
  errorMessage.innerHTML = `❌ Failed to load page. ${page}.`;

  container.innerHTML = "";
  container.appendChild(errorMessage);
}
