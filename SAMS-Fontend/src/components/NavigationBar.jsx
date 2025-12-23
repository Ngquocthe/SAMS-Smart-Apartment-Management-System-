import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import LanguageSwitcher from "./LanguageSwitcher";
import { useLanguage } from "../hooks/useLanguage";
import { useUser } from "../hooks/useUser";
import "../styles/NavigationBarTailwind.css";
import { keycloak } from "../keycloak/initKeycloak";
import ROUTER_PAGE from "../constants/Routes";
import { Avatar } from "antd";

const NavigationBar = ({ activePage = "home" }) => {
  const navigate = useNavigate();
  const { strings, setLanguage, language } = useLanguage();
  const { user } = useUser();
  const [isDropdownOpen, setIsDropdownOpen] = useState(false);
  const [keycloakUser, setKeycloakUser] = useState(null);

  useEffect(() => {
    if (keycloak.authenticated) {
      setKeycloakUser(keycloak.tokenParsed);
    }
  }, []);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (isDropdownOpen && !event.target.closest(".relative")) {
        setIsDropdownOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, [isDropdownOpen]);

  const handleLoginClick = () => {
    sessionStorage.setItem("kc_login_intent", "1");
    keycloak.login({
      redirectUri: `${window.location.origin}/auth/callback`,
    });
  };

  const handleNavigate = (path) => {
    navigate(path);
  };

  const handleLogout = () => {
    keycloak.logout();
  };

  const handleProfileClick = () => {
    navigate(ROUTER_PAGE.USER.PROFILE);
    setIsDropdownOpen(false);
  };

  const handleDropdownToggle = () => {
    setIsDropdownOpen(!isDropdownOpen);
  };

  return (
    <nav className="navigation-bar flex justify-between items-center px-8 py-6">
      {/* Left side - Logo với Tailwind cho layout */}
      <div className="nav-left">
        <div
          className="logo flex items-center gap-2 cursor-pointer"
          onClick={() => handleNavigate("/")}
        >
          <span className="logo-icon">🏢</span>
          <div className="logo-text-container flex flex-col items-start leading-tight">
            <span className="logo-text text-2xl font-bold text-white drop-shadow-md">
              Noah
            </span>
            <span className="logo-subtitle text-xs font-medium text-slate-400 tracking-wider">
              Smart Apartment Management
            </span>
          </div>
        </div>
      </div>

      {/* Right side - Menu và actions với Tailwind cho layout */}
      <div className="nav-right flex items-center gap-8">
        <div className="flex items-center gap-6">
          <button
            className={`nav-link px-4 py-2 rounded-lg font-semibold text-sm transition-all duration-300 ${
              activePage === "home" ? "active" : ""
            }`}
            onClick={() => handleNavigate("/")}
          >
            {strings.home}
          </button>
          <button
            className={`nav-link px-4 py-2 rounded-lg font-semibold text-sm transition-all duration-300 ${
              activePage === "about" ? "active" : ""
            }`}
            onClick={() => handleNavigate("/aboutUs")}
          >
            {strings.about}
          </button>
          <button
            className={`nav-link px-4 py-2 rounded-lg font-semibold text-sm transition-all duration-300 ${
              activePage === "features" ? "active" : ""
            }`}
            onClick={() => handleNavigate("/features")}
          >
            {strings.features}
          </button>
          <button
            className={`nav-link px-4 py-2 rounded-lg font-semibold text-sm transition-all duration-300 ${
              activePage === "contact" ? "active" : ""
            }`}
            onClick={() => handleNavigate("/contact")}
          >
            {strings.contact}
          </button>
        </div>

        <div className="nav-actions flex items-center gap-4">
          <LanguageSwitcher
            currentLanguage={language}
            setLanguage={setLanguage}
          />

          {/* User Avatar or Login Button */}
          {keycloak.authenticated ? (
            <div className="relative">
              <button
                className="flex items-center gap-2 rounded-lg hover:bg-gray-700 transition-all duration-200"
                onClick={handleDropdownToggle}
              >
                <Avatar
                  size={40}
                  shape="circle"
                  src={user?.avatarUrl || undefined}
                  onError={(e) => {
                    e.currentTarget.src = "";
                  }}
                  style={{ fontWeight: 700 }}
                >
                  {(
                    user?.firstName?.trim?.() ||
                    keycloakUser?.name?.trim?.() ||
                    keycloakUser?.preferred_username?.trim?.() ||
                    "U"
                  )
                    .charAt(0)
                    .toUpperCase()}
                </Avatar>
                <div className="hidden md:block text-left">
                  <p className="text-sm font-medium mb-0.5">
                    {user?.fullName || "User"}
                  </p>
                  <p className="text-xs text-gray-500 mb-0.5">
                    {user?.email || ""}
                  </p>
                </div>
                <svg
                  className="w-4 h-4 text-gray-400"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M19 9l-7 7-7-7"
                  />
                </svg>
              </button>

              {/* Dropdown Menu */}
              {isDropdownOpen && (
                <div className="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-50">
                  <button
                    className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 flex items-center gap-2"
                    onClick={handleProfileClick}
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
                        d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                      />
                    </svg>
                    Thông tin cá nhân
                  </button>
                  <button
                    className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 flex items-center gap-2"
                    onClick={() => {
                      navigate("/user/settings");
                      setIsDropdownOpen(false);
                    }}
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
                        d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"
                      />
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                      />
                    </svg>
                    Cài đặt
                  </button>
                  <hr className="my-1" />
                  <button
                    className="w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-red-50 flex items-center gap-2"
                    onClick={handleLogout}
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
              )}
            </div>
          ) : (
            <button
              className="login-btn flex items-center gap-2 px-4 py-2 rounded-md font-semibold text-sm transition-all duration-300"
              onClick={handleLoginClick}
            >
              <i className="fas fa-sign-in-alt"></i>
              <span>{strings.login}</span>
            </button>
          )}
        </div>
      </div>
    </nav>
  );
};

export default NavigationBar;
