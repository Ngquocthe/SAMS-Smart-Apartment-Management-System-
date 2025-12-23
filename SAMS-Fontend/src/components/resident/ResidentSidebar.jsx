import React from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { Layout, Button, Typography, Space } from "antd";
import {
  DashboardOutlined,
  FileTextOutlined,
  DollarOutlined,
  CalendarOutlined,
  FileProtectOutlined,
  TeamOutlined,
} from "@ant-design/icons";

const { Sider } = Layout;
const { Text } = Typography;

export default function ResidentSidebar({ collapsed, onCollapse, apartment }) {
  const navigate = useNavigate();
  const location = useLocation();

  // Helper function to check if current path matches menu item
  const isActive = (path) => location.pathname === path;

  return (
    <Sider
      collapsible
      collapsed={collapsed}
      onCollapse={onCollapse}
      theme="light"
      width={250}
      style={{
        overflow: "auto",
        height: "calc(100vh - 64px)",
        position: "fixed",
        left: 0,
        top: 64,
        bottom: 0,
        boxShadow: "2px 0 8px rgba(0,0,0,0.1)",
        background: "#fff",
      }}
    >
      {/* User Info in Sidebar */}
      {!collapsed && (
        <div
          style={{
            padding: "20px 16px",
            borderBottom: "1px solid #f0f0f0",
            background: "linear-gradient(135deg, #667eea15 0%, #764ba215 100%)",
          }}
        >
          <Space direction="vertical" size={4} style={{ width: "100%" }}>
            <Text strong style={{ fontSize: 14 }}>
              {apartment?.name || "Cư dân"}
            </Text>
            {apartment?.building && apartment?.number && (
              <Text type="secondary" style={{ fontSize: 12 }}>
                🏠 {apartment.building} - Căn {apartment.number}
              </Text>
            )}
          </Space>
        </div>
      )}

      {/* Sidebar Menu */}
      <div style={{ marginTop: 16 }}>
        <Button
          type="text"
          icon={<DashboardOutlined style={{ fontSize: 18 }} />}
          onClick={() => navigate("/resident/dashboard")}
          style={{
            width: "100%",
            height: 48,
            justifyContent: "flex-start",
            paddingLeft: collapsed ? 24 : 24,
            background: isActive("/resident/dashboard") 
              ? "linear-gradient(135deg, #667eea15 0%, #764ba215 100%)"
              : "transparent",
            borderLeft: isActive("/resident/dashboard") ? "3px solid #667eea" : "3px solid transparent",
            fontWeight: isActive("/resident/dashboard") ? 500 : 400,
            marginBottom: 4,
          }}
        >
          {!collapsed && <span style={{ marginLeft: 8 }}>Dashboard</span>}
        </Button>

        <Button
          type="text"
          icon={<FileTextOutlined style={{ fontSize: 18 }} />}
          onClick={() => navigate("/resident/my-tickets")}
          style={{
            width: "100%",
            height: 48,
            justifyContent: "flex-start",
            paddingLeft: collapsed ? 24 : 24,
            background: isActive("/resident/my-tickets") 
              ? "linear-gradient(135deg, #667eea15 0%, #764ba215 100%)"
              : "transparent",
            borderLeft: isActive("/resident/my-tickets") ? "3px solid #667eea" : "3px solid transparent",
            fontWeight: isActive("/resident/my-tickets") ? 500 : 400,
            marginBottom: 4,
          }}
        >
          {!collapsed && <span style={{ marginLeft: 8 }}>Yêu cầu của tôi</span>}
        </Button>

        <Button
          type="text"
          icon={<DollarOutlined style={{ fontSize: 18 }} />}
          onClick={() => navigate("/resident/invoices")}
          style={{
            width: "100%",
            height: 48,
            justifyContent: "flex-start",
            paddingLeft: collapsed ? 24 : 24,
            background: isActive("/resident/invoices") 
              ? "linear-gradient(135deg, #667eea15 0%, #764ba215 100%)"
              : "transparent",
            borderLeft: isActive("/resident/invoices") ? "3px solid #667eea" : "3px solid transparent",
            fontWeight: isActive("/resident/invoices") ? 500 : 400,
            marginBottom: 4,
          }}
        >
          {!collapsed && <span style={{ marginLeft: 8 }}>Hóa đơn</span>}
        </Button>

        <Button
          type="text"
          icon={<CalendarOutlined style={{ fontSize: 18 }} />}
          onClick={() => navigate("/resident/amenity-booking")}
          style={{
            width: "100%",
            height: 48,
            justifyContent: "flex-start",
            paddingLeft: collapsed ? 24 : 24,
            background: isActive("/resident/amenity-booking") 
              ? "linear-gradient(135deg, #667eea15 0%, #764ba215 100%)"
              : "transparent",
            borderLeft: isActive("/resident/amenity-booking") ? "3px solid #667eea" : "3px solid transparent",
            fontWeight: isActive("/resident/amenity-booking") ? 500 : 400,
            marginBottom: 4,
          }}
        >
          {!collapsed && (
            <span style={{ marginLeft: 8 }}>Đăng ký tiện ích</span>
          )}
        </Button>

        <Button
          type="text"
          icon={<FileProtectOutlined style={{ fontSize: 18 }} />}
          onClick={() => navigate("/resident/contract")}
          style={{
            width: "100%",
            height: 48,
            justifyContent: "flex-start",
            paddingLeft: collapsed ? 24 : 24,
            background: isActive("/resident/contract") 
              ? "linear-gradient(135deg, #667eea15 0%, #764ba215 100%)"
              : "transparent",
            borderLeft: isActive("/resident/contract") ? "3px solid #667eea" : "3px solid transparent",
            fontWeight: isActive("/resident/contract") ? 500 : 400,
            marginBottom: 4,
          }}
        >
          {!collapsed && <span style={{ marginLeft: 8 }}>Hợp đồng</span>}
        </Button>

        <Button
          type="text"
          icon={<TeamOutlined style={{ fontSize: 18 }} />}
          onClick={() => navigate("/resident/household")}
          style={{
            width: "100%",
            height: 48,
            justifyContent: "flex-start",
            paddingLeft: collapsed ? 24 : 24,
            background: isActive("/resident/household") 
              ? "linear-gradient(135deg, #667eea15 0%, #764ba215 100%)"
              : "transparent",
            borderLeft: isActive("/resident/household") ? "3px solid #667eea" : "3px solid transparent",
            fontWeight: isActive("/resident/household") ? 500 : 400,
            marginBottom: 4,
          }}
        >
          {!collapsed && (
            <span style={{ marginLeft: 8 }}>Thành viên gia đình</span>
          )}
        </Button>
      </div>
    </Sider>
  );
}
