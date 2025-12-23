// src/components/Layout.jsx
import { useEffect, useState } from "react";
import { Outlet } from "react-router-dom";
import Sidebar from "./Sidebar";
import ResidentLayout from "./resident/ResidentLayout";
import { keycloak } from "../keycloak/initKeycloak";
import { useUser } from "../hooks/useUser";

export default function Layout() {
  const [isSidebarOpen, setSidebarOpen] = useState(false);
  const [isMobile, setIsMobile] = useState(false);
  const { user } = useUser();

  useEffect(() => {
    const sync = () => setIsMobile(window.innerWidth < 768);
    sync();
    window.addEventListener("resize", sync);
    return () => window.removeEventListener("resize", sync);
  }, []);

  const email =
    keycloak?.tokenParsed?.email ??
    keycloak?.tokenParsed?.preferred_username ??
    "";

  // Determine if current user is a resident; if so, use ResidentLayout
  const roles = [
    ...(keycloak.tokenParsed?.realm_access?.roles ?? []),
    ...(keycloak.tokenParsed?.resource_access?.backend?.roles ?? []),
  ];

  const isResident = roles.includes("resident");

  const handleLogout = () => {
    keycloak.logout({});
  };

  if (isResident) {
    return (
      <ResidentLayout apartment={user?.apartment || null} pendingBillsCount={0}>
        <Outlet />
      </ResidentLayout>
    );
  }

  return (
    <div className="h-screen bg-slate-50">
      <div className="hidden md:block fixed inset-y-0 left-0 z-40 w-72">
        <Sidebar
          isOpen
          isMobile={false}
          onToggle={() => {}}
          onLogout={handleLogout}
        />
      </div>

      <div className="md:hidden">
        {isSidebarOpen && (
          <div
            className="fixed inset-0 bg-black/50 z-40"
            onClick={() => setSidebarOpen(false)}
          />
        )}
        <div className="fixed inset-y-0 left-0 z-50">
          <Sidebar
            isOpen={isSidebarOpen}
            isMobile
            onToggle={() => setSidebarOpen(false)}
            onLogout={handleLogout}
          />
        </div>
      </div>

      <header className="fixed top-0 right-0 left-0 md:left-72 h-14 bg-white/90 backdrop-blur border-b border-slate-200 z-30 flex items-center">
        <div className="flex items-center justify-between w-full px-3 md:px-5">
          <button
            className="md:hidden inline-flex items-center justify-center w-9 h-9 rounded-md border border-slate-300 text-slate-700"
            onClick={() => setSidebarOpen(true)}
            aria-label="Open sidebar"
          >
            <svg
              className="w-5 h-5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeWidth="2"
                strokeLinecap="round"
                d="M4 6h16M4 12h16M4 18h16"
              />
            </svg>
          </button>

          <div className="flex items-center gap-2">
            <span className="font-semibold text-slate-800">Dashboard</span>
            <span className="hidden sm:inline text-slate-400">•</span>
            <span className="hidden sm:inline text-slate-500 text-sm">
              {email}
            </span>
          </div>

          <button
            onClick={handleLogout}
            className="inline-flex items-center gap-2 px-3 h-9 rounded-md bg-red-600 hover:bg-red-700 text-white text-sm"
          >
            <svg
              className="w-4 h-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"
              />
            </svg>
            Đăng xuất
          </button>
        </div>
      </header>

      <main className="pt-14 md:ml-72 h-full">
        <div className="h-[calc(100vh-3.5rem)] overflow-auto p-3 md:p-5">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
