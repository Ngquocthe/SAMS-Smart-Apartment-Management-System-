export function getUserRolesFromKeycloak(keycloak) {
  const realm = keycloak.tokenParsed?.realm_access?.roles ?? [];
  const backend = keycloak.tokenParsed?.resource_access?.backend?.roles ?? [];

  return Array.from(new Set([...realm, ...backend]));
}

export function canSeeRoute(routes, userRoles = []) {
  if (!Array.isArray(routes)) return [];

  const have = (Array.isArray(userRoles) ? userRoles : [userRoles])
    .filter(Boolean)
    .map((r) => String(r).toLowerCase());

  return routes.filter((route) => {
    const needRaw = route.roles ?? route.role ?? [];
    const need = (Array.isArray(needRaw) ? needRaw : [needRaw])
      .filter(Boolean)
      .map((r) => String(r).toLowerCase());

    if (need.length === 0) return true;

    return have.some((r) => need.includes(r));
  });
}
