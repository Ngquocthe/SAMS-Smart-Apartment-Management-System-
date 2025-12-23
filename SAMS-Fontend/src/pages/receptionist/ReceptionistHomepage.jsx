import React, { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { Input, Table, Card, message, Button } from "antd";
import { SearchOutlined, UserOutlined, FileTextOutlined, CameraOutlined, FolderOpenOutlined } from "@ant-design/icons";
import ticketsApi from "../../features/tickets/ticketsApi";
import documentsApi from "../../features/documents/documentsApi";
import residentsApi from "../../features/residents/residentsApi";
import dayjs from "dayjs";

export default function ReceptionistHomepage() {
    const navigate = useNavigate();
    const [loading, setLoading] = useState(false);
    const [stats, setStats] = useState({
        openRequests: 0,
        residents: 0,
        documents: 0,
    });
    const [ticketsData, setTicketsData] = useState({ total: 0, items: [] });
    // NOTE: Đã bỏ News Section khỏi dashboard

    // Search state - tìm kiếm đơn giản giống trang Residents
    const [searchKeyword, setSearchKeyword] = useState("");


    // Fetch dashboard data
    const fetchDashboardData = useCallback(async () => {
        setLoading(true);
        try {
            // Fetch all tickets to count non-closed ones
            const allTicketsResult = await ticketsApi.search({
                page: 1,
                pageSize: 1000, // Lấy số lượng lớn để đếm chính xác
            });

            // Fetch closed tickets count
            const closedTicketsResult = await ticketsApi.search({
                status: "Đã đóng",
                page: 1,
                pageSize: 1, // Chỉ cần total, không cần items
            });

            // Calculate non-closed tickets count
            const totalTickets = allTicketsResult.total || 0;
            const closedTickets = closedTicketsResult.total || 0;
            const nonClosedTickets = totalTickets - closedTickets;

            // Fetch recent tickets for table (non-closed only)
            const recentTicketsResult = await ticketsApi.search({
                page: 1,
                pageSize: 5,
            });

            // Filter out closed tickets from recent tickets
            const nonClosedRecentTickets = (recentTicketsResult.items || []).filter(
                ticket => ticket.status !== "Đã đóng"
            );

            // Fetch active documents count (status = ACTIVE)
            let activeDocuments = 0;
            try {
                const docsResult = await documentsApi.search({
                    status: "ACTIVE",
                    page: 1,
                    pageSize: 1, // chỉ cần total
                });
                activeDocuments = docsResult?.total || 0;
            } catch (e) {
                console.error("Error fetching active documents:", e);
            }

            // Fetch residents count
            let residentsCount = 0;
            try {
                const residentsList = await residentsApi.getAll();
                residentsCount = Array.isArray(residentsList) ? residentsList.length : 0;
            } catch (e) {
                console.error("Error fetching residents:", e);
            }

            // Update stats
            setStats({
                openRequests: nonClosedTickets,
                residents: residentsCount,
                documents: activeDocuments,
            });

            setTicketsData({
                total: nonClosedRecentTickets.length,
                items: nonClosedRecentTickets,
            });
        } catch (e) {
            console.error("Error fetching dashboard data:", e);
            message.error("Không tải được dữ liệu dashboard");
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchDashboardData();
    }, [fetchDashboardData]);

    // Table columns
    const columns = [
        {
            title: "Phạm vi",
            dataIndex: "apartmentNumber",
            key: "apartmentNumber",
            width: 200,
            render: (number, record) => number || (record.apartmentId ? "—" : "Tòa nhà"),
        },
        {
            title: "Tiêu đề",
            dataIndex: "subject",
            key: "subject",
            render: (value, record) => (
                <div
                    role="button"
                    onClick={() =>
                        window.location.assign(`/receptionist/tickets/${record.ticketId}`)
                    }
                    style={{ cursor: "pointer" }}
                    className="text-blue-600 hover:text-blue-800"
                >
                    {value || "—"}
                </div>
            ),
        },
        {
            title: "Thời gian hoàn thành dự kiến",
            dataIndex: "expectedCompletionAt",
            key: "expectedCompletionAt",
            width: 150,
            render: (date) => {
                if (!date) return "—";
                const due = dayjs(date);
                const now = dayjs();
                const isOverdue = due.isBefore(now);
                return (
                    <span className={isOverdue ? "text-red-600 font-semibold" : ""}>
                        {due.format("DD/MM/YYYY")}
                    </span>
                );
            },
        },
    ];


    // Handle search - điều hướng đến trang cư dân với keyword
    // Tách logic: nếu có số -> tìm theo số căn hộ, nếu chỉ chữ -> tìm theo tên cư dân/chủ hộ
    const handleSearch = () => {
        const keyword = searchKeyword?.trim();

        if (!keyword) {
            // Nếu không có keyword, vẫn điều hướng đến trang cư dân (hiển thị tất cả)
            navigate("/receptionist/residents");
            return;
        }

        const params = new URLSearchParams();

        // Nếu keyword có chứa chữ số -> ưu tiên coi là số căn hộ
        const hasDigit = /\d/.test(keyword);
        if (hasDigit) {
            params.append("apartmentNumber", keyword);
        } else {
            // Ngược lại: coi là tên cư dân/chủ hộ
            params.append("ownerName", keyword);
        }

        navigate(`/receptionist/residents?${params.toString()}`);
    };


    return (
        <div className="min-h-screen bg-white">
            {/* Search Bar */}
            <div className="px-6 py-4 bg-gray-50 border-b border-gray-200">
                <div className="flex items-center gap-2">
                    <Input
                        placeholder="Tìm theo số căn hộ hoặc tên chủ hộ"
                        value={searchKeyword}
                        onChange={(e) => setSearchKeyword(e.target.value)}
                        allowClear
                        size="large"
                        onPressEnter={handleSearch}
                        prefix={<SearchOutlined />}
                        style={{ maxWidth: "400px" }}
                    />
                    <Button
                        type="primary"
                        icon={<SearchOutlined />}
                        size="large"
                        onClick={handleSearch}
                    >
                        Tìm kiếm
                    </Button>
                </div>
            </div>

            {/* Main Content */}
            <div className="p-6">
                {/* Summary Cards */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
                    {/* Requests Card */}
                    <Card
                        className="shadow-md hover:shadow-lg transition-shadow cursor-pointer"
                        onClick={() => navigate("/receptionist/tickets")}
                    >
                        <div className="flex items-center gap-4">
                            <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center">
                                <FileTextOutlined className="text-2xl text-blue-600" />
                            </div>
                            <div>
                                <p className="text-sm text-gray-600 mb-1">Yêu cầu</p>
                                <p className="text-2xl font-bold text-gray-800">
                                    {stats.openRequests} yêu cầu xử lý
                                </p>
                            </div>
                        </div>
                    </Card>

                    {/* Residents Card */}
                    <Card
                        className="shadow-md hover:shadow-lg transition-shadow cursor-pointer"
                        onClick={() => navigate("/receptionist/residents")}
                    >
                        <div className="flex items-center gap-4">
                            <div className="w-12 h-12 bg-purple-100 rounded-lg flex items-center justify-center">
                                <UserOutlined className="text-2xl text-purple-600" />
                            </div>
                            <div>
                                <p className="text-sm text-gray-600 mb-1">Cư dân</p>
                                <p className="text-2xl font-bold text-gray-800">
                                    {stats.residents} cư dân
                                </p>
                            </div>
                        </div>
                    </Card>

                    {/* Documents Card */}
                    <Card
                        className="shadow-md hover:shadow-lg transition-shadow cursor-pointer"
                        onClick={() => navigate("/receptionist/documents-management")}
                    >
                        <div className="flex items-center gap-4">
                            <div className="w-12 h-12 bg-green-100 rounded-lg flex items-center justify-center">
                                <FolderOpenOutlined className="text-2xl text-green-600" />
                            </div>
                            <div>
                                <p className="text-sm text-gray-600 mb-1">Tài liệu</p>
                                <p className="text-2xl font-bold text-gray-800">
                                    {stats.documents} tài liệu
                                </p>
                            </div>
                        </div>
                    </Card>
                </div>

                {/* Content Grid */}
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    {/* Left Column - Resident Requests */}
                    <div className="lg:col-span-2 space-y-6">
                        {/* Resident Request Table */}
                        <Card
                            title="Yêu cầu từ cư dân"
                            className="shadow-md"
                            extra={
                                <a
                                    href="/receptionist/tickets"
                                    className="text-blue-600 hover:text-blue-800 text-sm"
                                >
                                    View all
                                </a>
                            }
                        >
                            <Table
                                columns={columns}
                                dataSource={ticketsData.items}
                                rowKey={(record) => record.ticketId}
                                loading={loading}
                                pagination={false}
                                size="small"
                            />
                        </Card>

                    </div>

                    {/* Right Column - Quick Actions */}
                    <div className="space-y-6">
                        {/* Face Check-in Quick Access */}
                        <Card
                            title="Điểm danh bằng khuôn mặt"
                            className="shadow-md hover:shadow-lg transition-shadow"
                        >
                            <div className="space-y-4">
                                <div className="flex items-center gap-3">
                                    <button
                                        type="button"
                                        onClick={() => navigate("/receptionist/face-checkin?quickScan=true")}
                                        className="w-10 h-10 rounded-full bg-blue-50 flex items-center justify-center border border-transparent hover:border-blue-400 transition-colors"
                                        title="Bấm để mở quét nhanh khuôn mặt"
                                    >
                                        <CameraOutlined className="text-xl text-blue-600" />
                                    </button>
                                    <p className="text-sm text-gray-600">
                                        Bấm vào biểu tượng máy ảnh để mở ngay chế độ <b>Quét nhanh cư dân</b> bằng khuôn mặt.
                                    </p>
                                </div>
                            </div>
                        </Card>

                        {/* Document Management Quick Access */}
                        <Card
                            title="Quản lý tài liệu"
                            className="shadow-md hover:shadow-lg transition-shadow"
                        >
                            <div className="space-y-4">
                                <div className="flex items-center gap-3">
                                    <div className="w-10 h-10 rounded-full bg-green-50 flex items-center justify-center">
                                        <FolderOpenOutlined className="text-xl text-green-600" />
                                    </div>
                                    <p className="text-sm text-gray-600">
                                        Truy cập nhanh kho tài liệu nội bộ: thông báo, biểu mẫu, hướng dẫn cho cư dân.
                                    </p>
                                </div>
                                <Button
                                    type="primary"
                                    onClick={() => navigate("/receptionist/documents-management")}
                                    block
                                >
                                    Quản lý tài liệu
                                </Button>
                            </div>
                        </Card>
                    </div>
                </div>
            </div>
        </div>
    );
}

