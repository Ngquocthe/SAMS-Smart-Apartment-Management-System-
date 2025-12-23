// Function to filter routes by user role
export const filterRoutesByRole = (routes, userRole) => {
  if (!routes || !Array.isArray(routes)) {
    return [];
  }

  return routes.filter((route) => {
    const roles = Array.isArray(route.role)
      ? route.role
      : route.role
      ? [route.role]
      : [];

    if (route.private && roles.length === 0) {
      return false;
    }

    if (!route.private) return true;

    return roles.includes(userRole);
  });
};

// Function to check if user has access to a specific route
export const hasRouteAccess = (routeKey, userRole, routes) => {
  const route = routes.find((r) => r.key === routeKey);
  if (!route) {
    return false;
  }

  return filterRoutesByRole([route], userRole).length > 0;
};

// Function to get default route for a role
export const getDefaultRouteForRole = (userRole) => {
  const defaultRoutes = {
    admin: "/admin/dashboard",
    "building-manager": "/building-manager/dashboard",
    receptionist: "/receptionist/documents",
    resident: "/resident/dashboard",
    security: "/security/dashboard",
  };

  return defaultRoutes[userRole] || "/";
};

// import { Navigate, Outlet, useLocation } from "react-router-dom";

// export default function PrivateRoutes({ isAuthenticated, userRole = "building-manager" }) {
//   const location = useLocation();

//   if (!isAuthenticated) {
//     return <Navigate to="/login" replace state={{ from: location }} />;
//   }
//   return <Outlet />;
// }

// // Role-based route filter
// export function filterRoutesByRole(routes, userRole) {
//   return routes.filter(route => {
//     if (route.role === "all") return true;
//     return route.role === userRole;
//   });
// }
