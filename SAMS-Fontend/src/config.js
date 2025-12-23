// src/config.js
export const config = {
  apiBaseUrl: process.env.REACT_APP_API_BASE_URL,
  apiTimeout: Number(process.env.REACT_APP_API_TIMEOUT ?? 15000),
  environment: process.env.REACT_APP_ENVIRONMENT ?? "development",
  keycloak: {
    url: process.env.REACT_APP_KEYCLOAK_URL,
    realm: process.env.REACT_APP_KEYCLOAK_REALM,
    clientId: process.env.REACT_APP_KEYCLOAK_CLIENT_ID,
  },
};
