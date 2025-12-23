import React, { useCallback, useEffect, useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { Button, Form, Input, Modal, Select, Table, Tag, Space, message, DatePicker, Upload } from "antd";
import { PlusOutlined, ReloadOutlined, EyeOutlined } from "@ant-design/icons";
import dayjs from "dayjs";
import ticketsApi from "../../features/tickets/ticketsApi";
import { useUser } from "../../hooks/useUser";
import api from "../../lib/apiClient";

const statusColors = {
    "Mới tạo": "blue",
    "Đã tiếp nhận": "cyan",
    "Đang xử lý": "gold",
    "Hoàn thành": "green",
    "Đã đóng": "default",
};

const categoryOptions = ["Bảo trì", "An ninh", "Hóa đơn", "Khiếu nại", "Vệ sinh", "Bãi đỗ xe", "Tiện ích", "Khác"].map(v => ({ label: v, value: v }));

const scopeOptions = ["Tòa nhà", "Theo căn hộ"].map(v => ({ label: v, value: v }));

// File validation constants for tickets
const MAX_FILE_SIZE_TICKET = 10 * 1024 * 1024; // 10MB
const MAX_FILES_COUNT = 5;
const ALLOWED_TICKET_TYPES = ".jpg,.jpeg,.png,.gif,.bmp,.webp,.pdf,.doc,.docx";
const DANGEROUS_EXTENSIONS = ['.exe', '.bat', '.cmd', '.com', '.scr', '.vbs', '.js', '.jar', '.msi', '.dll', '.sh', '.ps1'];

/**
 * Format kích thước file từ bytes sang định dạng dễ đọc (B, KB, MB)
 * @param {number} bytes - Kích thước file tính bằng bytes
 * @returns {string} Chuỗi định dạng kích thước (ví dụ: "1.5 MB")
 */
const formatFileSize = (bytes) => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
};

/**
 * Validate file upload cho ticket
 * Kiểm tra: kích thước file, phần mở rộng, loại file được phép, và các extension nguy hiểm
 * @param {File} file - File object cần validate
 * @returns {boolean|string} true nếu hợp lệ, Upload.LIST_IGNORE nếu không hợp lệ
 */
const validateTicketFile = (file) => {
    // Check file size
    if (file.size > MAX_FILE_SIZE_TICKET) {
        message.error(`File "${file.name}" quá lớn. Dung lượng tối đa: ${formatFileSize(MAX_FILE_SIZE_TICKET)}`);
        return Upload.LIST_IGNORE;
    }

    // Check file extension
    const fileName = file.name.toLowerCase();
    const extension = fileName.substring(fileName.lastIndexOf('.'));

    if (!extension || extension === fileName) {
        message.error(`File "${file.name}" không có phần mở rộng.`);
        return Upload.LIST_IGNORE;
    }

    // Check dangerous extensions
    if (DANGEROUS_EXTENSIONS.includes(extension)) {
        message.error(`File "${file.name}" không được phép upload vì lý do bảo mật.`);
        return Upload.LIST_IGNORE;
    }

    // Check allowed types
    const allowedExtensions = ALLOWED_TICKET_TYPES.split(',');
    if (!allowedExtensions.some(ext => fileName.endsWith(ext.trim()))) {
        message.error(`File "${file.name}" không được hỗ trợ. Chỉ chấp nhận: ${ALLOWED_TICKET_TYPES}`);
        return Upload.LIST_IGNORE;
    }

    return true;
};

const defaultQuery = {
    keyword: "",
    status: undefined,
    fromDate: undefined,
    toDate: undefined,
    page: 1,
    pageSize: 10,
};

export default function TicketList() {
    const navigate = useNavigate();
    const location = useLocation();
    const [query, setQuery] = useState(defaultQuery);
    const [loading, setLoading] = useState(false);
    const [data, setData] = useState({ total: 0, items: [] });

    const [createOpen, setCreateOpen] = useState(false);
    const [createForm] = Form.useForm();
    const [selectedScope, setSelectedScope] = useState("Tòa nhà");
    const [apartmentOptions, setApartmentOptions] = useState([]);
    const [apartmentLoading, setApartmentLoading] = useState(false);
    const [fileList, setFileList] = useState([]);
    const { userId, user } = useUser();

    const columns = [
        {
            title: "Tiêu đề",
            dataIndex: "subject",
            key: "subject",
            render: (value, record) => (
                <div
                    role="button"
                    onClick={() => window.location.assign(`/receptionist/tickets/${record.ticketId}`)}
                    style={{ cursor: "pointer" }}
                >
                    <Space direction="vertical" size={0}>
                        <strong>{value}</strong>
                        <span style={{ fontSize: 12, color: "#888" }}>{record.category || "—"}</span>

                    </Space>
                </div>
            ),
            onCell: () => ({ style: { cursor: "pointer" } }),
        },
        {
            title: "Người tạo",
            dataIndex: "createdByUserName",
            key: "createdByUserName",
            width: 160,
            render: (name, record) => {
                if (name) return name;
                const v = record?.createdByUserId;
                if (!v) return "—";
                if (userId && String(v).toLowerCase() === String(userId).toLowerCase()) {
                    return user?.username || user?.fullName || "Bạn";
                }
                return "Người dùng";
            },
        },
        {
            title: "Ưu tiên",
            dataIndex: "priority",
            key: "priority",
            width: 110,
            render: (v) => v || "—",
        },
        {
            title: "Trạng thái",
            dataIndex: "status",
            key: "status",
            width: 130,
            render: (v) => {
                return (
                    <Tag color={statusColors[v] || "default"}>
                        {v || "—"}
                    </Tag>
                );
            },
        },
        {
            title: "Ngày tạo",
            dataIndex: "createdAt",
            key: "createdAt",
            width: 160,
            render: (date) => new Date(date).toLocaleDateString("vi-VN"),
        },
        {
            title: "Ngày hoàn thành",
            dataIndex: "expectedCompletionAt",
            key: "expectedCompletionAt",
            width: 160,
            render: (date) =>
                date ? dayjs(date).format("DD/MM/YYYY HH:mm") : "—",
        },
        {
            title: "Hành động",
            key: "actions",
            width: 100,
            render: (_, record) => (
                <Space>
                    <Button
                        type="link"
                        icon={<EyeOutlined />}
                        onClick={() => navigate(`/receptionist/tickets/${record.ticketId}`)}
                        size="small"
                    >
                        Xem chi tiết
                    </Button>
                </Space>
            ),
        },
    ];

    /**
     * Lấy danh sách tickets từ API với các filter hiện tại
     * Tự động cập nhật state data và loading
     */
    const fetchList = useCallback(async () => {
        setLoading(true);
        try {
            const result = await ticketsApi.search({
                search: query.keyword || undefined,
                status: query.status || undefined,
                fromDate: query.fromDate || undefined,
                toDate: query.toDate || undefined,
                page: query.page,
                pageSize: query.pageSize,
            });
            setData({ total: result.total || 0, items: result.items || [] });
        } catch (e) {
            message.error("Không tải được danh sách yêu cầu");
        } finally {
            setLoading(false);
        }
    }, [query.keyword, query.status, query.fromDate, query.toDate, query.page, query.pageSize]);

    // Đọc query parameters từ URL khi component mount hoặc location thay đổi
    useEffect(() => {
        const searchParams = new URLSearchParams(location.search);
        const keyword = searchParams.get("search") || "";
        const status = searchParams.get("status") || undefined;
        const apartmentId = searchParams.get("apartmentId") || undefined;
        const apartmentNumber = searchParams.get("apartmentNumber") || undefined;
        const ownerName = searchParams.get("ownerName") || undefined;

        // Cập nhật query từ URL params
        setQuery((prev) => ({
            ...prev,
            keyword: keyword,
            status: status,
            page: 1, // Reset về trang 1 khi có search params mới
        }));

        // Lưu các params khác nếu cần (có thể dùng cho filter nâng cao sau này)
        if (apartmentId || apartmentNumber || ownerName) {
            // Có thể mở rộng để lưu vào state nếu cần
        }
    }, [location.search]);

    // Fetch list khi query thay đổi (bao gồm cả khi load từ URL params)
    useEffect(() => {
        fetchList();
    }, [query.keyword, query.status, query.fromDate, query.toDate, query.page, query.pageSize]); // eslint-disable-line

    /**
     * Xử lý khi người dùng click nút tìm kiếm/lọc
     * Reset về trang 1 để hiển thị kết quả mới
     * fetchList sẽ tự động được gọi bởi useEffect khi query thay đổi
     */
    const handleSearch = () => {
        // Chỉ cần set page=1; fetchList đã được trigger bởi useEffect khi query đổi
        setQuery((q) => ({ ...q, page: 1 }));
    };

    /**
     * Tìm kiếm căn hộ theo số căn hộ (apartment number)
     * Chỉ tìm kiếm khi người dùng nhập ít nhất 2 ký tự
     * Kết quả hiển thị dạng: "Số căn hộ - Tên chủ hộ"
     * @param {string} searchText - Text để tìm kiếm căn hộ
     */
    const searchApartments = async (searchText) => {
        if (!searchText || searchText.length < 2) {
            setApartmentOptions([]);
            return;
        }

        setApartmentLoading(true);
        try {
            const response = await api.get("/Apartment/lookup", {
                params: {
                    number: searchText,
                    page: 1,
                    pageSize: 10
                }
            });

            const options = response.data.items.map(item => ({
                value: item.apartmentId,
                label: `${item.number}${item.ownerName ? ` - ${item.ownerName}` : ''}`
            }));
            setApartmentOptions(options);
        } catch (error) {
            console.error("Error searching apartments:", error);
            message.error("Không thể tìm kiếm căn hộ");
            setApartmentOptions([]);
        } finally {
            setApartmentLoading(false);
        }
    };

    /**
     * Xử lý submit form tạo ticket mới
     * Quy trình:
     * 1. Validate form fields
     * 2. Tạo ticket qua API
     * 3. Upload attachments nếu có (sau khi tạo ticket thành công)
     * 4. Reset form và reload danh sách tickets
     * 5. Hiển thị thông báo kết quả
     */
    const submitCreate = async () => {
        console.log("=== SUBMIT CREATE START ===");
        const close = message.loading("Đang tạo yêu cầu...", 0);
        try {
            console.log("Validating form fields...");
            const values = await createForm.validateFields();
            console.log("Form values:", values);
            console.log("Selected scope:", selectedScope);
            console.log("User ID:", userId);

            // Tạo yêu cầu trước
            const payload = {
                category: String(values.category || "").trim(),
                priority: String(values.priority || "").trim(),
                subject: String(values.subject || "").trim(),
                description: values.description ? String(values.description).trim() : undefined,
                createdByUserId: userId || undefined,
                scope: selectedScope,
                apartmentId: selectedScope === "Theo căn hộ" ? (values.apartmentId || undefined) : undefined,
            };



            const ticketResponse = await ticketsApi.create(payload);


            const ticketId = ticketResponse.ticketId || ticketResponse.TicketId;

            // Upload attachments nếu có
            if (fileList.length > 0) {
                // Validate all files before upload
                const invalidFiles = [];
                fileList.forEach((file) => {
                    if (file.originFileObj) {
                        const validation = validateTicketFile(file.originFileObj);
                        if (validation === Upload.LIST_IGNORE) {
                            invalidFiles.push(file.name);
                        }
                    }
                });

                if (invalidFiles.length > 0) {
                    close();
                    message.error(`Có ${invalidFiles.length} file không hợp lệ. Vui lòng kiểm tra lại.`);
                    return;
                }

                console.log("Uploading attachments:", fileList.length, "files");
                const formData = new FormData();
                fileList.forEach((file) => {
                    if (file.originFileObj) {
                        formData.append('files', file.originFileObj);
                    }
                });
                formData.append('ticketId', ticketId);

                try {
                    await api.post('/Ticket/attachments/upload', formData, {
                        headers: {
                            'Content-Type': 'multipart/form-data',
                        },
                    });
                    console.log("Attachments uploaded successfully");
                } catch (uploadError) {
                    console.error('Upload attachments failed:', uploadError);
                    message.warning('Ticket đã tạo nhưng upload ảnh thất bại');
                }
            }

            close();
            message.success("Đã tạo yêu cầu");
            setCreateOpen(false);
            createForm.resetFields();
            setSelectedScope("Tòa nhà");
            setApartmentOptions([]);
            setFileList([]); // Clear file list
            setQuery((q) => ({ ...q, page: 1 }));
            fetchList();
        } catch (e) {
            close();
            if (e?.errorFields) {
                message.error("Vui lòng kiểm tra lại thông tin nhập");
            } else {
                const apiMsg =
                    e?.response?.data?.message ||
                    e?.response?.data?.title || // fallback for ProblemDetails
                    e?.message ||
                    "Unknown error";
                message.error(`Tạo yêu cầu thất bại: ${apiMsg}`);
            }
        }
    };

    return (
        <div>
            <div className="px-8 py-6">
                <h1 className="text-3xl font-bold mb-4">Quản lý yêu cầu</h1>

                <div className="bg-gray-100 border border-gray-200 rounded-md px-4 py-3 mb-4 flex items-center gap-3">
                    <Input
                        placeholder="Tìm kiếm theo tiêu đề/mô tả"
                        value={query.keyword}
                        onChange={(e) => setQuery((q) => ({ ...q, keyword: e.target.value, page: 1 }))}
                        style={{ width: 300 }}
                        allowClear
                    />
                    <Select
                        placeholder="Trạng thái"
                        value={query.status}
                        onChange={(v) => setQuery((q) => ({ ...q, status: v, page: 1 }))}
                        style={{ width: 200 }}
                        allowClear
                        options={["Mới tạo", "Đã tiếp nhận", "Đang xử lý", "Hoàn thành", "Đã đóng"].map(v => ({ label: v, value: v }))}
                    />
                    <DatePicker
                        allowClear
                        placeholder="Ngày tạo yêu cầu"
                        onChange={(d) => {
                            if (d) {
                                const startOfDay = d.startOf('day').toDate().toISOString();
                                const endOfDay = d.endOf('day').toDate().toISOString();
                                setQuery((q) => ({
                                    ...q,
                                    fromDate: startOfDay,
                                    toDate: endOfDay,
                                    page: 1,
                                }));
                            } else {
                                setQuery((q) => ({
                                    ...q,
                                    fromDate: undefined,
                                    toDate: undefined,
                                    page: 1,
                                }));
                            }
                        }}
                    />
                    <Button icon={<ReloadOutlined />} onClick={handleSearch}>Lọc</Button>
                    <div className="flex-1" />
                    <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateOpen(true)}>Tạo ticket mới</Button>
                </div>

                <div className="bg-gray-100 border border-gray-200 rounded-md p-0">
                    <div className="bg-white p-0 rounded-md">
                        <Table
                            rowKey={(r) => r.ticketId}
                            columns={columns}
                            dataSource={data.items}
                            loading={loading}
                            pagination={{
                                current: query.page,
                                pageSize: query.pageSize,
                                total: data.total,
                                onChange: (p, ps) => setQuery((q) => ({ ...q, page: p, pageSize: ps })),
                                showSizeChanger: true,
                            }}
                        />
                    </div>
                </div>

                <Modal
                    open={createOpen}
                    title="Tạo yêu cầu"
                    onOk={submitCreate}
                    onCancel={() => setCreateOpen(false)}
                    okText="Tạo"
                    cancelText="Hủy"
                    destroyOnClose
                >
                    <Form form={createForm} layout="vertical" initialValues={{ scope: "Tòa nhà" }}>
                        <Form.Item name="subject" label="Tiêu đề" rules={[{ required: true, message: "Tiêu đề là bắt buộc" }]}>
                            <Input placeholder="Ví dụ: Hỏng điều hòa sảnh A" />
                        </Form.Item>
                        <Form.Item name="category" label="Loại Yêu cầu" rules={[{ required: true, message: "Chọn loại yêu cầu" }]}>
                            <Select
                                placeholder="Chọn loại Yêu cầu"
                                showSearch
                                allowClear
                                dropdownStyle={{ maxHeight: 240, overflowY: "auto" }}
                                listHeight={240}
                                virtual={false}
                                getPopupContainer={(triggerNode) => triggerNode.parentNode}
                                options={categoryOptions}
                            />
                        </Form.Item>
                        <Form.Item name="priority" label="Độ ưu tiên" rules={[{ required: true, message: "Chọn độ ưu tiên" }]}>
                            <Select
                                placeholder="Chọn độ ưu tiên"
                                dropdownStyle={{ maxHeight: 240, overflowY: "auto" }}
                                listHeight={240}
                                virtual={false}
                                getPopupContainer={(triggerNode) => triggerNode.parentNode}
                                options={["Thấp", "Bình thường", "Khẩn cấp"].map(v => ({ label: v, value: v }))}
                            />
                        </Form.Item>
                        <Form.Item name="scope" label="Phạm vi xử lý">
                            <Select
                                value={selectedScope}
                                onChange={(value) => setSelectedScope(value)}
                                dropdownStyle={{ maxHeight: 240, overflowY: "auto" }}
                                listHeight={240}
                                virtual={false}
                                getPopupContainer={(triggerNode) => triggerNode.parentNode}
                                options={scopeOptions}
                            />
                        </Form.Item>
                        {selectedScope === "Theo căn hộ" && (
                            <Form.Item name="apartmentId" label="Chọn căn hộ" rules={[{ required: true, message: "Chọn căn hộ khi chọn 'Theo căn hộ'" }]}>
                                <Select
                                    showSearch
                                    placeholder="Tìm kiếm theo số căn hộ (ví dụ: 102)"
                                    optionFilterProp="children"
                                    onSearch={searchApartments}
                                    loading={apartmentLoading}
                                    notFoundContent={apartmentLoading ? "Đang tìm kiếm..." : "Không tìm thấy căn hộ"}
                                    filterOption={false}
                                    dropdownStyle={{ maxHeight: 240, overflowY: "auto" }}
                                    listHeight={240}
                                    virtual={false}
                                    getPopupContainer={(triggerNode) => triggerNode.parentNode}
                                    options={apartmentOptions}
                                />
                            </Form.Item>
                        )}
                        <Form.Item name="description" label="Mô tả">
                            <Input.TextArea rows={4} placeholder="Mô tả chi tiết" />
                        </Form.Item>
                        <Form.Item name="attachments" label="Đính kèm ảnh/tài liệu">
                            <Upload
                                listType="picture-card"
                                fileList={fileList}
                                onChange={({ fileList: newFileList }) => {
                                    // Limit to MAX_FILES_COUNT
                                    if (newFileList.length > MAX_FILES_COUNT) {
                                        message.warning(`Chỉ được upload tối đa ${MAX_FILES_COUNT} file.`);
                                        return;
                                    }
                                    setFileList(newFileList);
                                }}
                                beforeUpload={(file) => {
                                    const isValid = validateTicketFile(file);
                                    return isValid === Upload.LIST_IGNORE ? Upload.LIST_IGNORE : false; // Prevent auto upload
                                }}
                                accept={ALLOWED_TICKET_TYPES}
                                multiple
                            >
                                {fileList.length >= MAX_FILES_COUNT ? null : (
                                    <div>
                                        <PlusOutlined />
                                        <div style={{ marginTop: 8 }}>Tải lên</div>
                                    </div>
                                )}
                            </Upload>
                            <div style={{ marginTop: 8, fontSize: '12px', color: '#999' }}>
                                Hỗ trợ: JPG, PNG, GIF, BMP, WEBP, PDF, DOC, DOCX (tối đa {formatFileSize(MAX_FILE_SIZE_TICKET)} mỗi file, tối đa {MAX_FILES_COUNT} files)
                            </div>
                        </Form.Item>
                    </Form>
                </Modal>
            </div>
        </div>
    );
}


