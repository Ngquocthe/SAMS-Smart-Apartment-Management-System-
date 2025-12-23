import api from "../../lib/apiClient";

export const keycloakApi = {
  async getClientRoles() {
    const res = await api.get(`/keycloak/roles/client`);
    const payload = res.data ?? [];
    return Array.isArray(payload) ? payload : [];
  },
};
