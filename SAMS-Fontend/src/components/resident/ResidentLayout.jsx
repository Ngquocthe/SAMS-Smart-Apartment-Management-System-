import React, { useState, useEffect } from "react";
import { Layout, ConfigProvider } from "antd";
import ResidentHeader from "./ResidentHeader";
import ResidentSidebar from "./ResidentSidebar";

const { Content } = Layout;

export default function ResidentLayout({
  children,
  apartment,
  pendingBillsCount = 0,
}) {
  const [collapsed, setCollapsed] = useState(false);

  // Set higher z-index for modals to prevent header overlap
  useEffect(() => {
    const style = document.createElement('style');
    style.textContent = `
      .ant-modal-wrap { z-index: 1050 !important; }
      .ant-modal-mask { z-index: 1040 !important; }
    `;
    document.head.appendChild(style);
    return () => document.head.removeChild(style);
  }, []);

  return (
    <ConfigProvider
      theme={{
        components: {
          Modal: {
            zIndexPopup: 1050,
          },
        },
      }}
    >
      <Layout style={{ minHeight: "100vh" }}>
      <ResidentHeader pendingBillsCount={pendingBillsCount} />
      <Layout style={{ marginTop: 64 }}>
        <ResidentSidebar
          collapsed={collapsed}
          onCollapse={setCollapsed}
          apartment={apartment}
        />

        <Layout
          style={{
            marginLeft: collapsed ? 80 : 250,
            transition: "all 0.2s",
          }}
        >
          <Content
            style={{
              padding: "24px",
              background: "#f0f2f5",
              minHeight: "calc(100vh - 64px)",
            }}
          >
            {children}
          </Content>
        </Layout>
      </Layout>
    </Layout>
    </ConfigProvider>
  );
}
