import React, { useEffect, useMemo, useState } from "react";
import {
  Card,
  Row,
  Col,
  Typography,
  Select,
  Table,
  Tag,
  Dropdown,
  Space,
  Button,
  Modal,
  Descriptions,
  Avatar,
  message,
  Input,
  Tooltip,
} from "antd";
import {
  HomeOutlined,
  MoreOutlined,
  UserOutlined,
  EditOutlined,
  EyeOutlined,
  DeleteOutlined,
  IdcardOutlined,
  TeamOutlined,
  FileTextOutlined,
  PhoneOutlined,
  MailOutlined,
  BankOutlined,
  DollarOutlined,
  SearchOutlined,
  LockOutlined,
  LoginOutlined,
} from "@ant-design/icons";

const { Title, Text } = Typography;
const { Option } = Select;

// =============== MOCK DATA & FAKE API LAYER ===============

// Tòa nhà
const MOCK_BUILDINGS = [
  { id: "bld-01", name: "Noah Tower A" },
  { id: "bld-02", name: "Noah Tower B" },
  { id: "bld-03", name: "Axonium 01" },
];

// Cư dân / tài khoản portal (nhiều thông tin hơn)
let MOCK_RESIDENTS = [
  {
    id: "res-001",
    buildingId: "bld-01",
    buildingName: "Noah Tower A",
    apartment: "A-101",
    floor: 10,
    tower: "A",
    fullName: "Nguyễn Văn A",
    gender: "male",
    phone: "0901234567",
    email: "a.nguyen@example.com",
    status: "ACTIVE", // ACTIVE | MOVED_OUT | TEMPORARY | LOCKED
    isHouseholdHead: true,
    avatarUrl:
      "https://cellphones.com.vn/sforum/wp-content/uploads/2023/10/avatar-trang-4.jpg",
    residenceType: "Owner", // Owner | Tenant
    portal: {
      username: "nguyenvana",
      roles: ["resident"],
      isLocked: false,
      lastLoginAt: "2025-10-12T19:30:00+07:00",
    },
    billing: {
      outstandingBalance: 450000,
      lastInvoiceMonth: "2025-10",
      hasAutoDebit: true,
      bankName: "Vietcombank",
      bankLast4: "1234",
    },
    contractId: "ctr-001",
    createdAt: "2024-01-10",
    updatedAt: "2025-10-13",
  },
  {
    id: "res-002",
    buildingId: "bld-01",
    buildingName: "Noah Tower A",
    apartment: "A-102",
    floor: 10,
    tower: "A",
    fullName: "Trần Thị B",
    gender: "female",
    phone: "0912345678",
    email: "b.tran@example.com",
    status: "ACTIVE",
    isHouseholdHead: false,
    avatarUrl:
      "https://cellphones.com.vn/sforum/wp-content/uploads/2023/10/avatar-trang-4.jpg",
    residenceType: "Tenant",
    portal: {
      username: "tranthib",
      roles: ["resident"],
      isLocked: false,
      lastLoginAt: "2025-11-01T08:15:00+07:00",
    },
    billing: {
      outstandingBalance: 0,
      lastInvoiceMonth: "2025-09",
      hasAutoDebit: false,
      bankName: null,
      bankLast4: null,
    },
    contractId: "ctr-002",
    createdAt: "2024-03-05",
    updatedAt: "2025-09-30",
  },
  {
    id: "res-003",
    buildingId: "bld-01",
    buildingName: "Noah Tower A",
    apartment: "A-1505",
    floor: 15,
    tower: "A",
    fullName: "Lê Minh C",
    gender: "male",
    phone: "0988888888",
    email: "c.le@example.com",
    status: "TEMPORARY",
    isHouseholdHead: false,
    avatarUrl:
      "https://cellphones.com.vn/sforum/wp-content/uploads/2023/10/avatar-trang-4.jpg",
    residenceType: "Tenant",
    portal: {
      username: "leminhc",
      roles: ["resident"],
      isLocked: false,
      lastLoginAt: "2025-09-10T21:00:00+07:00",
    },
    billing: {
      outstandingBalance: 1200000,
      lastInvoiceMonth: "2025-10",
      hasAutoDebit: false,
      bankName: "TPBank",
      bankLast4: "5678",
    },
    contractId: "ctr-003",
    createdAt: "2025-08-01",
    updatedAt: "2025-10-20",
  },
  {
    id: "res-004",
    buildingId: "bld-02",
    buildingName: "Noah Tower B",
    apartment: "B-201",
    floor: 2,
    tower: "B",
    fullName: "Phạm Văn D",
    gender: "male",
    phone: "0977777777",
    email: "d.pham@example.com",
    status: "MOVED_OUT",
    isHouseholdHead: true,
    avatarUrl:
      "https://cellphones.com.vn/sforum/wp-content/uploads/2023/10/avatar-trang-4.jpg",
    residenceType: "Owner",
    portal: {
      username: "phamvand",
      roles: ["resident"],
      isLocked: true,
      lastLoginAt: "2024-12-01T09:00:00+07:00",
    },
    billing: {
      outstandingBalance: 0,
      lastInvoiceMonth: "2024-11",
      hasAutoDebit: false,
      bankName: null,
      bankLast4: null,
    },
    contractId: "ctr-004",
    createdAt: "2023-05-10",
    updatedAt: "2024-12-05",
  },
  {
    id: "res-005",
    buildingId: "bld-02",
    buildingName: "Noah Tower B",
    apartment: "B-808",
    floor: 8,
    tower: "B",
    fullName: "Ngô Thị E",
    gender: "female",
    phone: "0933333333",
    email: "e.ngo@example.com",
    status: "ACTIVE",
    isHouseholdHead: true,
    avatarUrl:
      "https://cellphones.com.vn/sforum/wp-content/uploads/2023/10/avatar-trang-4.jpg",
    residenceType: "Owner",
    portal: {
      username: "ngothie",
      roles: ["resident"],
      isLocked: false,
      lastLoginAt: "2025-11-10T18:05:00+07:00",
    },
    billing: {
      outstandingBalance: 230000,
      lastInvoiceMonth: "2025-11",
      hasAutoDebit: true,
      bankName: "MB Bank",
      bankLast4: "9988",
    },
    contractId: "ctr-005",
    createdAt: "2024-06-20",
    updatedAt: "2025-11-10",
  },
  {
    id: "res-006",
    buildingId: "bld-03",
    buildingName: "Axonium 01",
    apartment: "C-1203",
    floor: 12,
    tower: "C",
    fullName: "Nguyễn Hoàng F",
    gender: "male",
    phone: "0966666666",
    email: "f.nguyen@example.com",
    status: "LOCKED",
    isHouseholdHead: true,
    avatarUrl:
      "https://cellphones.com.vn/sforum/wp-content/uploads/2023/10/avatar-trang-4.jpg",
    residenceType: "Tenant",
    portal: {
      username: "nguyenhoangf",
      roles: ["resident"],
      isLocked: true,
      lastLoginAt: "2025-09-01T07:30:00+07:00",
    },
    billing: {
      outstandingBalance: 3200000,
      lastInvoiceMonth: "2025-08",
      hasAutoDebit: false,
      bankName: "BIDV",
      bankLast4: "4455",
    },
    contractId: "ctr-006",
    createdAt: "2024-10-01",
    updatedAt: "2025-09-05",
  },
];

// Chi tiết hợp đồng / hộ / thành viên, giả lập theo residentId
const MOCK_RESIDENT_DETAILS = {
  "res-001": {
    householdType: "Gia đình",
    householdHead: "Nguyễn Văn A",
    occupants: [
      { name: "Nguyễn Văn A", relation: "Chủ hộ", dob: "1988-02-10" },
      { name: "Trần Thị H", relation: "Vợ", dob: "1990-05-12" },
      { name: "Nguyễn Văn I", relation: "Con", dob: "2015-03-01" },
    ],
    contract: {
      contractNo: "HD-2024-001",
      contractType: "Hợp đồng mua bán",
      startDate: "2024-01-15",
      endDate: "Không thời hạn",
      deposit: 0,
      rent: 0,
      status: "Đang hiệu lực",
    },
    vehicles: [
      { plate: "30H-123.45", type: "Ô tô", slot: "B2-15" },
      { plate: "29X1-678.90", type: "Xe máy", slot: "B1-88" },
    ],
    notes:
      "Gia đình thân thiện, thường tham gia các hoạt động cộng đồng của tòa nhà.",
  },
  "res-002": {
    householdType: "Nhóm thuê",
    householdHead: "Lê Quốc J",
    occupants: [
      { name: "Trần Thị B", relation: "Thành viên", dob: "1996-07-20" },
      { name: "Lê Quốc J", relation: "Chủ hợp đồng", dob: "1994-11-02" },
    ],
    contract: {
      contractNo: "HD-2024-002",
      contractType: "Hợp đồng cho thuê",
      startDate: "2024-03-10",
      endDate: "2025-03-10",
      deposit: 15000000,
      rent: 12000000,
      status: "Đang hiệu lực",
    },
    vehicles: [{ plate: "29D2-456.78", type: "Xe máy", slot: "B1-21" }],
    notes: "Nhóm sinh viên thuê, ít ở nhà vào giờ hành chính.",
  },
  "res-003": {
    householdType: "Tạm trú",
    householdHead: "Lê Minh C",
    occupants: [
      { name: "Lê Minh C", relation: "Chủ hộ", dob: "1992-01-05" },
      { name: "Nguyễn Thị K", relation: "Bạn ở cùng", dob: "1993-09-09" },
    ],
    contract: {
      contractNo: "HD-2025-003",
      contractType: "Hợp đồng cho thuê ngắn hạn",
      startDate: "2025-08-01",
      endDate: "2026-02-01",
      deposit: 8000000,
      rent: 10000000,
      status: "Đang hiệu lực",
    },
    vehicles: [],
    notes: "Khách hàng công tác, thường xuyên ra vào ngoài giờ hành chính.",
  },
  "res-004": {
    householdType: "Gia đình",
    householdHead: "Phạm Văn D",
    occupants: [
      { name: "Phạm Văn D", relation: "Chủ hộ cũ", dob: "1975-04-10" },
      { name: "Trần Thị L", relation: "Vợ", dob: "1978-03-08" },
    ],
    contract: {
      contractNo: "HD-2020-004",
      contractType: "Hợp đồng mua bán",
      startDate: "2020-01-01",
      endDate: "2024-12-01",
      deposit: 0,
      rent: 0,
      status: "Đã kết thúc",
    },
    vehicles: [],
    notes: "Đã chuyển đi tháng 12/2024, hiện căn hộ để trống.",
  },
  "res-005": {
    householdType: "Gia đình",
    householdHead: "Ngô Thị E",
    occupants: [
      { name: "Ngô Thị E", relation: "Chủ hộ", dob: "1989-03-12" },
      { name: "Trần Minh M", relation: "Chồng", dob: "1987-06-30" },
      { name: "Trần Ngô N", relation: "Con", dob: "2018-09-15" },
    ],
    contract: {
      contractNo: "HD-2024-005",
      contractType: "Hợp đồng mua bán",
      startDate: "2024-06-25",
      endDate: "Không thời hạn",
      deposit: 0,
      rent: 0,
      status: "Đang hiệu lực",
    },
    vehicles: [
      { plate: "30G-999.99", type: "Ô tô", slot: "B3-09" },
      { plate: "29Y3-111.22", type: "Xe máy", slot: "B1-03" },
    ],
    notes: "Gia đình có trẻ nhỏ, thường đăng ký sử dụng tiện ích hồ bơi.",
  },
  "res-006": {
    householdType: "Nhóm thuê",
    householdHead: "Nguyễn Hoàng F",
    occupants: [
      { name: "Nguyễn Hoàng F", relation: "Chủ hợp đồng", dob: "1990-10-10" },
      { name: "Lưu Thị O", relation: "Bạn ở cùng", dob: "1991-01-22" },
      { name: "Đặng Văn P", relation: "Bạn ở cùng", dob: "1993-02-14" },
    ],
    contract: {
      contractNo: "HD-2024-006",
      contractType: "Hợp đồng cho thuê",
      startDate: "2024-10-05",
      endDate: "2025-10-05",
      deposit: 15000000,
      rent: 13000000,
      status: "Đang hiệu lực",
    },
    vehicles: [
      { plate: "30F-555.66", type: "Ô tô", slot: "B2-22" },
      { plate: "29B1-222.33", type: "Xe máy", slot: "B1-77" },
    ],
    notes:
      "Tài khoản portal từng bị tạm khóa do nợ phí kéo dài, đã nhắc nhở nhiều lần.",
  },
};

// Giả lập delay mạng
function fakeDelay(ms = 500) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// Giả lập API
async function apiFetchBuildings() {
  await fakeDelay(200);
  return MOCK_BUILDINGS;
}

/**
 * params: { buildingId, status, search, page, pageSize }
 */
async function apiFetchResidents(params = {}) {
  await fakeDelay(400);
  const {
    buildingId,
    status = "ALL",
    search = "",
    page = 1,
    pageSize = 10,
  } = params;

  let data = [...MOCK_RESIDENTS];

  if (buildingId) {
    data = data.filter((r) => r.buildingId === buildingId);
  }
  if (status && status !== "ALL") {
    data = data.filter((r) => r.status === status);
  }
  if (search) {
    const s = search.toLowerCase();
    data = data.filter(
      (r) =>
        r.fullName.toLowerCase().includes(s) ||
        r.apartment.toLowerCase().includes(s) ||
        (r.phone && r.phone.toLowerCase().includes(s))
    );
  }

  const total = data.length;
  const start = (page - 1) * pageSize;
  const end = start + pageSize;
  const pageData = data.slice(start, end);

  return { data: pageData, total };
}

async function apiFetchResidentAccount(residentId) {
  await fakeDelay(300);
  const resident = MOCK_RESIDENTS.find((r) => r.id === residentId);
  if (!resident) throw new Error("Resident not found");
  return resident;
}

async function apiFetchResidentDetail(residentId) {
  await fakeDelay(300);
  const base = MOCK_RESIDENTS.find((r) => r.id === residentId);
  if (!base) throw new Error("Resident not found");
  const extra = MOCK_RESIDENT_DETAILS[residentId];
  return { ...base, detail: extra || null };
}

async function apiDeleteResident(residentId) {
  await fakeDelay(300);
  MOCK_RESIDENTS = MOCK_RESIDENTS.filter((r) => r.id !== residentId);
  return { success: true };
}

// =============== UI PAGE COMPONENT ===============

const STATUS_OPTIONS = [
  { value: "ALL", label: "Tất cả trạng thái" },
  { value: "ACTIVE", label: "Đang ở" },
  { value: "TEMPORARY", label: "Tạm trú" },
  { value: "MOVED_OUT", label: "Đã chuyển đi" },
  { value: "LOCKED", label: "Tài khoản bị khóa" },
];

function formatStatusTag(status) {
  switch (status) {
    case "ACTIVE":
      return <Tag color="green">Đang ở</Tag>;
    case "TEMPORARY":
      return <Tag color="purple">Tạm trú</Tag>;
    case "MOVED_OUT":
      return <Tag color="default">Đã chuyển đi</Tag>;
    case "LOCKED":
      return (
        <Tag color="red" icon={<LockOutlined />}>
          Bị khóa
        </Tag>
      );
    default:
      return <Tag>Không rõ</Tag>;
  }
}

function formatCurrency(v) {
  if (v == null) return "-";
  return `${v.toLocaleString("vi-VN")}₫`;
}

export default function ResidentAccountListPage() {
  const [buildings, setBuildings] = useState([]);
  const [buildingLoading, setBuildingLoading] = useState(false);

  const [selectedBuilding, setSelectedBuilding] = useState("bld-01");
  const [statusFilter, setStatusFilter] = useState("ALL");
  const [searchText, setSearchText] = useState("");

  const [listLoading, setListLoading] = useState(false);
  const [residents, setResidents] = useState([]);
  const [pagination, setPagination] = useState({
    current: 1,
    pageSize: 10,
    total: 0,
  });

  const [accountModalVisible, setAccountModalVisible] = useState(false);
  const [accountModalMode, setAccountModalMode] = useState("view"); // view | edit
  const [accountLoading, setAccountLoading] = useState(false);
  const [selectedResident, setSelectedResident] = useState(null);

  const [detailModalVisible, setDetailModalVisible] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [residentDetail, setResidentDetail] = useState(null);

  // ➕ State cho modal thành viên
  const [memberModalVisible, setMemberModalVisible] = useState(false);
  const [selectedMember, setSelectedMember] = useState(null);

  // Load buildings
  useEffect(() => {
    (async () => {
      setBuildingLoading(true);
      try {
        const blds = await apiFetchBuildings();
        setBuildings(blds);
        if (!selectedBuilding && blds.length > 0) {
          setSelectedBuilding(blds[0].id);
        }
      } finally {
        setBuildingLoading(false);
      }
    })();
  }, []);

  // Load residents list khi filter / pagination thay đổi
  const loadResidents = async (options = {}) => {
    const page = options.page || pagination.current || 1;
    const pageSize = options.pageSize || pagination.pageSize || 10;

    setListLoading(true);
    try {
      const res = await apiFetchResidents({
        buildingId: selectedBuilding,
        status: statusFilter,
        search: searchText,
        page,
        pageSize,
      });

      setResidents(res.data);
      setPagination((prev) => ({
        ...prev,
        current: page,
        pageSize,
        total: res.total,
      }));
    } catch (e) {
      console.error(e);
      message.error("Không tải được danh sách cư dân.");
    } finally {
      setListLoading(false);
    }
  };

  useEffect(() => {
    if (!selectedBuilding) return;
    loadResidents({ page: 1 });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedBuilding, statusFilter, searchText]);

  const handleChangeBuilding = (value) => {
    setSelectedBuilding(value);
  };

  const handleChangeStatus = (value) => {
    setStatusFilter(value);
  };

  const handleTableChange = (paginationConfig) => {
    loadResidents({
      page: paginationConfig.current,
      pageSize: paginationConfig.pageSize,
    });
  };

  const handleSearch = (value) => {
    setSearchText(value.trim());
  };

  const openAccountModal = async (record, mode = "view") => {
    setAccountModalMode(mode);
    setAccountModalVisible(true);
    setAccountLoading(true);
    try {
      const full = await apiFetchResidentAccount(record.id);
      setSelectedResident(full);
    } catch (e) {
      console.error(e);
      message.error("Không tải được thông tin tài khoản cư dân.");
      setAccountModalVisible(false);
    } finally {
      setAccountLoading(false);
    }
  };

  const closeAccountModal = () => {
    setAccountModalVisible(false);
    setSelectedResident(null);
  };

  const handleDeleteResident = (resident) => {
    Modal.confirm({
      title: "Xóa cư dân",
      content: `Bạn có chắc chắn muốn xóa cư dân "${resident.fullName}" (căn hộ ${resident.apartment})?`,
      okText: "Xóa",
      okType: "danger",
      cancelText: "Hủy",
      async onOk() {
        try {
          await apiDeleteResident(resident.id);
          message.success("Đã xóa cư dân khỏi hệ thống (mock).");
          // reload list
          loadResidents({ page: 1 });
        } catch (e) {
          console.error(e);
          message.error("Không xóa được cư dân.");
        }
      },
    });
  };

  const handleSaveAccount = () => {
    // Ở đây bạn gọi API update thật, mình mock message
    message.success("Đã lưu thông tin tài khoản cư dân (mock).");
    setAccountModalMode("view");
  };

  const openDetailModal = async () => {
    if (!selectedResident) return;
    setDetailModalVisible(true);
    setDetailLoading(true);
    try {
      const detail = await apiFetchResidentDetail(selectedResident.id);
      setResidentDetail(detail);
    } catch (e) {
      console.error(e);
      message.error("Không tải được chi tiết cư dân.");
      setDetailModalVisible(false);
    } finally {
      setDetailLoading(false);
    }
  };

  const closeDetailModal = () => {
    setDetailModalVisible(false);
    setResidentDetail(null);
  };

  // ➕ Handler modal thành viên
  const openMemberModal = (member) => {
    setSelectedMember(member);
    setMemberModalVisible(true);
  };

  const closeMemberModal = () => {
    setMemberModalVisible(false);
    setSelectedMember(null);
  };

  // Columns Table
  const columns = [
    {
      title: "Căn hộ",
      dataIndex: "apartment",
      key: "apartment",
      width: 110,
      render: (val, record) => (
        <Space direction="vertical" size={0}>
          <Tag color="blue">{val}</Tag>
          <Text type="secondary" style={{ fontSize: 11 }}>
            Tầng {record.floor} · Tháp {record.tower}
          </Text>
        </Space>
      ),
    },
    {
      title: "Cư dân",
      dataIndex: "fullName",
      key: "fullName",
      render: (text, record) => (
        <Space>
          <div>
            <div>{text}</div>
            <div style={{ fontSize: 11, color: "#888" }}>
              {record.residenceType === "Owner" ? "Chủ sở hữu" : "Người thuê"}
              {record.isHouseholdHead && " · Chủ hộ"}
            </div>
          </div>
        </Space>
      ),
    },
    {
      title: "Liên hệ",
      key: "contact",
      width: 220,
      render: (_, record) => (
        <Space direction="vertical" size={0}>
          <span>
            <PhoneOutlined className="mr-1" /> {record.phone}
          </span>
          <span style={{ fontSize: 11, color: "#888" }}>
            <MailOutlined className="mr-1" />
            {record.email}
          </span>
        </Space>
      ),
    },
    {
      title: "Portal account",
      key: "portal",
      width: 230,
      render: (_, record) => (
        <Space direction="vertical" size={0}>
          <span>
            <UserOutlined className="mr-1" /> {record.portal.username}
          </span>
          <span style={{ fontSize: 11, color: "#888" }}>
            <LoginOutlined className="mr-1" />
            Lần đăng nhập gần nhất:{" "}
            {record.portal.lastLoginAt
              ? new Date(record.portal.lastLoginAt).toLocaleString("vi-VN")
              : "Chưa đăng nhập"}
          </span>
          {record.portal.isLocked && (
            <Tag color="red" size="small" icon={<LockOutlined />}>
              Tài khoản bị khóa
            </Tag>
          )}
        </Space>
      ),
    },
    {
      title: "Công nợ",
      key: "billing",
      width: 180,
      render: (_, record) => (
        <Space direction="vertical" size={0}>
          <span>
            <DollarOutlined className="mr-1" />
            Công nợ: {formatCurrency(record.billing.outstandingBalance)}
          </span>
          <span style={{ fontSize: 11, color: "#888" }}>
            Kỳ hóa đơn gần nhất: {record.billing.lastInvoiceMonth || "-"}
          </span>
          {record.billing.hasAutoDebit && (
            <span style={{ fontSize: 11 }}>
              <BankOutlined className="mr-1" />
              Trích nợ: {record.billing.bankName} ••••
              {record.billing.bankLast4}
            </span>
          )}
        </Space>
      ),
    },
    {
      title: "Trạng thái cư trú",
      dataIndex: "status",
      key: "status",
      width: 150,
      render: (val) => formatStatusTag(val),
    },
    {
      title: "",
      key: "actions",
      width: 60,
      align: "right",
      render: (_, record) => {
        const menuItems = [
          {
            key: "view",
            label: (
              <span>
                <EyeOutlined className="mr-2" />
                Xem tài khoản
              </span>
            ),
            onClick: () => openAccountModal(record, "view"),
          },
          {
            key: "edit",
            label: (
              <span>
                <EditOutlined className="mr-2" />
                Sửa tài khoản
              </span>
            ),
            onClick: () => openAccountModal(record, "edit"),
          },
          {
            key: "delete",
            danger: true,
            label: (
              <span>
                <DeleteOutlined className="mr-2" />
                Xóa cư dân
              </span>
            ),
            onClick: () => handleDeleteResident(record),
          },
        ];

        return (
          <Dropdown
            menu={{ items: menuItems }}
            trigger={["click"]}
            placement="bottomRight"
          >
            <Button
              type="text"
              icon={<MoreOutlined />}
              onClick={(e) => e.stopPropagation()}
            />
          </Dropdown>
        );
      },
    },
  ];

  // Dữ liệu cho modal tài khoản
  const buildingNameById = useMemo(() => {
    const map = {};
    buildings.forEach((b) => {
      map[b.id] = b.name;
    });
    return map;
  }, [buildings]);

  return (
    <div className="mx-auto">
      {/* Header */}
      <Row justify="space-between" align="middle" className="mb-4">
        <Col>
          <Title level={3} className="!mb-1">
            <HomeOutlined className="mr-2 text-blue-600" />
            Tài khoản cư dân
          </Title>
          <Text type="secondary">
            Quản lý danh sách tài khoản cư dân theo từng tòa nhà, trạng thái và
            công nợ.
          </Text>
        </Col>
        <Col>
          <Space wrap>
            <Space>
              <Text type="secondary">Tòa nhà:</Text>
              <Select
                style={{ minWidth: 200 }}
                value={selectedBuilding}
                loading={buildingLoading}
                onChange={handleChangeBuilding}
              >
                {buildings.map((b) => (
                  <Option key={b.id} value={b.id}>
                    {b.name}
                  </Option>
                ))}
              </Select>
            </Space>

            <Space>
              <Text type="secondary">Trạng thái:</Text>
              <Select
                style={{ minWidth: 180 }}
                value={statusFilter}
                onChange={handleChangeStatus}
              >
                {STATUS_OPTIONS.map((s) => (
                  <Option key={s.value} value={s.value}>
                    {s.label}
                  </Option>
                ))}
              </Select>
            </Space>

            <Input.Search
              allowClear
              placeholder="Tìm theo tên, căn hộ, SĐT"
              style={{ minWidth: 220 }}
              onSearch={handleSearch}
              prefix={<SearchOutlined />}
            />
          </Space>
        </Col>
      </Row>

      {/* Card + Table */}
      <Card className="shadow-sm">
        <Table
          rowKey="id"
          columns={columns}
          dataSource={residents}
          loading={listLoading}
          pagination={pagination}
          onChange={handleTableChange}
          locale={{ emptyText: "Không có cư dân nào trong tòa nhà này." }}
        />
      </Card>

      {/* Modal thông tin tài khoản cư dân */}
      <Modal
        open={accountModalVisible}
        onCancel={closeAccountModal}
        title={
          <Space>
            <UserOutlined />
            <span>
              {accountModalMode === "edit"
                ? "Sửa thông tin tài khoản cư dân"
                : "Thông tin tài khoản cư dân"}
            </span>
          </Space>
        }
        okText={accountModalMode === "edit" ? "Lưu thay đổi" : "Đóng"}
        onOk={() =>
          accountModalMode === "edit"
            ? handleSaveAccount()
            : closeAccountModal()
        }
        cancelButtonProps={{
          style: accountModalMode === "edit" ? {} : { display: "none" },
        }}
        width={720}
      >
        {accountLoading || !selectedResident ? (
          <Text type="secondary">Đang tải dữ liệu...</Text>
        ) : (
          <>
            <Space align="center" className="mb-4">
              <Avatar
                size={56}
                src={selectedResident.avatarUrl}
                icon={!selectedResident.avatarUrl && <UserOutlined />}
              />
              <div>
                <div className="font-semibold text-base">
                  {selectedResident.fullName}{" "}
                  {selectedResident.isHouseholdHead && (
                    <Tag color="gold" icon={<IdcardOutlined />}>
                      Chủ hộ
                    </Tag>
                  )}
                </div>
                <div className="text-xs text-gray-500">
                  {buildingNameById[selectedResident.buildingId]} · Căn hộ{" "}
                  {selectedResident.apartment}
                </div>
              </div>
            </Space>

            {/* Thông tin tài khoản & portal */}
            <Descriptions
              column={2}
              size="small"
              bordered
              labelStyle={{ width: 180 }}
            >
              <Descriptions.Item label="Tài khoản portal">
                {selectedResident.portal.username}
              </Descriptions.Item>
              <Descriptions.Item label="Trạng thái tài khoản">
                {selectedResident.portal.isLocked ? (
                  <Tag color="red" icon={<LockOutlined />}>
                    Bị khóa
                  </Tag>
                ) : (
                  <Tag color="green">Hoạt động</Tag>
                )}
              </Descriptions.Item>

              <Descriptions.Item label="Vai trò trên portal">
                {selectedResident.portal.roles?.join(", ")}
              </Descriptions.Item>
              <Descriptions.Item label="Lần đăng nhập gần nhất">
                {selectedResident.portal.lastLoginAt
                  ? new Date(
                      selectedResident.portal.lastLoginAt
                    ).toLocaleString("vi-VN")
                  : "Chưa đăng nhập"}
              </Descriptions.Item>

              <Descriptions.Item label="Email">
                {selectedResident.email}
              </Descriptions.Item>
              <Descriptions.Item label="Số điện thoại">
                {selectedResident.phone}
              </Descriptions.Item>

              <Descriptions.Item label="Loại cư trú">
                {selectedResident.residenceType === "Owner"
                  ? "Chủ sở hữu"
                  : "Người thuê"}
              </Descriptions.Item>
              <Descriptions.Item label="Trạng thái cư trú">
                {formatStatusTag(selectedResident.status)}
              </Descriptions.Item>

              <Descriptions.Item label="Công nợ hiện tại">
                {formatCurrency(selectedResident.billing.outstandingBalance)}
              </Descriptions.Item>
              <Descriptions.Item label="Kỳ hóa đơn gần nhất">
                {selectedResident.billing.lastInvoiceMonth || "-"}
              </Descriptions.Item>

              <Descriptions.Item label="Trích nợ tự động">
                {selectedResident.billing.hasAutoDebit ? (
                  <span>
                    {selectedResident.billing.bankName} ••••
                    {selectedResident.billing.bankLast4}
                  </span>
                ) : (
                  "Không"
                )}
              </Descriptions.Item>
              <Descriptions.Item label="Mã hợp đồng">
                {selectedResident.contractId || "-"}
              </Descriptions.Item>
            </Descriptions>

            <div className="mt-4 flex justify-between items-center">
              <Tooltip title="Xem chi tiết thông tin cư dân (hợp đồng, số người ở, xe, ghi chú…)">
                <Button
                  type="link"
                  icon={<FileTextOutlined />}
                  onClick={openDetailModal}
                >
                  Xem chi tiết cư dân
                </Button>
              </Tooltip>

              {accountModalMode === "view" && (
                <Button
                  icon={<EditOutlined />}
                  onClick={() => setAccountModalMode("edit")}
                >
                  Chuyển sang chế độ sửa
                </Button>
              )}
            </div>
          </>
        )}
      </Modal>

      {/* Modal chi tiết cư dân */}
      <Modal
        open={detailModalVisible}
        onCancel={closeDetailModal}
        title={
          <Space>
            <TeamOutlined />
            <span>Chi tiết cư dân</span>
          </Space>
        }
        footer={
          <Button type="primary" onClick={closeDetailModal}>
            Đóng
          </Button>
        }
        width={900}
      >
        {detailLoading || !residentDetail ? (
          <Text type="secondary">Đang tải chi tiết...</Text>
        ) : (
          <>
            {/* Hộ gia đình */}
            <Descriptions
              title={
                <Space>
                  <UserOutlined />
                  <span>Thông tin hộ / nhóm cư trú</span>
                </Space>
              }
              bordered
              size="small"
              column={2}
              className="mb-4"
            >
              <Descriptions.Item label="Tòa nhà">
                {buildingNameById[residentDetail.buildingId]}
              </Descriptions.Item>
              <Descriptions.Item label="Căn hộ">
                {residentDetail.apartment} (Tầng {residentDetail.floor} · Tháp{" "}
                {residentDetail.tower})
              </Descriptions.Item>
              <Descriptions.Item label="Loại hộ">
                {residentDetail.detail?.householdType || "-"}
              </Descriptions.Item>
              <Descriptions.Item label="Chủ hộ">
                {residentDetail.detail?.householdHead || "-"}
              </Descriptions.Item>
              <Descriptions.Item label="Số người ở">
                {residentDetail.detail?.occupants?.length ?? "-"}
              </Descriptions.Item>
              <Descriptions.Item label="Trạng thái cư trú">
                {formatStatusTag(residentDetail.status)}
              </Descriptions.Item>
            </Descriptions>

            {/* Hợp đồng */}
            {residentDetail.detail?.contract && (
              <Descriptions
                title={
                  <Space>
                    <FileTextOutlined />
                    <span>Thông tin hợp đồng</span>
                  </Space>
                }
                bordered
                size="small"
                column={2}
                className="mb-4"
              >
                <Descriptions.Item label="Mã hợp đồng">
                  {residentDetail.detail.contract.contractNo}
                </Descriptions.Item>
                <Descriptions.Item label="Loại hợp đồng">
                  {residentDetail.detail.contract.contractType}
                </Descriptions.Item>
                <Descriptions.Item label="Ngày bắt đầu">
                  {residentDetail.detail.contract.startDate}
                </Descriptions.Item>
                <Descriptions.Item label="Ngày kết thúc">
                  {residentDetail.detail.contract.endDate}
                </Descriptions.Item>
                <Descriptions.Item label="Tiền cọc">
                  {formatCurrency(residentDetail.detail.contract.deposit)}
                </Descriptions.Item>
                <Descriptions.Item label="Tiền thuê / tháng">
                  {formatCurrency(residentDetail.detail.contract.rent)}
                </Descriptions.Item>
                <Descriptions.Item label="Trạng thái hợp đồng">
                  {residentDetail.detail.contract.status}
                </Descriptions.Item>
              </Descriptions>
            )}

            {/* Thành viên trong hộ */}
            <Descriptions
              title={
                <Space>
                  <TeamOutlined />
                  <span>Thành viên trong hộ</span>
                </Space>
              }
              bordered
              size="small"
              column={1}
              className="mb-4"
            >
              {residentDetail.detail?.occupants?.length ? (
                residentDetail.detail.occupants.map((o, idx) => (
                  <Descriptions.Item key={idx} label={o.relation}>
                    <Space direction="vertical" size={0}>
                      <Space>
                        <UserOutlined />
                        <Button
                          type="link"
                          size="small"
                          onClick={() =>
                            openMemberModal({
                              ...o,
                              apartment: residentDetail.apartment,
                              buildingName:
                                buildingNameById[residentDetail.buildingId],
                            })
                          }
                        >
                          {o.name}
                        </Button>
                      </Space>
                      <Text type="secondary" style={{ fontSize: 12 }}>
                        Ngày sinh: {o.dob}
                      </Text>
                    </Space>
                  </Descriptions.Item>
                ))
              ) : (
                <Descriptions.Item label="Thành viên">
                  Không có dữ liệu.
                </Descriptions.Item>
              )}
            </Descriptions>

            {/* Xe & ghi chú */}
            <Descriptions
              title={
                <Space>
                  <HomeOutlined />
                  <span>Xe & ghi chú quản lý</span>
                </Space>
              }
              bordered
              size="small"
              column={1}
            >
              <Descriptions.Item label="Phương tiện">
                {residentDetail.detail?.vehicles?.length ? (
                  <ul style={{ paddingLeft: 16, margin: 0 }}>
                    {residentDetail.detail.vehicles.map((v, idx) => (
                      <li key={idx}>
                        {v.type} – biển số {v.plate} – chỗ đậu {v.slot}
                      </li>
                    ))}
                  </ul>
                ) : (
                  "Không đăng ký phương tiện."
                )}
              </Descriptions.Item>
              <Descriptions.Item label="Ghi chú quản lý">
                {residentDetail.detail?.notes || "Không có ghi chú."}
              </Descriptions.Item>
            </Descriptions>
          </>
        )}
      </Modal>

      {/* Modal thông tin thành viên căn hộ */}
      <Modal
        open={memberModalVisible}
        onCancel={closeMemberModal}
        title={
          <Space>
            <UserOutlined />
            <span>Thông tin thành viên căn hộ</span>
          </Space>
        }
        footer={
          <Button type="primary" onClick={closeMemberModal}>
            Đóng
          </Button>
        }
        width={480}
      >
        {selectedMember ? (
          <Descriptions
            bordered
            size="small"
            column={1}
            labelStyle={{ width: 180 }}
          >
            <Descriptions.Item label="Họ tên">
              {selectedMember.name}
            </Descriptions.Item>
            <Descriptions.Item label="Quan hệ với chủ hộ">
              {selectedMember.relation}
            </Descriptions.Item>
            <Descriptions.Item label="Ngày sinh">
              {selectedMember.dob}
            </Descriptions.Item>
            {selectedMember.buildingName && (
              <Descriptions.Item label="Tòa nhà">
                {selectedMember.buildingName}
              </Descriptions.Item>
            )}
            {selectedMember.apartment && (
              <Descriptions.Item label="Căn hộ">
                {selectedMember.apartment}
              </Descriptions.Item>
            )}
          </Descriptions>
        ) : (
          <Text type="secondary">Không có dữ liệu thành viên.</Text>
        )}
      </Modal>
    </div>
  );
}
