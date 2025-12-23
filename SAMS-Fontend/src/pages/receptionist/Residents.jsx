import React, { useCallback, useEffect, useMemo, useState } from "react";
import { useLocation } from "react-router-dom";
import {
    Card,
    Input,
    Table,
    Space,
    Button,
    message,
    Modal,
    Descriptions,
    Tag,
    Tabs,
    List,
    Avatar,
    Typography,
    Row,
    Col,
    Statistic,
    Empty,
    Spin,
    Divider,
} from "antd";
import {
    ReloadOutlined,
    SearchOutlined,
    EyeOutlined,
    HomeOutlined,
    UserOutlined,
    PhoneOutlined,
    MailOutlined,
    FileTextOutlined,
    InfoCircleOutlined,
} from "@ant-design/icons";
import api from "../../lib/apiClient";
import ticketsApi from "../../features/tickets/ticketsApi";
import dayjs from "dayjs";

const { Text, Title } = Typography;
const { TabPane } = Tabs;

const defaultQuery = {
    keyword: "",
    apartmentId: undefined,
    apartmentNumber: "",
    ownerName: "",
    page: 1,
    pageSize: 10,
};

export default function Residents() {
    const location = useLocation();
    const [query, setQuery] = useState(defaultQuery);
    const [loading, setLoading] = useState(false);
    const [data, setData] = useState({ total: 0, items: [] });
    const [statsTotal, setStatsTotal] = useState({ total: 0, withOwner: 0, empty: 0 });
    const [detailModalOpen, setDetailModalOpen] = useState(false);
    const [selectedApartment, setSelectedApartment] = useState(null);
    const [apartmentDetail, setApartmentDetail] = useState(null);
    const [apartmentResidents, setApartmentResidents] = useState([]);
    const [apartmentTickets, setApartmentTickets] = useState([]);
    const [detailLoading, setDetailLoading] = useState(false);
    const [expandedRowKeys, setExpandedRowKeys] = useState([]);

    const fetchList = useCallback(async () => {
        setLoading(true);
        try {
            // Nếu có ownerName, ưu tiên tìm theo tên chủ hộ
            // Nếu có apartmentNumber, tìm theo số căn hộ
            const params = {
                page: query.page,
                pageSize: query.pageSize,
            };

            if (query.ownerName && query.ownerName.trim()) {
                // Tìm theo tên cư dân (tất cả cư dân, không chỉ chủ căn hộ)
                params.ownerName = query.ownerName.trim();
            } else if (query.apartmentNumber && query.apartmentNumber.trim()) {
                // Tìm theo số căn hộ
                params.number = query.apartmentNumber.trim();
            } else if (query.keyword && query.keyword.trim()) {
                // Fallback: tìm theo keyword như số căn hộ
                params.number = query.keyword.trim();
            }

            const res = await api.get("/Apartment/lookup", { params });
            const items = res.data?.items || [];

            // Nếu có apartmentId cụ thể, filter để chỉ hiển thị căn hộ đó
            let filteredItems = items;
            if (query.apartmentId) {
                filteredItems = items.filter(item => item.apartmentId === query.apartmentId);
            }

            // Khử trùng theo apartmentId
            // Nếu đang tìm theo ownerName, ưu tiên giữ record có matchedResidentName khớp với query
            const uniqMap = new Map();
            const searchName = query.ownerName && query.ownerName.trim() ? query.ownerName.trim().toLowerCase() : null;

            filteredItems.forEach((x) => {
                if (!uniqMap.has(x.apartmentId)) {
                    uniqMap.set(x.apartmentId, x);
                } else if (searchName) {
                    // Nếu đang tìm theo ownerName, ưu tiên record có matchedResidentName khớp tốt hơn
                    const existing = uniqMap.get(x.apartmentId);
                    const existingMatched = existing.matchedResidentName ? existing.matchedResidentName.toLowerCase() : "";
                    const currentMatched = x.matchedResidentName ? x.matchedResidentName.toLowerCase() : "";

                    const existingExact = existingMatched === searchName;
                    const currentExact = currentMatched === searchName;
                    const existingContains = existingMatched.includes(searchName);
                    const currentContains = currentMatched.includes(searchName);

                    // Ưu tiên: exact match > contains > không khớp
                    if (currentExact && !existingExact) {
                        // Record hiện tại exact match, record cũ không -> thay thế
                        uniqMap.set(x.apartmentId, x);
                    } else if (currentExact === existingExact && currentContains && !existingContains) {
                        // Cùng exact match status, nhưng record hiện tại contains -> thay thế
                        uniqMap.set(x.apartmentId, x);
                    }
                }
            });
            const uniq = Array.from(uniqMap.values());
            const total = res.data?.total ?? uniq.length;
            setData({ total, items: uniq });
        } catch (e) {
            console.error("Residents fetch error:", e);
            message.error("Không tải được danh sách cư dân/căn hộ");
            setData({ total: 0, items: [] });
        } finally {
            setLoading(false);
        }
    }, [query.keyword, query.apartmentId, query.apartmentNumber, query.ownerName, query.page, query.pageSize]);

    // Đọc query parameters từ URL khi component mount hoặc location thay đổi
    useEffect(() => {
        const searchParams = new URLSearchParams(location.search);
        const keyword = searchParams.get("search") || "";
        const apartmentId = searchParams.get("apartmentId") || undefined;
        const apartmentNumber = searchParams.get("apartmentNumber") || "";
        const ownerName = searchParams.get("ownerName") || "";

        // Cập nhật query từ URL params - luôn cập nhật để đảm bảo sync với URL
        setQuery((prev) => {
            // Kiểm tra xem có thay đổi không
            const hasChanges =
                prev.keyword !== keyword ||
                prev.apartmentId !== apartmentId ||
                prev.apartmentNumber !== apartmentNumber ||
                prev.ownerName !== ownerName;

            if (hasChanges) {
                return {
                    ...prev,
                    keyword: keyword,
                    apartmentId: apartmentId,
                    apartmentNumber: apartmentNumber,
                    ownerName: ownerName,
                    page: 1, // Reset về trang 1 khi có search params mới
                };
            }
            return prev;
        });
    }, [location.search]);

    // Fetch list khi query thay đổi (bao gồm cả khi load từ URL params)
    // Sử dụng trực tiếp các query params để đảm bảo fetch lại khi thay đổi
    useEffect(() => {
        fetchList();
    }, [query.keyword, query.apartmentId, query.apartmentNumber, query.ownerName, query.page, query.pageSize, fetchList]);

    // Fetch totals (dedup to tránh đếm trùng do LEFT JOIN)
    const fetchTotals = useCallback(async () => {
        try {
            const res = await api.get("/Apartment/lookup", {
                params: { number: query.keyword || undefined, page: 1, pageSize: 10000 },
            });
            const raw = res.data?.items || [];
            // Deduplicate by apartmentId
            const uniqMap = new Map();
            raw.forEach((x) => {
                if (!uniqMap.has(x.apartmentId)) {
                    uniqMap.set(x.apartmentId, x);
                }
            });
            const uniq = Array.from(uniqMap.values());
            const withOwner = uniq.filter((x) => !!x.ownerName && String(x.ownerName).trim() !== "").length;
            const total = uniq.length;
            setStatsTotal({ total, withOwner, empty: Math.max(total - withOwner, 0) });
        } catch (e) {
            // Fallback về số liệu trang hiện tại nếu lỗi
            const current = data.items || [];
            const withOwner = current.filter((x) => !!x.ownerName && String(x.ownerName).trim() !== "").length;
            setStatsTotal({ total: data.total || current.length, withOwner, empty: Math.max((data.total || current.length) - withOwner, 0) });
        }
    }, [query.keyword, data.items, data.total]);

    useEffect(() => {
        fetchTotals();
    }, [fetchTotals]);

    const fetchApartmentDetail = useCallback(async (apartmentId) => {
        setDetailLoading(true);
        try {
            // Fetch apartment summary (contains owner info)
            const summaryRes = await api.get(`/Apartment/${apartmentId}/summary`);
            const summary = summaryRes.data;
            setApartmentDetail(summary);

            // Fetch apartment full details
            try {
                const detailRes = await api.get(`/Apartment/${apartmentId}`);
                const apartment = detailRes.data;
                // Merge apartment details with summary
                setApartmentDetail({ ...summary, ...apartment });
            } catch (e) {
                console.error("Error fetching apartment details:", e);
                // Use summary if detail fetch fails
            }

            // Fetch residents in this apartment via new API
            try {
                const membersRes = await api.get(`/Residents/apartment/${apartmentId}`);
                const members = membersRes.data || [];
                setApartmentResidents(members);
            } catch (e) {
                console.error("Error fetching apartment residents:", e);
                // Fallback: chỉ có chủ hộ từ summary
                if (summary?.ownerName) {
                    setApartmentResidents([
                        {
                            fullName: summary.ownerName,
                            isPrimary: true,
                        },
                    ]);
                } else {
                    setApartmentResidents([]);
                }
            }

            // Fetch tickets for this apartment
            try {
                const ticketsRes = await ticketsApi.search({
                    page: 1,
                    pageSize: 10,
                });
                const allTickets = ticketsRes.items || [];
                const apartmentTickets = allTickets.filter(
                    (ticket) => ticket.apartmentId === apartmentId
                );
                setApartmentTickets(apartmentTickets);
            } catch (e) {
                console.error("Error fetching tickets:", e);
                setApartmentTickets([]);
            }
        } catch (e) {
            console.error("Error fetching apartment detail:", e);
            message.error("Không tải được thông tin chi tiết căn hộ");
        } finally {
            setDetailLoading(false);
        }
    }, []);

    const handleViewDetail = async (record) => {
        setSelectedApartment(record);
        setDetailModalOpen(true);
        let aptId = record?.apartmentId;
        // Fallback: nếu thiếu apartmentId, tra theo số căn hộ để lấy id
        if (!aptId && record?.number) {
            try {
                const res = await api.get(`/Apartment/number/${encodeURIComponent(record.number)}`);
                aptId = res.data?.apartmentId;
            } catch (e) {
                console.error("Cannot resolve apartmentId from number:", e);
            }
        }
        if (aptId) {
            fetchApartmentDetail(aptId);
        } else {
            message.error("Không xác định được ID căn hộ");
        }
    };

    // Không lọc lại theo keyword tại client để tránh làm sai kết quả tìm theo số căn hộ (VD: A0808)
    const filteredItems = useMemo(() => data.items, [data.items]);

    const columns = [
        {
            title: "Số căn hộ",
            dataIndex: "number",
            key: "number",
            width: 180,
            render: (text, record) => (
                <Space>
                    <HomeOutlined style={{ color: "#1890ff" }} />
                    <Text strong>{text}</Text>
                </Space>
            ),
        },
        {
            title: "Chủ hộ",
            dataIndex: "ownerName",
            key: "ownerName",
            width: 250,
            render: (v) => (
                <Space>
                    <UserOutlined />
                    <Text>{v || "Chưa có chủ hộ"}</Text>
                </Space>
            ),
        },
        {
            title: "Thao tác",
            key: "actions",
            width: 150,
            render: (_, record) => (
                <Button
                    type="link"
                    icon={<EyeOutlined />}
                    onClick={() => handleViewDetail(record)}
                >
                    Xem chi tiết
                </Button>
            ),
        },
    ];

    const expandedRowRender = (record) => {
        return (
            <div style={{ padding: "16px", background: "#fafafa" }}>
                <Row gutter={16}>
                    <Col span={12}>
                        <Card size="small" title="Thông tin căn hộ">
                            <Descriptions column={1} size="small">
                                <Descriptions.Item label="Số căn hộ">
                                    {record.number}
                                </Descriptions.Item>
                                <Descriptions.Item label="Chủ hộ">
                                    {record.ownerName || "Chưa có"}
                                </Descriptions.Item>
                            </Descriptions>
                        </Card>
                    </Col>
                    <Col span={12}>
                        <Card size="small" title="Thao tác nhanh">
                            <Space direction="vertical" style={{ width: "100%" }}>
                                <Button
                                    type="primary"
                                    block
                                    icon={<EyeOutlined />}
                                    onClick={() => handleViewDetail(record)}
                                >
                                    Xem chi tiết đầy đủ
                                </Button>
                            </Space>
                        </Card>
                    </Col>
                </Row>
            </div>
        );
    };

    const ticketStatusColors = {
        "Mới tạo": "blue",
        "Chờ xử lý": "orange",
        "Đang xử lý": "gold",
        "Đã đóng": "green",
    };

    const ticketPriorityColors = {
        "Thấp": "default",
        "Bình thường": "blue",
        "Trung bình": "cyan",
        "Cao": "orange",
        "Khẩn cấp": "red",
    };

    return (
        <div className="p-6">
            <div className="mb-6">
                <Title level={2} style={{ marginBottom: 8 }}>
                    Quản lý Cư dân & Căn hộ
                </Title>
                <Text type="secondary">
                    Quản lý thông tin cư dân và căn hộ trong tòa nhà
                </Text>
            </div>

            {/* Search Bar */}
            <Card className="mb-4">
                <Space wrap>
                    <Input
                        placeholder="Tìm theo số căn hộ hoặc tên chủ hộ"
                        value={query.keyword}
                        onChange={(e) =>
                            setQuery((q) => ({ ...q, keyword: e.target.value, page: 1 }))
                        }
                        allowClear
                        style={{ width: 400 }}
                        onPressEnter={fetchList}
                        prefix={<SearchOutlined />}
                    />
                    <Button type="primary" icon={<SearchOutlined />} onClick={fetchList}>
                        Tìm kiếm
                    </Button>
                    <Button
                        icon={<ReloadOutlined />}
                        onClick={() => {
                            setQuery(defaultQuery);
                            fetchList();
                        }}
                    >
                        Đặt lại
                    </Button>
                </Space>
            </Card>

            {/* Statistics */}
            <Row gutter={16} className="mb-4">
                <Col xs={24} sm={8}>
                    <Card>
                        <Statistic
                            title="Tổng số căn hộ"
                            value={statsTotal.total}
                            prefix={<HomeOutlined />}
                        />
                    </Card>
                </Col>
                <Col xs={24} sm={8}>
                    <Card>
                        <Statistic
                            title="Căn hộ có chủ"
                            value={statsTotal.withOwner}
                            prefix={<UserOutlined />}
                        />
                    </Card>
                </Col>
                <Col xs={24} sm={8}>
                    <Card>
                        <Statistic
                            title="Căn hộ trống"
                            value={statsTotal.empty}
                            prefix={<InfoCircleOutlined />}
                        />
                    </Card>
                </Col>
            </Row>

            {/* Table */}
            <Card>
                <Table
                    rowKey={(r) => r.apartmentId}
                    columns={columns}
                    dataSource={filteredItems}
                    loading={loading}
                    expandable={{
                        expandedRowRender,
                        expandedRowKeys,
                        onExpandedRowsChange: setExpandedRowKeys,
                    }}
                    pagination={{
                        current: query.page,
                        pageSize: query.pageSize,
                        total: data.total,
                        showSizeChanger: true,
                        showTotal: (total) => `Tổng ${total} căn hộ`,
                        onChange: (p, ps) =>
                            setQuery((q) => ({ ...q, page: p, pageSize: ps })),
                    }}
                />
            </Card>

            {/* Detail Modal */}
            <Modal
                title={
                    <Space>
                        <HomeOutlined />
                        <span>Chi tiết căn hộ: {selectedApartment?.number}</span>
                    </Space>
                }
                open={detailModalOpen}
                onCancel={() => {
                    setDetailModalOpen(false);
                    setSelectedApartment(null);
                    setApartmentDetail(null);
                    setApartmentResidents([]);
                    setApartmentTickets([]);
                }}
                footer={null}
                width={900}
                destroyOnClose
            >
                <Spin spinning={detailLoading}>
                    {apartmentDetail ? (
                        <Tabs defaultActiveKey="1">
                            <TabPane
                                tab={
                                    <span>
                                        <InfoCircleOutlined />
                                        Thông tin
                                    </span>
                                }
                                key="1"
                            >
                                <Descriptions bordered column={2}>
                                    <Descriptions.Item label="Số căn hộ" span={2}>
                                        <Text strong style={{ fontSize: 16 }}>{apartmentDetail.number}</Text>
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Chủ hộ">
                                        {apartmentDetail.ownerName || apartmentDetail.ownerInfo?.fullName || (
                                            <Text type="secondary">Chưa có</Text>
                                        )}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Số điện thoại">
                                        {apartmentDetail.ownerInfo?.phone || (
                                            <Text type="secondary">—</Text>
                                        )}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Email">
                                        {apartmentDetail.ownerInfo?.email || (
                                            <Text type="secondary">—</Text>
                                        )}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Tầng">
                                        {apartmentDetail.floorNumber ? `Tầng ${apartmentDetail.floorNumber}` : (
                                            apartmentDetail.floorName || <Text type="secondary">—</Text>
                                        )}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Tên tầng">
                                        {apartmentDetail.floorName || (
                                            <Text type="secondary">—</Text>
                                        )}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Diện tích">
                                        {apartmentDetail.areaM2 ? `${apartmentDetail.areaM2} m²` : (
                                            <Text type="secondary">—</Text>
                                        )}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Số phòng ngủ">
                                        {apartmentDetail.bedrooms !== null && apartmentDetail.bedrooms !== undefined ? (
                                            `${apartmentDetail.bedrooms} phòng`
                                        ) : (
                                            <Text type="secondary">—</Text>
                                        )}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Loại căn hộ">
                                        {apartmentDetail.type || (
                                            <Text type="secondary">—</Text>
                                        )}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Trạng thái">
                                        <Tag color={apartmentDetail.status === "ACTIVE" ? "green" : "default"}>
                                            {apartmentDetail.status === "ACTIVE" ? "Hoạt động" : apartmentDetail.status || "—"}
                                        </Tag>
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Số cư dân">
                                        {apartmentDetail.residentCount !== undefined ? (
                                            <Text strong>{apartmentDetail.residentCount} người</Text>
                                        ) : (
                                            <Text type="secondary">—</Text>
                                        )}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Số phương tiện">
                                        {apartmentDetail.vehicleCount !== undefined ? (
                                            <Text strong>{apartmentDetail.vehicleCount} xe</Text>
                                        ) : (
                                            <Text type="secondary">—</Text>
                                        )}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Ngày tạo">
                                        {apartmentDetail.createdAt ? (
                                            dayjs(apartmentDetail.createdAt).format("DD/MM/YYYY HH:mm")
                                        ) : (
                                            <Text type="secondary">—</Text>
                                        )}
                                    </Descriptions.Item>
                                </Descriptions>
                            </TabPane>

                            <TabPane
                                tab={
                                    <span>
                                        <UserOutlined />
                                        Cư dân ({apartmentResidents.length})
                                    </span>
                                }
                                key="2"
                            >
                                {apartmentResidents.length > 0 ? (
                                    <List
                                        dataSource={apartmentResidents}
                                        renderItem={(resident) => (
                                            <List.Item>
                                                <List.Item.Meta
                                                    avatar={
                                                        <Avatar
                                                            icon={<UserOutlined />}
                                                            style={{
                                                                backgroundColor: resident.isPrimary
                                                                    ? "#1890ff"
                                                                    : "#52c41a",
                                                            }}
                                                        />
                                                    }
                                                    title={
                                                        <Space>
                                                            <Text strong>
                                                                {resident.fullName ||
                                                                    "Chưa có tên"}
                                                            </Text>
                                                            {resident.isPrimary && (
                                                                <Tag color="blue">Chủ hộ</Tag>
                                                            )}
                                                        </Space>
                                                    }
                                                    description={
                                                        <Space direction="vertical" size="small">
                                                            {resident.phone && (
                                                                <Space>
                                                                    <PhoneOutlined />
                                                                    <Text>{resident.phone}</Text>
                                                                </Space>
                                                            )}
                                                            {resident.email && (
                                                                <Space>
                                                                    <MailOutlined />
                                                                    <Text>{resident.email}</Text>
                                                                </Space>
                                                            )}
                                                            {resident.address && (
                                                                <Text type="secondary">
                                                                    {resident.address}
                                                                </Text>
                                                            )}
                                                        </Space>
                                                    }
                                                />
                                            </List.Item>
                                        )}
                                    />
                                ) : (
                                    <Empty
                                        description="Chưa có thông tin cư dân"
                                        image={Empty.PRESENTED_IMAGE_SIMPLE}
                                    />
                                )}
                            </TabPane>

                            <TabPane
                                tab={
                                    <span>
                                        <FileTextOutlined />
                                        Yêu cầu ({apartmentTickets.length})
                                    </span>
                                }
                                key="3"
                            >
                                {apartmentTickets.length > 0 ? (
                                    <List
                                        dataSource={apartmentTickets}
                                        renderItem={(ticket) => (
                                            <List.Item>
                                                <List.Item.Meta
                                                    avatar={
                                                        <Avatar
                                                            icon={<FileTextOutlined />}
                                                            style={{
                                                                backgroundColor: "#52c41a",
                                                            }}
                                                        />
                                                    }
                                                    title={
                                                        <Space>
                                                            <Text
                                                                strong
                                                                style={{ cursor: "pointer" }}
                                                                onClick={() => {
                                                                    window.open(
                                                                        `/receptionist/tickets/${ticket.ticketId}`,
                                                                        "_blank"
                                                                    );
                                                                }}
                                                            >
                                                                {ticket.subject}
                                                            </Text>
                                                            <Tag
                                                                color={
                                                                    ticketStatusColors[
                                                                    ticket.status
                                                                    ] || "default"
                                                                }
                                                            >
                                                                {ticket.status}
                                                            </Tag>
                                                            <Tag
                                                                color={
                                                                    ticketPriorityColors[
                                                                    ticket.priority
                                                                    ] || "default"
                                                                }
                                                            >
                                                                {ticket.priority}
                                                            </Tag>
                                                        </Space>
                                                    }
                                                    description={
                                                        <Space direction="vertical" size="small">
                                                            <Text type="secondary">
                                                                {ticket.description ||
                                                                    "Không có mô tả"}
                                                            </Text>
                                                            <Space>
                                                                <Text type="secondary" style={{ fontSize: 12 }}>
                                                                    Tạo:{" "}
                                                                    {dayjs(ticket.createdAt).format(
                                                                        "DD/MM/YYYY HH:mm"
                                                                    )}
                                                                </Text>
                                                                {ticket.createdByUserName && (
                                                                    <>
                                                                        <Divider type="vertical" />
                                                                        <Text type="secondary" style={{ fontSize: 12 }}>
                                                                            Người tạo:{" "}
                                                                            {ticket.createdByUserName}
                                                                        </Text>
                                                                    </>
                                                                )}
                                                            </Space>
                                                        </Space>
                                                    }
                                                />
                                            </List.Item>
                                        )}
                                    />
                                ) : (
                                    <Empty
                                        description="Chưa có yêu cầu nào"
                                        image={Empty.PRESENTED_IMAGE_SIMPLE}
                                    />
                                )}
                            </TabPane>
                        </Tabs>
                    ) : (
                        <Empty description="Không tìm thấy thông tin" />
                    )}
                </Spin>
            </Modal>
        </div>
    );
}
