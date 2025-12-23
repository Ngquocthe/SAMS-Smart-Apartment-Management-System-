// src/keycloak/initKeycloak.js
import Keycloak from "keycloak-js";
import { config } from "../config";
import useUserStore from "../stores/userStore";

export const initKeycloakOption = {
  url: config.keycloak.url || "https://auth.noahbuilding.me",
  realm: config.keycloak.realm || "NOAH",
  clientId: config.keycloak.clientId || "frontend",
};

export const keycloak = new Keycloak(initKeycloakOption);

const _logout = keycloak.logout.bind(keycloak);
keycloak.logout = (options = {}) => {
  const home =
    typeof window !== "undefined" ? `${window.location.origin}/` : "/";
  return _logout({ redirectUri: home, ...options });
};

keycloak.onAuthLogout = () => {
  try {
    sessionStorage.removeItem("kc_login_intent");
    sessionStorage.removeItem("kc_just_logged_in");
    localStorage.removeItem("access_token");
    const st = useUserStore.getState ? useUserStore.getState() : null;
    st && st.clearUser && st.clearUser();
  } finally {
    window.location.replace(`${window.location.origin}/`);
  }
};

if (typeof window !== "undefined") {
  window.keycloak = keycloak;
}

let hasInit = false;

keycloak.onAuthSuccess = () => {
  sessionStorage.setItem("kc_just_logged_in", "1");
};

export function initKeycloak() {
  if (
    hasInit ||
    (typeof window !== "undefined" && window.__KC_INIT__ === true)
  ) {
    return Promise.resolve(!!keycloak.authenticated);
  }

  return keycloak
    .init({
      onLoad: "check-sso",
      checkLoginIframe: false,
      pkceMethod: "S256",
    })
    .then((authenticated) => {
      hasInit = true;
      if (typeof window !== "undefined") window.__KC_INIT__ = true;

      if (authenticated) {
        if (keycloak.token) {
          localStorage.setItem("access_token", keycloak.token);
        }

        const userInfo = keycloak.tokenParsed;
        if (userInfo?.sub) {
          const { fetchUserData } = useUserStore.getState();
          fetchUserData(userInfo.sub);
        }

        setupTokenRefresh();
      }

      return authenticated;
    })
    .catch((error) => {
      throw error;
    });
}

function setupTokenRefresh() {
  const refresh = async () => {
    try {
      const refreshed = await keycloak.updateToken(30);
      if (refreshed && keycloak.token) {
        localStorage.setItem("access_token", keycloak.token);
      }
    } catch (e) {
      console.warn("Token refresh failed, logging out", e);
      keycloak.logout();
    }
  };

  keycloak.onTokenExpired = refresh;

  if (typeof window !== "undefined") {
    if (window.__KC_REFRESH_INTERVAL__) {
      clearInterval(window.__KC_REFRESH_INTERVAL__);
    }
    window.__KC_REFRESH_INTERVAL__ = setInterval(refresh, 60 * 1000);
  }
}
