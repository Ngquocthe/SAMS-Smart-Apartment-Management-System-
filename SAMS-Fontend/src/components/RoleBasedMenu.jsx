import React from 'react';
import { filterRoutesByRole } from '../routers/PrivateRoutes';
import CONFIG_ROUTER from '../routers/ConfigRoutes';

const RoleBasedMenu = ({ userRole = 'building-manager' }) => {
  const filteredRoutes = filterRoutesByRole(CONFIG_ROUTER, userRole);

  return (
    <nav>
      <ul>
        {filteredRoutes.map((route) => (
          <li key={route.key}>
            <a href={route.path}>
              {route.icon && <route.icon />}
              {route.menuName}
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
};

export default RoleBasedMenu;
