import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { Layout, Button, Typography, Space, Avatar } from "antd";
import {
  HomeOutlined,
  UserOutlined,
  NotificationOutlined,
  FileTextOutlined,
} from "@ant-design/icons";
import { keycloak } from "../../keycloak/initKeycloak";
import { useUser } from "../../hooks/useUser";
import ROUTER_PAGE from "../../constants/Routes";
import NotificationBell from "../NotificationBell";

const { Header } = Layout;
const { Text } = Typography;

export default function ResidentHeader({ pendingBillsCount = 0 }) {
  const navigate = useNavigate();
  const { user } = useUser();
  const [isDropdownOpen, setIsDropdownOpen] = useState(false);
  const [keycloakUser, setKeycloakUser] = useState(null);

  useEffect(() => {
    if (keycloak.authenticated) {
      setKeycloakUser(keycloak.tokenParsed);
    }
  }, []);

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (isDropdownOpen && !event.target.closest(".user-dropdown-container")) {
        setIsDropdownOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, [isDropdownOpen]);

  return (
    <Header
      style={{
        background: "linear-gradient(135deg, #667eea 0%, #764ba2 100%)",
        padding: "0 24px",
        boxShadow: "0 2px 8px rgba(0,0,0,0.15)",
        position: "fixed",
        width: "100%",
        top: 0,
        zIndex: 1001,
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
      }}
    >
      {/* Left - Logo */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 12,
          cursor: "pointer",
        }}
        onClick={() => navigate(ROUTER_PAGE.HOME)}
      >
        <span style={{ fontSize: 28 }}>🏢</span>
        <div
          style={{
            display: "flex",
            flexDirection: "column",
            lineHeight: 1.2,
          }}
        >
          <Text style={{ color: "#fff", fontSize: 18, fontWeight: "bold" }}>
            Noah
          </Text>
          <Text style={{ color: "rgba(255,255,255,0.7)", fontSize: 10 }}>
            Smart Apartment
          </Text>
        </div>
      </div>

      {/* Center - Common Navigation */}
      <Space size="large">
        <Button
          type="text"
          icon={<HomeOutlined />}
          onClick={() => navigate(ROUTER_PAGE.RESIDENT.DASHBOARD)}
          style={{
            color: "#fff",
            fontWeight: 500,
            fontSize: 14,
            display: "flex",
            alignItems: "center",
            gap: 6,
          }}
        >
          Trang chủ
        </Button>
        <Button
          type="text"
          icon={<NotificationOutlined />}
          onClick={() => navigate(ROUTER_PAGE.RESIDENT.NEWS)}
          style={{
            color: "#fff",
            fontWeight: 500,
            fontSize: 14,
            display: "flex",
            alignItems: "center",
            gap: 6,
          }}
        >
          Tin tức
        </Button>
        <Button
          type="text"
          icon={<FileTextOutlined />}
          onClick={() => navigate(ROUTER_PAGE.RESIDENT.DOCUMENTS)}
          style={{
            color: "#fff",
            fontWeight: 500,
            fontSize: 14,
            display: "flex",
            alignItems: "center",
            gap: 6,
          }}
        >
          Tài liệu
        </Button>
      </Space>

      {/* Right - Notifications & User */}
      <Space size="large">
        {/* Notification Bell - Hiển thị thông báo cho cư dân */}
        <div style={{ color: '#fff' }}>
          <NotificationBell onlyMaintenance={false} />
        </div>

        {/* User Dropdown */}
        <div
          className="user-dropdown-container"
          style={{ position: "relative" }}
        >
          <Button
            type="text"
            onClick={() => setIsDropdownOpen(!isDropdownOpen)}
            style={{
              display: "flex",
              alignItems: "center",
              padding: "4px 8px",
              height: "auto",
              background: "rgba(255,255,255,0.1)",
              borderRadius: 8,
            }}
          >
            <Avatar
              size={32}
              shape="circle"
              src={user?.avatarUrl || undefined}
              onError={(e) => {
                e.currentTarget.src = "";
              }}
              style={{ fontWeight: 700, marginRight: 8 }}
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

            <Text
              style={{
                color: "#fff",
                fontSize: 14,
                fontWeight: 500,
                maxWidth: 120,
              }}
              ellipsis
            >
              {keycloakUser?.name ||
                keycloakUser?.preferred_username ||
                "Cư dân"}
            </Text>

            {/* Dropdown Arrow */}
            <svg
              style={{
                width: 14,
                height: 14,
                color: "rgba(255,255,255,0.7)",
                transition: "transform 0.3s",
                transform: isDropdownOpen ? "rotate(180deg)" : "rotate(0deg)",
              }}
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
          </Button>

          {/* Dropdown Menu */}
          {isDropdownOpen && (
            <div
              style={{
                position: "absolute",
                right: 0,
                marginTop: 8,
                width: 220,
                background: "#fff",
                borderRadius: 8,
                boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
                border: "1px solid #e8e8e8",
                zIndex: 1002,
              }}
            >
              {/* User Info Section */}
              <div
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 12,
                  padding: "8px 16px",
                  borderBottom: "1px solid #f0f0f0",
                }}
              >
                {/* Avatar */}
                <Avatar
                  size={40}
                  shape="circle"
                  src={user?.avatarUrl || undefined}
                  onError={(e) => {
                    e.currentTarget.src = "";
                  }}
                  style={{ fontWeight: 700, flexShrink: 0 }}
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

                {/* Info */}
                <div style={{ flex: 1, minWidth: 0 }}>
                  <Text
                    style={{
                      fontSize: 14,
                      fontWeight: 500,
                      display: "block",
                      marginBottom: 2,
                    }}
                    ellipsis
                  >
                    {keycloakUser?.name ||
                      keycloakUser?.preferred_username ||
                      "Người dùng"}
                  </Text>
                  <Text
                    style={{
                      fontSize: 12,
                      color: "#8c8c8c",
                      display: "block",
                    }}
                    ellipsis
                  >
                    {keycloakUser?.email || "Chưa có email"}
                  </Text>
                  {user?.phoneNumber && (
                    <Text
                      style={{
                        fontSize: 12,
                        color: "#8c8c8c",
                        display: "block",
                        marginTop: 2,
                      }}
                      ellipsis
                    >
                      📱 {user.phoneNumber}
                    </Text>
                  )}
                </div>
              </div>

              {/* Menu Items */}
              <div style={{ padding: "4px 0" }}>
                <Button
                  type="text"
                  icon={<UserOutlined />}
                  onClick={() => {
                    navigate("/user/profile");
                    setIsDropdownOpen(false);
                  }}
                  style={{
                    textAlign: "left",
                    borderRadius: 0,
                    display: "flex",
                    alignItems: "center",
                  }}
                >
                  <span style={{ fontSize: 14 }}>Cài đặt tài khoản</span>
                </Button>
              </div>

              {/* Logout */}
              <div
                style={{
                  borderTop: "1px solid #f0f0f0",
                  padding: "4px 0",
                  marginTop: 4,
                }}
              >
                <Button
                  type="text"
                  danger
                  onClick={() => {
                    keycloak.logout();
                    setIsDropdownOpen(false);
                  }}
                  style={{
                    textAlign: "left",
                    borderRadius: 0,
                    display: "flex",
                    alignItems: "center",
                  }}
                >
                  <svg
                    style={{ width: 14, height: 14 }}
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
                  <span style={{ fontSize: 14 }}>Đăng xuất</span>
                </Button>
              </div>
            </div>
          )}
        </div>
      </Space>
    </Header>
  );
}
