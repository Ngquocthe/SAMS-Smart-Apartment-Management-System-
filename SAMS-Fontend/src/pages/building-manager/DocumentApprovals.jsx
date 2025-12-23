import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Button, Dropdown, Space, Table, Tag, message, Form, Upload } from "antd";
import { CheckOutlined, CloseOutlined, DownloadOutlined, EyeOutlined, FileTextOutlined, MoreOutlined } from "@ant-design/icons";
import documentsApi from "../../features/documents/documentsApi";
import { getCategoryLabel, getCategoryOptions, stringToCategoryEnum } from "../../features/documents/documentCategories";
import { keycloak } from "../../keycloak/initKeycloak";
import DocumentFilters from "../../components/documents/DocumentFilters";
import DocumentPreviewModal from "../../components/documents/DocumentPreviewModal";
import DocumentVersionsModal from "../../components/documents/DocumentVersionsModal";
import DocumentLogModal from "../../components/documents/DocumentLogModal";
import RejectReasonModal from "./document-approvals/components/RejectReasonModal";
import CreateDocumentModal from "../../components/documents/CreateDocumentModal";

const statusColors = {
    ACTIVE: "green",
    INACTIVE: "volcano",
    PENDING_APPROVAL: "gold",
    PENDING_DELETE: "gold",
    REJECTED: "red",
    DELETED: "default",
};

const formatStatus = (value) => {
    if (!value) return "";
    const upper = String(value).toUpperCase();
    const map = {
        ACTIVE: "Hoạt động",
        INACTIVE: "Ngừng hiển thị",
        PENDING_APPROVAL: "Chờ duyệt",
        PENDING_DELETE: "Chờ duyệt xóa",
        REJECTED: "Bị từ chối",
        DELETED: "Đã xóa",
    };
    return map[upper] || value;
};

const STATUS_ALL = "ALL";

const statusSegments = [
    { label: <span style={{ padding: "0 8px" }}>Tất cả</span>, value: STATUS_ALL },
    { label: <span style={{ padding: "0 8px" }}>Chờ duyệt</span>, value: "PENDING_APPROVAL" },
    { label: <span style={{ padding: "0 8px" }}>Hoạt động</span>, value: "ACTIVE" },
    { label: <span style={{ padding: "0 8px" }}>Ngừng hiển thị</span>, value: "INACTIVE" },
    // Không cho lọc trực tiếp theo trạng thái bị từ chối
];

const defaultQuery = {
    keyword: "",
    page: 1,
    pageSize: 10,
    status: "PENDING_APPROVAL",
    category: undefined,
    visibilityScope: undefined,
};

const MAX_FILE_SIZE_BYTES = 100 * 1024 * 1024; // 100MB
const formatFileSizeMb = (bytes) => `${(bytes / (1024 * 1024)).toFixed(1)}MB`;
const isFileTooLarge = (file) => !!file && file.size > MAX_FILE_SIZE_BYTES;
const getOversizeMessage = (size) => `Dung lượng tối đa 100MB (file hiện ${formatFileSizeMb(size)})`;

export default function DocumentApprovals() {
    const [query, setQuery] = useState(() => ({ ...defaultQuery }));
    const [loading, setLoading] = useState(false);
    const [data, setData] = useState({ total: 0, items: [] });
    const [createOpen, setCreateOpen] = useState(false);
    const [createForm] = Form.useForm();
    const [previewOpen, setPreviewOpen] = useState(false);
    const [previewRecord, setPreviewRecord] = useState(null);
    const [rejectModalOpen, setRejectModalOpen] = useState(false);
    const [rejectReason, setRejectReason] = useState("");
    const [rejectTarget, setRejectTarget] = useState(null);
    const [versionsOpen, setVersionsOpen] = useState(false);
    const [versionsData, setVersionsData] = useState([]);
    const [versionsLoading, setVersionsLoading] = useState(false);
    const [versionsCache, setVersionsCache] = useState({});
    const [logOpen, setLogOpen] = useState(false);
    const [logLoading, setLogLoading] = useState(false);
    const [logRecords, setLogRecords] = useState([]);
    const [logDocument, setLogDocument] = useState(null);

    const reviewer = keycloak?.tokenParsed?.preferred_username || keycloak?.tokenParsed?.name || "building_management";
    const reviewerId = keycloak?.tokenParsed?.sub || null;

    const statusFilterOptions = useMemo(() => statusSegments, []);
    const categoryOptions = useMemo(() => getCategoryOptions(), []);

    const handleBeforeUpload = useCallback((file) => {
        if (isFileTooLarge(file)) {
            message.error(getOversizeMessage(file.size));
            return Upload.LIST_IGNORE;
        }
        return false;
    }, []);

    const fetchList = useCallback(async (overrideQuery) => {
        const effectiveQuery = overrideQuery ?? query;
        setLoading(true);
        try {
            const result = await documentsApi.search({
                keyword: effectiveQuery.keyword || undefined,
                pageIndex: effectiveQuery.page,
                pageSize: effectiveQuery.pageSize,
                status: effectiveQuery.status,
                category: effectiveQuery.category,
                visibilityScope: effectiveQuery.visibilityScope,
            });
            setData({ total: result.total || 0, items: result.items || [] });
        } catch (error) {
            console.error("Fetch documents error:", error);
            let errorMessage = "Không tải được danh sách tài liệu";
            if (error.response?.status === 0 || error.code === "ERR_NETWORK") {
                errorMessage = "Không thể kết nối đến server. Vui lòng kiểm tra kết nối mạng và thử lại.";
            } else if (error.response?.data?.message) {
                errorMessage += `: ${error.response.data.message}`;
            } else if (error.response?.status >= 500) {
                errorMessage += ": Lỗi server, vui lòng thử lại sau";
            }
            message.error({ content: errorMessage, duration: 5 });
        } finally {
            setLoading(false);
        }
    }, [query]);

    useEffect(() => {
        fetchList();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [query.page, query.pageSize]);

    const handleSearch = () => {
        const nextQuery = { ...query, page: 1 };
        setQuery(nextQuery);
        fetchList(nextQuery);
    };

    const handleKeywordChange = (value) => setQuery((prev) => ({ ...prev, keyword: value }));
    const handleStatusChange = (value) => {
        const nextQuery = {
            ...query,
            status: value || undefined,
            page: 1,
        };
        setQuery(nextQuery);
        fetchList(nextQuery);
    };

    const handleCategoryFilterChange = (value) => {
        const nextQuery = { ...query, category: value || undefined, page: 1 };
        setQuery(nextQuery);
        fetchList(nextQuery);
    };

    const handleScopeFilterChange = (value) => {
        const SCOPE_ALL_VALUE = "__ALL__";
        const normalized = value === SCOPE_ALL_VALUE ? undefined : value;
        const nextQuery = { ...query, visibilityScope: normalized, page: 1 };
        setQuery(nextQuery);
        fetchList(nextQuery);
    };

    const handleResetFilters = () => {
        const reset = { ...defaultQuery };
        setQuery(reset);
        fetchList(reset);
    };

    const openPreview = (record) => {
        setPreviewRecord(record);
        setPreviewOpen(true);
    };

    const handleDownload = useCallback((fileId) => {
        const url = documentsApi.buildDownloadUrl(fileId);
        window.open(url, "_blank");
    }, []);

    const openVersions = useCallback(async (record) => {
        setPreviewRecord(record);
        setVersionsOpen(true);
        const documentId = record.documentId || record.id;
        if (!documentId) {
            message.error("Không tìm thấy ID tài liệu");
            setVersionsData([]);
            return;
        }
        if (versionsCache[documentId]) {
            setVersionsData(versionsCache[documentId]);
            return;
        }

        setVersionsLoading(true);
        const loadingMessage = message.loading('Đang tải danh sách phiên bản...', 0);
        try {
            const versions = await documentsApi.getVersions(documentId);
            const versionsList = Array.isArray(versions) ? versions : [];
            setVersionsCache((prev) => ({ ...prev, [documentId]: versionsList }));
            setVersionsData(versionsList);
            loadingMessage();
            if (versionsList.length === 0) {
                message.info("Không có phiên bản nào");
            }
        } catch (error) {
            loadingMessage();
            console.error("Error loading versions:", error);
            message.error("Không thể tải danh sách phiên bản");
            setVersionsData([]);
        } finally {
            setVersionsLoading(false);
        }
    }, [versionsCache]);

    const handleApprove = async (record) => {
        const id = record.documentId || record.id;
        if (!id) {
            message.error("Không tìm thấy ID tài liệu");
            return;
        }
        const close = message.loading("Đang phê duyệt...", 0);
        try {
            await documentsApi.changeStatus(id, {
                status: "ACTIVE",
                actorId: reviewerId,
                detail: `Phê duyệt bởi ${reviewer}`,
            });
            close();
            message.success("Đã phê duyệt tài liệu");
            await fetchList();
        } catch (error) {
            close();
            console.error("Approve error:", error);
            message.error("Phê duyệt thất bại");
        }
    };

    const handleApproveDelete = async (record) => {
        const id = record.documentId || record.id;
        if (!id) {
            message.error("Không tìm thấy ID tài liệu");
            return;
        }
        const close = message.loading("Đang phê duyệt ngưng hiển thị...", 0);
        try {
            await documentsApi.changeStatus(id, {
                status: "INACTIVE",
                actorId: reviewerId,
                detail: `Phê duyệt ngưng hiển thị bởi ${reviewer}`,
            });
            close();
            message.success("Đã phê duyệt ngưng hiển thị tài liệu");
            await fetchList();
        } catch (error) {
            close();
            console.error("Approve delete error:", error);
            message.error("Phê duyệt xóa thất bại");
        }
    };

    const handleDeactivate = async (record) => {
        const id = record.documentId || record.id;
        if (!id) {
            message.error("Không tìm thấy ID tài liệu");
            return;
        }
        const close = message.loading("Đang ngưng hiển thị...", 0);
        try {
            await documentsApi.changeStatus(id, {
                status: "INACTIVE",
                actorId: reviewerId,
                detail: `Ngừng hiển thị bởi ${reviewer}`,
            });
            close();
            message.success("Đã ngưng hiển thị tài liệu");
            await fetchList();
        } catch (error) {
            close();
            console.error("Deactivate error:", error);
            message.error("Ngưng hiển thị thất bại");
        }
    };
    const handleOpenReject = (record) => {
        setRejectTarget(record);
        setRejectReason("");
        setRejectModalOpen(true);
    };

    const submitReject = async () => {
        if (!rejectTarget) return;
        const id = rejectTarget.documentId || rejectTarget.id;
        if (!id) {
            message.error("Không tìm thấy ID tài liệu");
            return;
        }
        const close = message.loading("Đang từ chối...", 0);
        try {
            const note = rejectReason?.trim()
                ? `Từ chối bởi ${reviewer}: ${rejectReason.trim()}`
                : `Từ chối bởi ${reviewer}`;
            await documentsApi.changeStatus(id, {
                status: "REJECTED",
                actorId: reviewerId,
                detail: note,
            });
            close();
            message.success("Đã từ chối tài liệu");
            setRejectModalOpen(false);
            setRejectReason("");
            setRejectTarget(null);
            await fetchList();
        } catch (error) {
            close();
            console.error("Reject error:", error);
            message.error("Từ chối thất bại");
        }
    };

    const handlePreviewVersion = (version) => {
        if (!previewRecord) return;
        const versionRecord = {
            ...previewRecord,
            fileId: version.fileId,
            originalFileName: version.originalFileName || version.fileName || previewRecord?.originalFileName || "Unknown file",
            mimeType: version.mimeType || version.contentType || previewRecord?.mimeType || "application/octet-stream",
            fileSize: version.fileSize || version.size || previewRecord?.fileSize || 0,
            title: previewRecord?.title || "Document",
        };
        setPreviewRecord(versionRecord);
        setPreviewOpen(true);
    };

    const openLogs = useCallback(async (record) => {
        const id = record.documentId || record.id;
        if (!id) {
            message.error("Không tìm thấy ID tài liệu");
            return;
        }
        setLogDocument(record);
        setLogOpen(true);
        setLogLoading(true);
        try {
            const logs = await documentsApi.getLogs(id);
            setLogRecords(Array.isArray(logs) ? logs : []);
        } catch (error) {
            console.error("Load logs error:", error);
            message.error("Không thể tải lịch sử");
            setLogRecords([]);
        } finally {
            setLogLoading(false);
        }
    }, []);

    const handleCreateOk = async (values) => {
        const loadingMessage = message.loading("Đang tạo tài liệu...", 0);

        try {
            const selectedFile = values.file.file;

            const username = keycloak?.tokenParsed?.preferred_username || window.keycloak?.tokenParsed?.preferred_username;
            if (!username) {
                loadingMessage();
                message.error("Không thể lấy thông tin đăng nhập. Vui lòng đăng nhập lại.");
                return;
            }

            const formData = new FormData();
            formData.append("Title", values.title);
            if (values.category) formData.append("Category", values.category.toString());
            formData.append("VisibilityScope", values.visibilityScope);
            formData.append("CreatedBy", username);
            formData.append("file", selectedFile);

            await documentsApi.create(formData);

            loadingMessage();
            message.success({
                content: "Đã tạo tài liệu! Tài liệu sẽ chờ duyệt.",
                duration: 4,
            });

            setCreateOpen(false);
            createForm.resetFields();
            const resetQuery = { ...defaultQuery };
            setQuery(resetQuery);
            await fetchList(resetQuery);
        } catch (e) {
            loadingMessage();
            if (e?.errorFields) return;

            let errorMessage = "Không thể tạo tài liệu";
            if (e.response?.data?.message) {
                errorMessage += `: ${e.response.data.message}`;
            } else if (e.response?.status === 400) {
                errorMessage += ": Dữ liệu không hợp lệ";
            } else if (e.response?.status === 413) {
                errorMessage += ": File quá lớn";
            } else if (e.response?.status >= 500) {
                errorMessage += ": Lỗi server, vui lòng thử lại sau";
            } else if (e.message) {
                errorMessage += `: ${e.message}`;
            }

            message.error({
                content: errorMessage,
                duration: 5,
            });

            console.error("Create document error:", e);
        }
    };

    const columns = [
        {
            title: "Tên tài liệu",
            dataIndex: "title",
            key: "title",
            render: (value, record) => (
                <Space direction="vertical" size={0}>
                    <strong>{value}</strong>
                    {record.code && (
                        <span className="text-muted" style={{ fontSize: 12 }}>{record.code}</span>
                    )}
                </Space>
            ),
        },
        {
            title: "Ngày cập nhật",
            dataIndex: "changedAt",
            key: "changedAt",
            width: 150,
            render: (v) => (v ? new Date(v).toLocaleDateString("vi-VN") : ""),
        },
        {
            title: "Phân loại",
            dataIndex: "category",
            key: "category",
            width: 180,
            render: (category) => {
                const categoryValue = typeof category === 'number' ? category : stringToCategoryEnum(category);
                return getCategoryLabel(categoryValue);
            },
        },
        {
            title: "Người tạo",
            dataIndex: "createdBy",
            key: "createdBy",
            width: 180,
        },
        {
            title: "Trạng thái",
            dataIndex: "status",
            key: "status",
            width: 140,
            render: (value) => <Tag color={statusColors[String(value).toUpperCase()] || "default"}>{formatStatus(value)}</Tag>,
        },
        {
            title: "Phiên bản",
            dataIndex: "latestVersionNo",
            key: "latestVersionNo",
            width: 110,
            align: "center",
        },
        {
            title: "Bản đang hiển thị",
            dataIndex: "currentVersion",
            key: "currentVersion",
            width: 150,
            align: "center",
            render: (value) => value ?? "—",
        },
        {
            title: "",
            key: "actions",
            width: 100,
            align: "center",
            render: (_, record) => {
                const statusValue = String(record.status || "").toUpperCase();
                const isPendingContent = statusValue === "PENDING_APPROVAL";
                const isPendingDelete = statusValue === "PENDING_DELETE";
                const isActive = statusValue === "ACTIVE";

                const items = [
                    ...(record.fileId ? [{
                        key: 'view',
                        label: 'Xem',
                        icon: <EyeOutlined />,
                        onClick: () => openPreview(record)
                    }] : []),
                    // Ở trạng thái chờ duyệt / chờ duyệt xóa không cho xem phiên bản
                    ...(!isPendingContent && !isPendingDelete ? [{
                        key: 'versions',
                        label: 'Xem phiên bản',
                        icon: <FileTextOutlined />,
                        onClick: () => openVersions(record)
                    }] : []),
                    {
                        key: 'logs',
                        label: 'Xem log',
                        icon: <FileTextOutlined />,
                        onClick: () => openLogs(record)
                    },
                    ...(record.fileId ? [{
                        key: 'download',
                        label: 'Tải xuống',
                        icon: <DownloadOutlined />,
                        onClick: () => handleDownload(record.fileId)
                    }] : []),
                    ...(isActive ? [{
                        key: 'deactivate',
                        label: 'Ngưng hiển thị',
                        icon: <CloseOutlined />,
                        onClick: () => handleDeactivate(record)
                    }] : []),
                    ...(isPendingContent ? [{
                        key: 'approve',
                        label: 'Phê duyệt (kích hoạt)',
                        icon: <CheckOutlined />,
                        onClick: () => handleApprove(record)
                    }, {
                        key: 'reject',
                        label: 'Từ chối',
                        icon: <CloseOutlined />,
                        onClick: () => handleOpenReject(record)
                    }] : []),
                    ...(isPendingDelete ? [{
                        key: 'approve_delete',
                        label: 'Phê duyệt xóa',
                        icon: <CloseOutlined />,
                        onClick: () => handleApproveDelete(record)
                    }, {
                        key: 'reject_delete',
                        label: 'Từ chối xóa',
                        icon: <CloseOutlined />,
                        onClick: () => handleOpenReject(record)
                    }] : []),
                ];

                return (
                    <Dropdown
                        menu={{ items }}
                        trigger={['click']}
                        placement="bottomRight"
                    >
                        <Button
                            type="text"
                            icon={<MoreOutlined />}
                            size="small"
                            style={{ border: 'none', boxShadow: 'none' }}
                        />
                    </Dropdown>
                );
            },
        },
    ];

    return (
        <div className="px-8 py-6">
            <h1 className="text-3xl font-bold mb-4">Quản lý tài liệu</h1>
            <DocumentFilters
                query={query}
                statusOptions={statusFilterOptions}
                statusValue={query.status ?? STATUS_ALL}
                statusAllValue={STATUS_ALL}
                categoryOptions={categoryOptions}
                onKeywordChange={handleKeywordChange}
                onStatusChange={handleStatusChange}
                onCategoryChange={handleCategoryFilterChange}
                onScopeChange={handleScopeFilterChange}
                onSearch={handleSearch}
                onReset={handleResetFilters}
                onCreateClick={() => setCreateOpen(true)}
            />

            <div className="bg-gray-100 border border-gray-200 rounded-md p-0">
                <div className="bg-white p-0 rounded-md">
                    <Table
                        rowKey={(r) => r.documentId || r.id}
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

            <DocumentPreviewModal open={previewOpen} record={previewRecord} onClose={() => setPreviewOpen(false)} onDownload={handleDownload} />

            <DocumentVersionsModal
                open={versionsOpen}
                record={previewRecord}
                loading={versionsLoading}
                versions={versionsData}
                onClose={() => setVersionsOpen(false)}
                onPreviewVersion={handlePreviewVersion}
                onDownloadVersion={handleDownload}
            />

            <RejectReasonModal
                open={rejectModalOpen}
                reason={rejectReason}
                onChange={setRejectReason}
                onSubmit={submitReject}
                onCancel={() => setRejectModalOpen(false)}
            />

            <DocumentLogModal
                open={logOpen}
                loading={logLoading}
                logs={logRecords}
                documentTitle={logDocument?.title}
                onClose={() => setLogOpen(false)}
            />

            <CreateDocumentModal
                open={createOpen}
                form={createForm}
                onOk={handleCreateOk}
                onCancel={() => setCreateOpen(false)}
                onBeforeUpload={handleBeforeUpload}
            />
        </div>
    );
}

