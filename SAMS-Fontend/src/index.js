// src/index.js
import ReactDOM from "react-dom/client";
import "./index.css";
import App from "./App";
import reportWebVitals from "./reportWebVitals";
import "@fortawesome/fontawesome-free/css/all.min.css";
import { initKeycloak } from "./keycloak/initKeycloak";

function getRoot() {
  const container = document.getElementById("root");
  if (!window.__APP_ROOT__) {
    window.__APP_ROOT__ = ReactDOM.createRoot(container);
  }
  return window.__APP_ROOT__;
}

initKeycloak().then(() => {
  const root = getRoot();
  root.render(<App />);
});

reportWebVitals();
