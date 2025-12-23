// src/App.js
import { Suspense, useEffect } from "react";
import { BrowserRouter, Routes, Route, useNavigate } from "react-router-dom";
import "bootstrap/dist/css/bootstrap.min.css";
import "./index.css";
import "antd/dist/reset.css";
import Homepage from "./pages/common/Homepage";
import AboutUs from "./pages/common/AboutUs";
import PaymentSuccess from "./pages/payment/PaymentSuccess";
import PaymentCancel from "./pages/payment/PaymentCancel";

import Layout from "./components/Layout";
import CONFIG_ROUTER from "./routers/ConfigRoutes";
import ROUTER_PAGE from "./constants/Routes";
import { keycloak } from "./keycloak/initKeycloak";
import { NotificationProvider } from "./hooks/useNotification";
import StaffCreateForm from "./components/staff/StaffAddNew";
import StaffUpdatePage from "./pages/admin/StaffUpdatePage";
import BuildingCreateForm from "./components/building/BuildingCreateForm";

function AuthCallback() {
  const navigate = useNavigate();

  useEffect(() => {
    if (!keycloak.authenticated) return;

    // đánh dấu vừa login (để logic redirect khác có thể dùng)
    sessionStorage.setItem("kc_just_logged_in", "1");

    // lấy role từ cả realm và resource (backend)
    const roles = [
      ...(keycloak.tokenParsed?.realm_access?.roles ?? []),
      ...(keycloak.tokenParsed?.resource_access?.backend?.roles ?? []),
    ];

    // Xác định role chính của user (ưu tiên theo thứ tự)
    let target = ROUTER_PAGE.HOME; // default fallback

    if (roles.includes("global_admin")) {
      target = ROUTER_PAGE.ADMIN.BUILDING.LIST_BUILDINGS;
    } else if (roles.includes("building_admin")) {
      target = ROUTER_PAGE.BUILDING_MANAGER.DASHBOARD;
    } else if (roles.includes("accountant")) {
      target = ROUTER_PAGE.ACCOUNTANT.FINANCIAL_DASHBOARD;
    } else if (roles.includes("receptionist")) {
      target = ROUTER_PAGE.RECEPTIONIST.DASHBOARD;
    } else if (roles.includes("resident")) {
      target = ROUTER_PAGE.RESIDENT.DASHBOARD;
    } else if (roles.includes("security")) {
      target = "/security/dashboard";
    }

    navigate(target, { replace: true });
  }, [navigate]);

  return null;
}

function AppContent() {
  return (
    <NotificationProvider>
      <BrowserRouter>
        <Suspense fallback={<div className="loading">Loading…</div>}>
          <Routes>
            <Route path={ROUTER_PAGE.HOME} element={<Homepage />} />
            <Route path={ROUTER_PAGE.ABOUT_US} element={<AboutUs />} />
            <Route path={ROUTER_PAGE.CALLBACK} element={<AuthCallback />} />
            <Route
              path={ROUTER_PAGE.PAYMENT.SUCCESS}
              element={<PaymentSuccess />}
            />
            <Route
              path={ROUTER_PAGE.PAYMENT.CANCEL}
              element={<PaymentCancel />}
            />

            <Route element={<Layout />}>
              <Route
                path={ROUTER_PAGE.ADMIN.STAFF.CREATE_STAFF}
                element={<StaffCreateForm />}
              />
              <Route
                path={ROUTER_PAGE.ADMIN.STAFF.EDIT_STAFF}
                element={<StaffUpdatePage />}
              />
            </Route>
            <Route element={<Layout />}>
              <Route
                path={ROUTER_PAGE.ADMIN.BUILDING.CREATE_BUILDING}
                element={<BuildingCreateForm />}
              />
            </Route>

            <Route element={<Layout />}>
              {CONFIG_ROUTER.filter((r) => r.private)
                .flatMap((r) =>
                  r.children && Array.isArray(r.children) ? r.children : [r]
                )
                .filter((r) => r && r.path && r.component)
                .map((r) => (
                  <Route
                    key={r.key || r.path}
                    path={r.path}
                    element={<r.component />}
                  />
                ))}
            </Route>
          </Routes>
        </Suspense>
      </BrowserRouter>
    </NotificationProvider>
  );
}

export default function App() {
  return <AppContent />;
}
