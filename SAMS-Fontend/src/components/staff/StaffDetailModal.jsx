// src/components/staff/StaffDetailModal.jsx
import React, { useEffect, useMemo, useState } from "react";
import {
  Modal,
  Button,
  Space,
  Typography,
  Tag,
  Descriptions,
  Divider,
  Skeleton,
  message,
} from "antd";
import { CheckCircleOutlined, CloseCircleOutlined } from "@ant-design/icons";
import dayjs from "dayjs";

import { coreApi } from "../../features/building/coreApi";
import { staffApi } from "../../features/staff/staffApi";
import { keycloakApi } from "../../features/keycloak/keycloakApi";

export default function StaffDetailModal({
  open,
  loading,
  initialValues,
  schemaName,
  onCancel,
}) {
  const isActive = useMemo(
    () => initialValues?.isActive ?? !initialValues?.terminationDate,
    [initialValues]
  );

  const [buildings, setBuildings] = useState([]);
  const [roles, setRoles] = useState([]);
  const [kcRoles, setKcRoles] = useState([]);

  useEffect(() => {
    if (!open) return;

    (async () => {
      try {
        const [bds, kc] = await Promise.all([
          coreApi.getBuildings(),
          keycloakApi.getClientRoles(),
        ]);

        setBuildings(bds || []);

        const kcList = Array.isArray(kc) ? kc : kc?.data || [];
        setKcRoles(kcList);
      } catch (e) {
        console.error(e);
        message.error("Không tải được dữ liệu tham chiếu");
      }
    })();
  }, [open]);

  useEffect(() => {
    if (!open) {
      setRoles([]);
      return;
    }

    (async () => {
      try {
        const list = await staffApi.getWorkRoles(schemaName);
        setRoles(list || []);
      } catch (e) {
        console.error(e);
        message.error("Không tải được danh sách vai trò");
      }
    })();
  }, [open, schemaName]);

  const buildingName = useMemo(() => {
    if (!initialValues?.buildingId) return "-";
    if (!buildings.length) return "Đang tải...";

    return (
      buildings.find((b) => b.id === initialValues.buildingId)?.buildingName ||
      initialValues.buildingId
    );
  }, [buildings, initialValues?.buildingId]);

  const roleName = useMemo(() => {
    if (!initialValues?.roleId) return "-";
    if (!roles.length) return "Đang tải...";

    return (
      roles.find((r) => r.roleId === initialValues.roleId)?.roleName ||
      initialValues.roleId
    );
  }, [roles, initialValues]);

  const accessRoleLabels = useMemo(() => {
    if (!Array.isArray(initialValues?.accessRoles)) return [];

    if (!kcRoles.length) {
      return initialValues.accessRoles;
    }

    return initialValues.accessRoles.map((key) => {
      const found = kcRoles.find((r) => r.name === key || r.id === key);
      return found?.description || found?.name || key;
    });
  }, [kcRoles, initialValues]);

  if (!open) return null;

  return (
    <Modal
      open={open}
      title="Chi tiết nhân sự"
      onCancel={onCancel}
      footer={[
        <Button key="close" onClick={onCancel}>
          Đóng
        </Button>,
      ]}
      destroyOnHidden
      width={900}
    >
      {loading ? (
        <Skeleton active paragraph={{ rows: 10 }} />
      ) : !initialValues ? (
        <Typography.Text type="secondary">
          Không có dữ liệu nhân viên
        </Typography.Text>
      ) : (
        <>
          {/* ===== HEADER ===== */}
          <Space size={12} align="center">
            <Typography.Title level={4} style={{ margin: 0 }}>
              {initialValues.fullName ||
                [initialValues.firstName, initialValues.lastName]
                  .filter(Boolean)
                  .join(" ") ||
                "-"}
            </Typography.Title>

            {isActive ? (
              <Tag icon={<CheckCircleOutlined />} color="success">
                Active
              </Tag>
            ) : (
              <Tag icon={<CloseCircleOutlined />} color="default">
                Inactive
              </Tag>
            )}
          </Space>

          <Typography.Paragraph type="secondary" style={{ marginTop: 4 }}>
            Staff Code: <code>{initialValues.staffCode}</code>
          </Typography.Paragraph>

          <Divider />

          {/* ===== BASIC INFO ===== */}
          <Divider orientation="left">Thông tin cơ bản</Divider>
          <Descriptions bordered size="middle" column={2}>
            <Descriptions.Item label="Họ">
              {initialValues.firstName || "-"}
            </Descriptions.Item>
            <Descriptions.Item label="Tên">
              {initialValues.lastName || "-"}
            </Descriptions.Item>
            <Descriptions.Item label="Tên tài khoản">
              {initialValues.username || "-"}
            </Descriptions.Item>
            <Descriptions.Item label="Email">
              {initialValues.email || "-"}
            </Descriptions.Item>
            <Descriptions.Item label="Số điện thoại">
              {initialValues.phone || "-"}
            </Descriptions.Item>
            <Descriptions.Item label="Ngày sinh">
              {initialValues.dob
                ? dayjs(initialValues.dob).format("DD/MM/YYYY")
                : "-"}
            </Descriptions.Item>
          </Descriptions>

          {/* ===== ROLE & ACCESS ===== */}
          <Divider orientation="left">Phân quyền</Divider>
          <Descriptions bordered size="middle" column={2}>
            <Descriptions.Item label="Vai trò">
              <Tag color="blue">{roleName}</Tag>
            </Descriptions.Item>

            <Descriptions.Item label="Quyền truy cập">
              {accessRoleLabels.length
                ? accessRoleLabels.map((l) => <Tag key={l}>{l}</Tag>)
                : "-"}
            </Descriptions.Item>
          </Descriptions>

          {/* ===== WORK INFO ===== */}
          <Divider orientation="left">Thông tin công việc</Divider>
          <Descriptions bordered size="middle" column={2}>
            <Descriptions.Item label="Tòa nhà">
              {buildingName}
            </Descriptions.Item>

            <Descriptions.Item label="Ngày vào làm">
              {initialValues.hireDate
                ? dayjs(initialValues.hireDate).format("DD/MM/YYYY")
                : "-"}
            </Descriptions.Item>

            <Descriptions.Item label="Ngày nghỉ việc">
              {initialValues.terminationDate
                ? dayjs(initialValues.terminationDate).format("DD/MM/YYYY")
                : "-"}
            </Descriptions.Item>

            <Descriptions.Item label="Lương cơ bản">
              {initialValues.baseSalary
                ? `${Number(initialValues.baseSalary).toLocaleString()} ₫`
                : "-"}
            </Descriptions.Item>
          </Descriptions>

          {/* ===== ADDRESS ===== */}
          <Divider orientation="left">Địa chỉ</Divider>
          <Descriptions bordered size="middle" column={1}>
            <Descriptions.Item label="Quê quán">
              {initialValues.address || "-"}
            </Descriptions.Item>
            <Descriptions.Item label="Địa chỉ hiện tại">
              {initialValues.currentAddress || "-"}
            </Descriptions.Item>
          </Descriptions>

          {/* ===== NOTES ===== */}
          <Divider orientation="left">Ghi chú</Divider>
          <Typography.Paragraph>
            {initialValues.notes || "-"}
          </Typography.Paragraph>
        </>
      )}
    </Modal>
  );
}
