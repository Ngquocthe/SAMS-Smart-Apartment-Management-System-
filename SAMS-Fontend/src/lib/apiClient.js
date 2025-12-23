import axios from "axios";

const api = axios.create({
  baseURL: process.env.REACT_APP_API_BASE_URL
    ? `${process.env.REACT_APP_API_BASE_URL}/api`
    //: "https://noahbuilding.me/api",
    : "https://localhost:5000/api",
  timeout: parseInt(process.env.REACT_APP_API_TIMEOUT) || 15000,
});

api.interceptors.request.use(async (config) => {
  let token = localStorage.getItem("access_token");
  if (window.keycloak && window.keycloak.authenticated) {
    try {
      const isTokenExpired = window.keycloak.isTokenExpired(30);

      if (isTokenExpired) {
        const refreshed = await window.keycloak.updateToken(30);
        if (refreshed) {
          token = window.keycloak.token;
          localStorage.setItem("access_token", token);
        }
      } else {
        token = window.keycloak.token;
      }
    } catch (error) {
      console.error("❌ Error refreshing token:", error);
      window.keycloak.logout();
    }
  }

  config.headers = config.headers || {};
  config.headers.Accept = "application/json";

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  if (config.data instanceof FormData) {
    delete config.headers["Content-Type"];
  } else {
    config.headers["Content-Type"] = "application/json";
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const res = error.response;

    if (!res) {
      error.errorMessage = "Không có phản hồi từ máy chủ.";
      return Promise.reject(error);
    }

    const data = res.data;
    let message = "Đã xảy ra lỗi";

    if (typeof data === "string") {
      const clean = data.replace(/<[^>]+>/g, "");
      const line = clean
        .split("\n")
        .find((l) => /Exception|Lỗi|Keycloak/i.test(l));
      message = line?.trim() || clean.trim();
      message = extractErrorMessage(message);
    } else if (typeof data === "object") {
      message = data.message || data.error || data.detail || message;
    }

    error.errorMessage = message;
    return Promise.reject(error);
  }
);

function extractErrorMessage(str) {
  if (!str) return "";
  const clean = str.replace(/<[^>]+>/g, ""); // bỏ HTML nếu có
  const idx = clean.indexOf(":");
  if (idx !== -1) return clean.substring(idx + 1).trim();
  return clean.trim();
}

export default api;
