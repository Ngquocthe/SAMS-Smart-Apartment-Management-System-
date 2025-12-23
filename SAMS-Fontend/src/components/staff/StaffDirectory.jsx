import { useEffect, useMemo, useState, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import {
  Table,
  Card,
  Space,
  Button,
  Tag,
  Input,
  Select,
  Flex,
  message,
  Dropdown,
  Modal,
  Tooltip,
  Typography,
} from "antd";
import {
  EyeOutlined,
  EditOutlined,
  PoweroffOutlined,
  CheckCircleOutlined,
  ReloadOutlined,
  SearchOutlined,
  HomeOutlined,
  MoreOutlined,
  PlusOutlined,
} from "@ant-design/icons";
import { coreApi } from "../../features/building/coreApi";
import { staffApi } from "../../features/staff/staffApi";
import StaffDetailModal from "../../components/staff/StaffDetailModal";
import ROUTER_PAGE from "../../constants/Routes";

const shortId = (val, prefix = 8, suffix = 6) => {
  if (!val) return "-";
  const s = String(val);
  return s.length <= prefix + suffix
    ? s
    : `${s.slice(0, prefix)}…${s.slice(-suffix)}`;
};

const confirmToggle = (active, onOk) => {
  Modal.confirm({
    title: active
      ? "Bạn có chắc muốn khóa tài khoản này?"
      : "Bạn có chắc muốn kích hoạt tài khoản này?",
    okText: "Xác nhận",
    cancelText: "Hủy",
    onOk,
  });
};

export default function StaffDirectory({
  defaultBuildingId,
  onViewDetail,
  onEdit,
}) {
  const [buildings, setBuildings] = useState([]);
  const [buildingId, setBuildingId] = useState(defaultBuildingId || undefined);
  const [searchText, setSearchText] = useState("");
  const [roles, setRoles] = useState([]);
  const [roleId, setRoleId] = useState(null);

  const [data, setData] = useState([]);
  const [total, setTotal] = useState(0);
  const [paging, setPaging] = useState({ current: 1, pageSize: 10 });
  const [loading, setLoading] = useState(false);

  const [detailOpen, setDetailOpen] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detail, setDetail] = useState(null);
  const [detailMode, setDetailMode] = useState("view");

  const schemaName = useMemo(() => {
    if (!buildingId || !buildings?.length) return undefined;
    const b = buildings.find((x) => x.id === buildingId);
    return b?.schemaName;
  }, [buildingId, buildings]);

  // Load buildings
  useEffect(() => {
    (async () => {
      try {
        const items = await coreApi.getBuildings();
        setBuildings(items);
        if (!defaultBuildingId && items?.length) {
          const firstId = items[0]?.id;
          setBuildingId(firstId);
        }
      } catch (e) {
        console.error(e);
        message.error("Không tải được danh sách toà nhà");
      }
    })();
  }, [defaultBuildingId]);

  useEffect(() => {
    if (!schemaName) {
      setRoles([]);
      setRoleId(null);
      return;
    }
    (async () => {
      try {
        const list = await staffApi.getWorkRoles(schemaName);
        const normalized = (list || []).map((r) => ({
          roleId: r.roleId,
          roleName: r.roleName,
        }));
        setRoles(normalized);
      } catch (e) {
        console.error(e);
        message.error("Không tải được danh sách vai trò");
        setRoles([]);
      }
    })();
  }, [schemaName]);

  const loadData = useCallback(async () => {
    if (!schemaName) return;
    setLoading(true);

    const { current, pageSize } = paging;

    try {
      const {
        items,
        totalCount,
        pageNumber,
        pageSize: serverPageSize,
      } = await staffApi.listStaff(schemaName, {
        search: searchText || undefined,
        roleId: roleId || undefined,
        page: current,
        pageSize,
      });

      const normalized = (items || []).map((r) => ({
        staffCode: r.staffCode,
        userId: r.userId,
        firstName: r.firstName ?? "",
        lastName: r.lastName ?? "",
        fullName: r.fullName ?? "",
        email: r.email ?? "",
        phone: r.phone ?? "",
        role: r.role ?? null,
        terminationDate: r.terminationDate,
        isActive: r.isActive ?? false,
      }));

      setData(normalized);
      setTotal(totalCount ?? 0);

      if (pageNumber && pageNumber !== current) {
        setPaging((p) => ({ ...p, current: pageNumber }));
      }
      if (serverPageSize && serverPageSize !== pageSize) {
        setPaging((p) => ({ ...p, pageSize: serverPageSize }));
      }
    } catch (e) {
      console.error(e);
      message.error("Không tải được danh sách nhân sự");
    } finally {
      setLoading(false);
    }
  }, [schemaName, searchText, roleId, paging]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const navigate = useNavigate();

  const handleAddStaff = () => {
    navigate(ROUTER_PAGE.ADMIN.STAFF.CREATE_STAFF);
  };

  // Actions
  const ensureSchemaOrWarn = () => {
    if (!schemaName) {
      message.warning("Vui lòng chọn toà nhà trước khi thao tác");
      return false;
    }
    return true;
  };

  const handleActivate = async (record) => {
    if (!ensureSchemaOrWarn()) return;
    try {
      const active = !record.terminationDate;
      if (active) {
        await staffApi.deactivate(schemaName, record.staffCode);
        message.success("Đã khóa tài khoản này");
      } else {
        await staffApi.activate(schemaName, record.staffCode);
        message.success("Đã kích hoạt tài khoản này");
      }
      loadData();
    } catch (e) {
      console.error(e);
      message.error("Thao tác thất bại");
    }
  };

  const openDetailModal = (mode = "view") => {
    setDetailMode(mode);
    setDetail(null);
    setDetailLoading(true);
    setDetailOpen(true);
  };

  const fetchDetail = async (staffCode) => {
    const res = await staffApi.getDetail(schemaName, staffCode);

    if (!res) {
      setDetail(null);
      return;
    }

    const firstName = res.firstName ?? "";
    const lastName = res.lastName ?? "";

    const fullName =
      res.fullName && res.fullName.trim().length > 0
        ? res.fullName
        : `${firstName} ${lastName}`.trim();

    const normalized = {
      // ===== IDENTIFIER =====
      staffCode: res.staffCode,
      userId: res.userId,
      buildingId: res.buildingId,

      // ===== BASIC INFO =====
      firstName,
      lastName,
      fullName,
      username: res.username ?? "",
      email: res.email ?? "",
      phone: res.phone ?? "",
      dob: res.dob ?? null,

      // ===== ROLE & ACCESS =====
      roleId: res.roleId ?? null,
      role: res.role ?? null,
      accessRoles: res.accessRoles ?? [],

      // ===== WORK INFO =====
      hireDate: res.hireDate ?? null,
      terminationDate: res.terminationDate ?? null,
      isActive: res.isActive ?? !res.terminationDate,
      baseSalary: res.baseSalary ?? null,

      // ===== ADDRESS =====
      address: res.address ?? "",
      currentAddress: res.currentAddress ?? "",

      // ===== EMERGENCY CONTACT =====
      emergencyContactName: res.emergencyContactName ?? null,
      emergencyContactPhone: res.emergencyContactPhone ?? null,
      emergencyContactRelation: res.emergencyContactRelation ?? null,

      // ===== BANK =====
      bankAccountNo: res.bankAccountNo ?? null,
      bankName: res.bankName ?? null,
      bankBranch: res.bankBranch ?? null,

      // ===== INSURANCE & TAX =====
      taxCode: res.taxCode ?? null,
      socialInsuranceNo: res.socialInsuranceNo ?? null,

      // ===== MEDIA =====
      avatarUrl: res.avatarUrl ?? null,
      cardPhotoUrl: res.cardPhotoUrl ?? null,

      // ===== NOTE =====
      notes: res.notes ?? "",
    };

    setDetail(normalized);
  };

  const handleView = async (record) => {
    if (!ensureSchemaOrWarn()) return;
    if (onViewDetail) return onViewDetail(record);
    try {
      openDetailModal("view");
      await fetchDetail(record.staffCode);
    } catch {
      message.error("Không lấy được chi tiết");
      setDetailOpen(false);
    } finally {
      setDetailLoading(false);
    }
  };

  const handleEdit = async (record) => {
    if (!ensureSchemaOrWarn()) return;
    if (onEdit) return onEdit(record);
    // Navigate to update page
    navigate(
      ROUTER_PAGE.ADMIN.STAFF.EDIT_STAFF.replace(":id", record.staffCode)
    );
  };

  const handleSubmitDetail = async (payload) => {
    if (!schemaName || !payload?.staffCode) return;
    await staffApi.updateStaff(schemaName, payload.staffCode, payload);
    setDetailOpen(false);
    await loadData();
  };

  const resetFilters = () => {
    setSearchText("");
    setRoleId(null);
    setPaging((p) => ({ ...p, current: 1 }));
  };

  const onMenuClick = (info, record) => {
    const key = info?.key;
    if (key === "view") return handleView(record);
    if (key === "edit") return handleEdit(record);
    if (key === "toggle") {
      const active = !record.terminationDate && record.isActive;
      return confirmToggle(active, () => handleActivate(record));
    }
  };

  // Columns
  const columns = [
    {
      title: "Mã nhân sự",
      dataIndex: "staffCode",
      onHeaderCell: () => ({ style: { whiteSpace: "nowrap" } }),
      onCell: () => ({ style: { whiteSpace: "nowrap" } }),
      render: (v) => (
        <Space size={6}>
          <Tooltip title={v}>
            <span
              style={{
                fontFamily: "ui-monospace, SFMono-Regular, Menlo, monospace",
              }}
            >
              {shortId(v)}
            </span>
          </Tooltip>
          <Typography.Text copyable={{ text: v }} />
        </Space>
      ),
    },
    {
      title: "Họ tên",
      dataIndex: "fullName",
      render: (_, r) =>
        r.fullName || [r.firstName, r.lastName].filter(Boolean).join(" "),
      onHeaderCell: () => ({ style: { whiteSpace: "nowrap" } }),
      onCell: () => ({ style: { whiteSpace: "nowrap" } }),
    },
    {
      title: "Email",
      dataIndex: "email",
      responsive: ["md"],
      onHeaderCell: () => ({ style: { whiteSpace: "nowrap" } }),
      onCell: () => ({ style: { whiteSpace: "nowrap" } }),
    },
    {
      title: "Số điện thoại",
      dataIndex: "phone",
      onHeaderCell: () => ({ style: { whiteSpace: "nowrap" } }),
      onCell: () => ({ style: { whiteSpace: "nowrap" } }),
    },
    {
      title: "Vai trò",
      dataIndex: "role",
      responsive: ["lg"],
      onHeaderCell: () => ({ style: { whiteSpace: "nowrap" } }),
      onCell: () => ({ style: { whiteSpace: "nowrap" } }),
      render: (v) => (v ? <Tag>{v}</Tag> : "-"),
    },
    {
      title: "Trạng thái",
      key: "status",
      render: (_, r) => {
        const active = !r.terminationDate && r.isActive;

        return active ? (
          <Tag icon={<CheckCircleOutlined />} color="success">
            Active
          </Tag>
        ) : (
          <Tag color="default">Inactive</Tag>
        );
      },
      onHeaderCell: () => ({ style: { whiteSpace: "nowrap" } }),
      onCell: () => ({ style: { whiteSpace: "nowrap" } }),
    },
    {
      title: "Thao tác",
      key: "actions",
      onHeaderCell: () => ({ style: { whiteSpace: "nowrap" } }),
      onCell: () => ({ style: { whiteSpace: "nowrap" } }),
      render: (_, record) => {
        const active = !record.terminationDate;
        const items = [
          { key: "view", icon: <EyeOutlined />, label: "Xem chi tiết" },
          { key: "edit", icon: <EditOutlined />, label: "Chỉnh sửa" },
          { type: "divider" },
          {
            key: "toggle",
            icon: <PoweroffOutlined />,
            label: active ? "Khóa tài khoản" : "Kích hoạt",
            danger: active,
          },
        ];
        return (
          <Dropdown
            trigger={["click"]}
            menu={{ items, onClick: (info) => onMenuClick(info, record) }}
            placement="bottomRight"
            disabled={!schemaName}
          >
            <Button
              type="text"
              icon={<MoreOutlined />}
              disabled={!schemaName}
            />
          </Dropdown>
        );
      },
    },
  ];

  // Building options
  const buildingOptions = useMemo(
    () =>
      (buildings || []).map((b) => ({
        value: b.id,
        label: (
          <Flex gap={8} align="center">
            <HomeOutlined />
            <span>{b.buildingName}</span>
            <Tag style={{ marginLeft: 8 }}>{b.code}</Tag>
          </Flex>
        ),
      })),
    [buildings]
  );

  // Role options
  const roleOptions = useMemo(
    () =>
      (roles || []).map((r) => ({
        value: r.roleId,
        label: r.roleName,
      })),
    [roles]
  );

  return (
    <Card
      title="Danh sách nhân sự"
      extra={
        <Space>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={handleAddStaff}
          >
            Tạo tài khoản nhân sự
          </Button>
          <Button
            onClick={() => loadData()}
            icon={<ReloadOutlined />}
            type="default"
            disabled={!schemaName}
          >
            Tải lại dữ liệu
          </Button>
        </Space>
      }
      styles={{ header: { borderBottom: "1px solid var(--ant-color-border)" } }}
    >
      {/* Filters */}
      <Card size="small" style={{ marginBottom: 16 }}>
        <Flex gap={12} wrap="wrap">
          <Select
            style={{ minWidth: 280 }}
            placeholder="Chọn toà nhà"
            options={buildingOptions}
            value={buildingId}
            onChange={(v) => {
              setBuildingId(v);
              setPaging((p) => ({ ...p, current: 1 }));
            }}
            allowClear
            showSearch
            optionFilterProp="label"
          />

          <Select
            style={{ minWidth: 220 }}
            placeholder="Lọc theo vai trò"
            options={roleOptions}
            value={roleId}
            onChange={(v) => {
              setRoleId(v || null);
              setPaging((p) => ({ ...p, current: 1 }));
            }}
            allowClear
            showSearch
            optionFilterProp="label"
            disabled={!schemaName}
          />

          <Input
            style={{ minWidth: 320 }}
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            onPressEnter={() =>
              setPaging((p) => ({ ...p, current: 1 })) || loadData()
            }
            prefix={<SearchOutlined />}
            allowClear
            placeholder="Tìm theo tên / email / số điện thoại"
          />

          <Space>
            <Button
              type="primary"
              onClick={() => {
                setPaging((p) => ({ ...p, current: 1 }));
                loadData();
              }}
              disabled={!schemaName}
            >
              Tìm kiếm
            </Button>
            <Button onClick={resetFilters}>Xoá lọc</Button>
          </Space>
        </Flex>
      </Card>

      {/* Table */}
      <Table
        columns={columns}
        dataSource={data}
        rowKey="staffCode"
        loading={loading}
        tableLayout="auto"
        scroll={{ x: true }}
        pagination={{
          current: paging.current,
          pageSize: paging.pageSize,
          total,
          showSizeChanger: true,
          onChange: (current, pageSize) => setPaging({ current, pageSize }),
          showTotal: (t) => `${t} nhân sự`,
        }}
      />

      <StaffDetailModal
        open={detailOpen}
        loading={detailLoading}
        initialValues={detail}
        mode={detailMode}
        allowInlineEdit={true}
        onCancel={() => setDetailOpen(false)}
        onSubmit={handleSubmitDetail}
        roleOptions={roleOptions}
      />
    </Card>
  );
}
