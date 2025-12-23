import React from 'react';
import { Navigate } from 'react-router-dom';
import { hasPermission, isValidRole } from '../utils/roleUtils';

const RoleGuard = ({ 
  children, 
  userRole, 
  requiredPermission, 
  fallbackPath = "/unauthorized" 
}) => {
  // Check if user has valid role
  if (!isValidRole(userRole)) {
    return <Navigate to={fallbackPath} replace />;
  }

  // Check if user has required permission
  if (requiredPermission && !hasPermission(userRole, requiredPermission)) {
    return <Navigate to={fallbackPath} replace />;
  }

  return children;
};

export default RoleGuard;
