import React, { useMemo, useState } from "react";
import { NavLink, useLocation, matchPath } from "react-router-dom";
import { useLanguage } from "../hooks/useLanguage";
import CONFIG_ROUTER from "../routers/ConfigRoutes";
import { getUserRolesFromKeycloak, canSeeRoute } from "../utils/auth";
import { keycloak } from "../keycloak/initKeycloak";

export default function Sidebar({
  isOpen,
  onToggle,
  isMobile,
  userRoles: userRolesProp,
}) {
  const { strings } = useLanguage();
  const { pathname } = useLocation();
  const userRoles = userRolesProp ?? getUserRolesFromKeycloak(keycloak);

  const PRIORITY = [
    "global_admin",
    "admin",
    "building-manager",
    "receptionist",
    "resident",
    "security",
  ];
  const mainRole = userRoles?.find((r) => PRIORITY.includes(r)) ?? "user";

  const getRoleLabel = (role) => {
    const map = {
      admin: strings.admin || "Admin",
      "building-manager": strings.buildingManager || "Building Manager",
      receptionist: "Receptionist",
      resident: strings.resident || "Resident",
      security: strings.security || "Security",
      global_admin: "Global Admin",
      user: "User",
    };
    return map[role] || role;
  };

  const getMenuName = (menuKey) => {
    const menuNames = {
      // Admin
      ADMIN_DASHBOARD: strings.dashboard,
      ADMIN_USERS: strings.userManagement,
      ADMIN_BUILDINGS: strings.buildings,
      ADMIN_REPORTS: strings.reports,
      ADMIN_SETTINGS: strings.systemSettings,
      // Building Manager
      BUILDING_MANAGER_DASHBOARD: strings.dashboard,
      USER_PROFILE: strings.userProfile || "Thông tin cá nhân",
      ANNOUNCEMENTS: strings.announcements,
      FLOORS: strings.floors,
      BUILDINGS: strings.buildings,
      APARTMENTS: strings.apartments,
      AMENITIES: strings.amenities,
      CARDS: strings.cards,
      ASSETS: strings.assets,
      FEES: strings.fees,
      BUILDING_MANAGER_DOCUMENTS: "Quản lý tài liệu",
      REPORTS: strings.reports,
      SETTINGS: strings.settings,
      SERVICE_TYPES: "Loại dịch vụ",
      ACCOUNTANT_INVOICES: "Hoá đơn",
      ACCOUNTANT_RECEIPTS: "Biên lai",
      ACCOUNTANT_VOUCHERS: "Phiếu chi",

      // Receptionist menus
      RECEPTIONIST_DASHBOARD: "Dashboard",
      RECEPTIONIST_DOCUMENTS: "Documents",
      RECEPTIONIST_DOCUMENTS_MANAGEMENT: "Quản lý tài liệu",
      RECEPTIONIST_FACE_CHECKIN: "Face Check-in",
      RECEPTIONIST_RESIDENTS: "Residents",
      RECEPTIONIST_REQUESTS: "Requests",
      RECEPTIONIST_REPORTS: "Reports",
      RECEPTIONIST_ANNOUNCEMENTS: "Quản lý tin tức",
      RECEPTIONIST_MAINTENANCE_SCHEDULES: "Quản lý lịch bảo trì",
      RECEPTIONIST_RECEIPT_CREATE: "Thu tiền tại quầy",
      // Resident
      RESIDENT_DASHBOARD: "Dashboard",
      RESIDENT_PROFILE: "Profile",
      RESIDENT_REQUESTS: "My Requests",
      RESIDENT_DOCUMENTS: "Tài liệu",
      // Security
      SECURITY_DASHBOARD: "Dashboard",
      SECURITY_VISITORS: "Visitors",
    };
    return menuNames[menuKey] || menuKey;
  };

  const itemVisibleForUser = (item, roles) => {
    if (item.show === false) return false;
    if (item.private && (!roles || roles.length === 0)) return false;
    // Hỗ trợ cả item.role (số ít) và item.roles (số nhiều)
    const itemRoles = item.roles || item.role;
    if (Array.isArray(itemRoles) && itemRoles.length > 0) {
      if (!roles?.some((r) => itemRoles.includes(r))) return false;
    }
    if (item.path) {
      const can = canSeeRoute([item], roles);
      return Array.isArray(can) ? can.length > 0 : !!can;
    }
    return true;
  };

  const filterAccessibleMenus = (menus, roles) => {
    return menus
      .filter((m) => m.private)
      .map((m) => {
        const hasChildren = Array.isArray(m.children) && m.children.length > 0;
        if (!hasChildren) {
          return itemVisibleForUser(m, roles) ? m : null;
        }
        const filteredChildren = filterAccessibleMenus(m.children, roles);
        const parentVisible = itemVisibleForUser(m, roles);
        if (!parentVisible && filteredChildren.length === 0) return null;
        return { ...m, children: filteredChildren };
      })
      .filter(Boolean);
  };

  const menus = useMemo(() => {
    const filtered = filterAccessibleMenus(CONFIG_ROUTER, userRoles);
    // Sắp xếp để Dashboard luôn ở đầu
    const dashboardItem = filtered.find(
      (m) => m.key === "BUILDING_MANAGER_DASHBOARD"
    );
    const otherItems = filtered.filter(
      (m) => m.key !== "BUILDING_MANAGER_DASHBOARD"
    );
    return dashboardItem ? [dashboardItem, ...otherItems] : filtered;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userRoles, pathname]);

  const isItemActive = (item) => {
    const paths = [];

    const collect = (node) => {
      if (!node) return;
      if (node.path) paths.push(node.path);
      if (Array.isArray(node.extraActivePaths))
        paths.push(...node.extraActivePaths);
      if (Array.isArray(node.children)) node.children.forEach(collect);
    };

    collect(item);

    return paths.some((p) =>
      p ? !!matchPath({ path: p, end: false }, pathname) : false
    );
  };

  const MenuLeaf = ({ item }) => {
    const Icon = item.icon;

    const leafActiveLike = useMemo(() => isItemActive(item), [item]);

    return (
      <NavLink
        to={item.path}
        className={
          `flex items-center gap-1.5 px-2 py-2 rounded-md transition-all duration-200 group ` +
          (leafActiveLike
            ? "bg-gray-200 text-gray-800"
            : "text-white hover:bg-slate-700")
        }
        end
      >
        <div
          className={
            `flex items-center justify-center w-7 h-7 rounded-md transition-all duration-200 flex-shrink-0 ` +
            (leafActiveLike
              ? "bg-gray-300"
              : "bg-slate-600 group-hover:bg-slate-500")
          }
        >
          {Icon ? (
            <Icon
              size={16}
              style={{ color: leafActiveLike ? "#374151" : "#cbd5e1" }}
            />
          ) : null}
        </div>
        <span className="font-medium text-sm flex-1 leading-tight">
          {item.menuName ?? getMenuName(item.key) ?? item.key}
        </span>
      </NavLink>
    );
  };

  const ParentToggle = ({ item, open, onToggle }) => {
    const Icon = item.icon;
    const activeLike = useMemo(() => isItemActive(item), [item]);
    return (
      <button
        type="button"
        onClick={onToggle}
        className={`w-full flex items-center gap-1.5 px-2 py-2 rounded-md transition-all duration-200 text-left group
          ${
            activeLike
              ? "bg-gray-200 text-gray-800"
              : "text-white hover:bg-slate-700"
          }`}
        aria-expanded={open}
      >
        <div
          className={`flex items-center justify-center w-7 h-7 rounded-md transition-all duration-200 flex-shrink-0
            ${
              activeLike
                ? "bg-gray-300"
                : "bg-slate-600 group-hover:bg-slate-500"
            }`}
        >
          {Icon ? (
            <Icon
              size={16}
              style={{ color: activeLike ? "#374151" : "#cbd5e1" }}
            />
          ) : null}
        </div>
        <span className="font-medium text-sm flex-1 leading-tight">
          {item.menuName ?? getMenuName(item.key) ?? item.key}
        </span>
        {/* chevron */}
        <svg
          className={`w-4 h-4 transition-transform duration-200 ${
            open ? "rotate-180" : ""
          } ${activeLike ? "text-gray-700" : "text-slate-300"}`}
          viewBox="0 0 20 20"
          fill="currentColor"
        >
          <path
            fillRule="evenodd"
            d="M5.23 7.21a.75.75 0 011.06.02L10 10.94l3.71-3.71a.75.75 0 111.06 1.06l-4.24 4.24a.75.75 0 01-1.06 0L5.21 8.29a.75.75 0 01.02-1.08z"
            clipRule="evenodd"
          />
        </svg>
      </button>
    );
  };

  const MenuBranch = ({ item }) => {
    const activeLike = isItemActive(item);
    const [open, setOpen] = useState(activeLike);

    const hasChildren =
      Array.isArray(item.children) && item.children.length > 0;

    if (!hasChildren) return item.path ? <MenuLeaf item={item} /> : null;

    return (
      <div className="space-y-1">
        <ParentToggle
          item={item}
          open={open}
          onToggle={() => setOpen((o) => !o)}
        />
        {open && (
          <ul className="mt-1 ml-3 pl-2 border-l border-slate-700 space-y-1">
            {item.children.map((child) => (
              <li key={child.key}>
                <MenuBranch item={child} />
              </li>
            ))}
          </ul>
        )}
      </div>
    );
  };

  const email =
    keycloak?.tokenParsed?.email ??
    keycloak?.tokenParsed?.preferred_username ??
    "user@example.com";

  return (
    <aside
      className={`bg-slate-800 text-white w-72 h-screen flex flex-col transition-transform duration-300 shadow-2xl overflow-hidden ${
        isMobile
          ? `fixed inset-y-0 left-0 z-50 ${
              isOpen ? "translate-x-0" : "-translate-x-full"
            }`
          : "relative"
      }`}
    >
      {/* Header */}
      <div className="p-6 border-b border-slate-600 relative">
        <div className="flex items-center gap-4">
          <div className="w-12 h-12 bg-slate-600 rounded-lg flex items-center justify-center">
            <div className="text-2xl">🏢</div>
          </div>
          <div>
            <h3 className="text-lg font-bold text-white">
              Apartment Management
            </h3>
            <p className="text-sm text-slate-300">Smart Building System</p>
          </div>
        </div>
        {isMobile && (
          <button
            className="absolute top-4 right-4 bg-slate-700 hover:bg-slate-600 border-none rounded-lg text-white w-8 h-8 cursor-pointer flex items-center justify-center text-base transition-all duration-200"
            onClick={onToggle}
          >
            ×
          </button>
        )}
      </div>

      {/* User Info */}
      <div className="p-4 border-b border-slate-600">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 bg-slate-600 rounded-full flex items-center justify-center">
            <div className="text-lg">👤</div>
          </div>
          <div className="flex-1">
            <span className="block font-semibold text-sm text-white">
              {getRoleLabel(mainRole)}
            </span>
            <span className="block text-xs text-slate-300">{email}</span>
          </div>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 p-3 overflow-y-auto overflow-x-hidden min-h-0">
        <ul className="space-y-1 pl-0">
          {menus.map((m) => (
            <li key={m.key}>
              <MenuBranch item={m} />
            </li>
          ))}
        </ul>
      </nav>
    </aside>
  );
}
