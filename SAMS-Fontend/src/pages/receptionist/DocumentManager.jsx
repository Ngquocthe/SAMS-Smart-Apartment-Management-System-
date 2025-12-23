import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Form, Upload, message, Modal } from "antd";
import documentsApi from "../../features/documents/documentsApi";
import { getCategoryOptions } from "../../features/documents/documentCategories";
import { keycloak } from "../../keycloak/initKeycloak";
import DocumentFilters from "../../components/documents/DocumentFilters";
import DocumentTable from "./document-manager/components/DocumentTable";
import CreateDocumentModal from "../../components/documents/CreateDocumentModal";
import EditDocumentModal from "./document-manager/components/EditDocumentModal";
import UploadVersionModal from "./document-manager/components/UploadVersionModal";
import DocumentPreviewModal from "../../components/documents/DocumentPreviewModal";
import DocumentVersionsModal from "../../components/documents/DocumentVersionsModal";
import DeleteConfirmModal from "./document-manager/components/DeleteConfirmModal";
import RestoreRequestModal from "./document-manager/components/RestoreRequestModal";

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
/**
 * Format kích thước file từ bytes sang MB
 * @param {number} bytes - Kích thước file tính bằng bytes
 * @returns {string} Chuỗi định dạng (ví dụ: "50.5MB")
 */
const formatFileSizeMb = (bytes) => `${(bytes / (1024 * 1024)).toFixed(1)}MB`;

/**
 * Kiểm tra file có vượt quá giới hạn 100MB không
 * @param {File} file - File object cần kiểm tra
 * @returns {boolean} true nếu file quá lớn
 */
const isFileTooLarge = (file) => !!file && file.size > MAX_FILE_SIZE_BYTES;

/**
 * Tạo thông báo lỗi khi file quá lớn
 * @param {number} size - Kích thước file tính bằng bytes
 * @returns {string} Thông báo lỗi
 */
const getOversizeMessage = (size) => `Dung lượng tối đa 100MB (file hiện ${formatFileSizeMb(size)})`;

export default function DocumentManager() {
    const [query, setQuery] = useState(() => ({ ...defaultQuery }));
    const [loading, setLoading] = useState(false);
    const [data, setData] = useState({ total: 0, items: [] });
    const [createOpen, setCreateOpen] = useState(false);
    const [createForm] = Form.useForm();
    const [previewOpen, setPreviewOpen] = useState(false);
    const [previewRecord, setPreviewRecord] = useState(null);
    const [uploadOpen, setUploadOpen] = useState(false);
    const [uploadForm] = Form.useForm();
    const [editOpen, setEditOpen] = useState(false);
    const [editForm] = Form.useForm();
    const [versionsOpen, setVersionsOpen] = useState(false);
    const [versionsData, setVersionsData] = useState([]);
    const [versionsLoading, setVersionsLoading] = useState(false);
    const [versionsCache, setVersionsCache] = useState({});
    const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
    const [deleteReason, setDeleteReason] = useState("");
    const [deletePermanent, setDeletePermanent] = useState(false);
    const [restoreConfirmOpen, setRestoreConfirmOpen] = useState(false);
    const [restoreReason, setRestoreReason] = useState("");

    /**
     * Validate file trước khi upload
     * Kiểm tra kích thước file (tối đa 100MB)
     * @param {File} file - File cần validate
     * @returns {boolean|string} false nếu hợp lệ, Upload.LIST_IGNORE nếu không hợp lệ
     */
    const handleBeforeUpload = useCallback((file) => {
        if (isFileTooLarge(file)) {
            message.error(getOversizeMessage(file.size));
            return Upload.LIST_IGNORE;
        }
        return false;
    }, []);

    const categoryOptions = useMemo(() => getCategoryOptions(), []);

    /**
     * Lấy danh sách tài liệu từ API với các filter hiện tại
     * Hỗ trợ override query để có thể tìm kiếm với query khác (dùng khi reset filter)
     * @param {object} overrideQuery - Query tùy chọn để override query hiện tại
     */
    const fetchList = useCallback(
        async (overrideQuery) => {
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
                console.error("Fetch list error:", error);
                let errorMessage = "Không tải được danh sách tài liệu";
                if (error.response?.data?.message) {
                    errorMessage += `: ${error.response.data.message}`;
                } else if (error.response?.status >= 500) {
                    errorMessage += ": Lỗi server, vui lòng thử lại sau";
                }
                message.error({
                    content: errorMessage,
                    duration: 4,
                });
            } finally {
                setLoading(false);
            }
        },
        [query]
    );

    useEffect(() => {
        fetchList();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [query.page, query.pageSize]);

    /**
     * Xử lý khi người dùng click nút tìm kiếm
     * Reset về trang 1 và fetch lại danh sách với query mới
     */
    const handleSearch = () => {
        const nextQuery = { ...query, page: 1 };
        setQuery(nextQuery);
        fetchList(nextQuery);
    };

    /**
     * Xử lý khi người dùng thay đổi filter theo trạng thái
     * Nếu chọn "Tất cả" (STATUS_ALL) thì set status = undefined
     * @param {string} value - Giá trị status được chọn
     */
    const handleStatusSegmentChange = (value) => {
        const normalized = value === STATUS_ALL ? undefined : value;
        const nextQuery = { ...query, status: normalized, page: 1 };
        setQuery(nextQuery);
        fetchList(nextQuery);
    };

    /**
     * Xử lý khi người dùng thay đổi filter theo danh mục
     * @param {string} value - Giá trị category được chọn
     */
    const handleCategoryFilterChange = (value) => {
        const nextQuery = { ...query, category: value || undefined, page: 1 };
        setQuery(nextQuery);
        fetchList(nextQuery);
    };

    /**
     * Xử lý khi người dùng thay đổi filter theo phạm vi hiển thị
     * Nếu chọn "Tất cả" (SCOPE_ALL_VALUE) thì set visibilityScope = undefined
     * @param {string} value - Giá trị visibilityScope được chọn
     */
    const handleScopeFilterChange = (value) => {
        const SCOPE_ALL_VALUE = "__ALL__";
        const normalized = value === SCOPE_ALL_VALUE ? undefined : value;
        const nextQuery = { ...query, visibilityScope: normalized, page: 1 };
        setQuery(nextQuery);
        fetchList(nextQuery);
    };

    /**
     * Reset tất cả filter về giá trị mặc định
     * Trạng thái mặc định là "PENDING_APPROVAL" (Chờ duyệt)
     */
    const handleResetFilters = () => {
        const reset = { ...defaultQuery };
        setQuery(reset);
        fetchList(reset);
    };

    /**
     * Xử lý tải xuống file tài liệu
     * Mở link download trong tab mới
     * @param {string} fileId - ID của file cần tải xuống
     */
    const handleDownload = useCallback((fileId) => {
        const url = documentsApi.buildDownloadUrl(fileId);
        window.open(url, "_blank");
    }, []);

    /**
     * Mở modal preview tài liệu
     * @param {object} record - Document object cần preview
     */
    const openPreview = useCallback((record) => {
        setPreviewRecord(record);
        setPreviewOpen(true);
    }, []);

    /**
     * Mở modal upload phiên bản mới cho tài liệu
     * Reset form và set record hiện tại
     * @param {object} record - Document object cần upload version
     */
    const openUploadVersion = useCallback((record) => {
        setPreviewRecord(record);
        uploadForm.resetFields();
        setUploadOpen(true);
    }, [uploadForm]);

    /**
     * Mở modal chỉnh sửa thông tin tài liệu
     * Điền sẵn các giá trị hiện tại vào form
     * @param {object} record - Document object cần chỉnh sửa
     */
    const openEdit = useCallback(
        (record) => {
            setPreviewRecord(record);
            editForm.setFieldsValue({
                title: record.title,
                category: record.category ?? undefined,
                visibilityScope: record.visibilityScope ?? undefined,
            });
            setEditOpen(true);
        },
        [editForm]
    );

    /**
     * Mở modal xem danh sách các phiên bản của tài liệu
     * Sử dụng cache để tránh fetch lại nếu đã tải trước đó
     * @param {object} record - Document object cần xem versions
     */
    const openVersions = useCallback(
        async (record) => {
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
            const loadingMessage = message.loading("Đang tải danh sách phiên bản...", 0);

            try {
                const versions = await documentsApi.getVersions(documentId);
                const versionsList = Array.isArray(versions) ? versions : [];

                setVersionsCache((prev) => ({
                    ...prev,
                    [documentId]: versionsList,
                }));

                setVersionsData(versionsList);

                loadingMessage();
                if (versionsList.length > 0) {
                    message.success(`Đã tải ${versionsList.length} phiên bản`);
                } else {
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
        },
        [versionsCache]
    );

    /**
     * Preview một phiên bản cụ thể của tài liệu
     * Tạo record mới với thông tin file từ version được chọn
     * @param {object} version - Version object chứa thông tin file của phiên bản
     */
    const handlePreviewVersion = useCallback(
        (version) => {
            if (!previewRecord) return;
            const originalTitle = (previewRecord?.title || "Document").split(" - Version ")[0];
            const versionRecord = {
                ...previewRecord,
                fileId: version.fileId,
                originalFileName: version.originalFileName || version.fileName || previewRecord?.originalFileName || "Unknown file",
                mimeType: version.mimeType || version.contentType || previewRecord?.mimeType || "application/octet-stream",
                fileSize: version.fileSize || version.size || previewRecord?.fileSize || 0,
                title: originalTitle, // Giữ title gốc, không append version
            };
            setPreviewRecord(versionRecord);
            setPreviewOpen(true);
        },
        [previewRecord]
    );

    /**
     * Upload phiên bản mới cho tài liệu
     * Hỗ trợ retry tự động (tối đa 3 lần) khi gặp lỗi 500 (Cloudinary timestamp issue)
     * Sau khi upload thành công, tài liệu sẽ chuyển sang trạng thái "Chờ duyệt"
     * @param {object} values - Form values chứa file và note (nếu có)
     */
    const submitUploadVersion = async (values) => {
        const loadingMessage = message.loading("Đang tải phiên bản mới...", 0);

        try {
            const documentId = previewRecord.documentId || previewRecord.id;
            if (!documentId) {
                loadingMessage();
                message.error("Không tìm thấy ID tài liệu");
                return;
            }

            const formData = new FormData();
            formData.append("file", values.file.file);
            if (values.note) formData.append("note", values.note);

            let retryCount = 0;
            const maxRetries = 3;

            while (retryCount < maxRetries) {
                try {
                    await documentsApi.uploadVersion(documentId, formData);
                    loadingMessage();
                    message.success({
                        content: "Đã tải phiên bản mới! Tài liệu sẽ chuyển sang trạng thái chờ duyệt.",
                        duration: 4,
                    });
                    break;
                } catch (error) {
                    retryCount++;
                    if (error.response?.status === 500 && retryCount < maxRetries) {
                        console.log(`Retry ${retryCount}/${maxRetries} for Cloudinary timestamp issue...`);
                        await new Promise((resolve) => setTimeout(resolve, 1000));
                        continue;
                    }
                    throw error;
                }
            }

            setUploadOpen(false);
            uploadForm.resetFields();
            await fetchList();
        } catch (e) {
            loadingMessage();
            if (e?.errorFields) return;

            let errorMessage = "Tải phiên bản thất bại";
            if (e.response?.data?.message) {
                // Kiểm tra xem có phải lỗi do đang chờ phê duyệt không
                if (e.response.data.message.includes("chờ phê duyệt")) {
                    errorMessage = e.response.data.message;
                } else {
                    errorMessage += `: ${e.response.data.message}`;
                }
            } else if (e.response?.status === 404) {
                errorMessage += ": Không tìm thấy tài liệu";
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

            console.error("Upload version error:", e);
        }
    };

    /**
     * Cập nhật metadata của tài liệu (title, category, visibilityScope)
     * Sau khi cập nhật, tài liệu sẽ chuyển sang trạng thái "Chờ duyệt" và cần được Ban quản lý phê duyệt
     * @param {object} values - Form values chứa title, category, visibilityScope
     */
    const submitEdit = async (values) => {
        const loadingMessage = message.loading("Đang cập nhật thông tin...", 0);

        try {
            const id = previewRecord.documentId || previewRecord.id;

            if (!id) {
                loadingMessage();
                message.error("Không tìm thấy ID tài liệu");
                return;
            }

            await documentsApi.updateMetadata(id, {
                title: values.title,
                category: values.category?.toString(),
                visibilityScope: values.visibilityScope,
            });

            loadingMessage();
            message.success({
                content: "Đã cập nhật thông tin! Tài liệu sẽ chờ duyệt từ Ban quản lý.",
                duration: 4,
            });

            setEditOpen(false);
            await fetchList();
        } catch (e) {
            loadingMessage();
            if (e?.errorFields) return;

            let errorMessage = "Cập nhật thông tin thất bại";
            if (e.response?.data?.message) {
                // Kiểm tra xem có phải lỗi do đang chờ phê duyệt không
                if (e.response.data.message.includes("chờ phê duyệt")) {
                    errorMessage = e.response.data.message;
                } else {
                    errorMessage += `: ${e.response.data.message}`;
                }
            } else if (e.response?.status === 404) {
                errorMessage += ": Không tìm thấy tài liệu";
            } else if (e.response?.status === 400) {
                errorMessage += ": Dữ liệu không hợp lệ";
            } else if (e.response?.status >= 500) {
                errorMessage += ": Lỗi server, vui lòng thử lại sau";
            } else if (e.message) {
                errorMessage += `: ${e.message}`;
            }

            message.error({
                content: errorMessage,
                duration: 5,
            });

            console.error("Update metadata error:", e);
        }
    };

    /**
     * Tạo tài liệu mới
     * Lấy username từ Keycloak token để gán vào CreatedBy
     * Sau khi tạo, tài liệu sẽ ở trạng thái "Chờ duyệt" và cần được Ban quản lý phê duyệt
     * @param {object} values - Form values chứa title, category, visibilityScope, file
     */
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
            formData.append("Category", values.category);
            formData.append("VisibilityScope", values.visibilityScope);
            formData.append("CreatedBy", username);
            formData.append("file", selectedFile);

            await documentsApi.create(formData);

            loadingMessage();
            message.success({
                content: "Đã tạo tài liệu! Ban quản lý sẽ duyệt trước khi cư dân nhìn thấy.",
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

    /**
     * Gửi yêu cầu khôi phục tài liệu đã bị xóa
     * Yêu cầu sẽ được gửi tới Ban quản lý để phê duyệt
     */
    const submitRestoreRequest = useCallback(async () => {
        if (!previewRecord) return;
        const documentId = previewRecord.documentId || previewRecord.id;
        if (!documentId) {
            message.error("Không tìm thấy ID tài liệu");
            return;
        }

        const close = message.loading("Đang gửi yêu cầu khôi phục...", 0);
        try {
            await documentsApi.restore(documentId, {
                reason: restoreReason.trim() || undefined,
            });
            close();
            message.success("Đã gửi yêu cầu khôi phục, chờ ban quản lý phê duyệt");
            setRestoreConfirmOpen(false);
            setRestoreReason("");
            await fetchList();
        } catch (error) {
            close();
            console.error("Request restore error:", error);
            message.error("Không thể gửi yêu cầu khôi phục");
        }
    }, [previewRecord, restoreReason, fetchList]);

    /**
     * Kiểm tra xem tài liệu có đang ở trạng thái "Ngừng hiển thị" (INACTIVE) không
     * Dùng để xác định có nên xóa vĩnh viễn hay chỉ soft delete
     * @param {object} record - Document object cần kiểm tra
     * @returns {boolean} true nếu tài liệu đang ở trạng thái INACTIVE
     */
    const isRecordInactive = useCallback((record) => {
        const statusValue = String(record?.status || "").toUpperCase();
        return statusValue === "INACTIVE" || record?.status === "Ngừng hiển thị";
    }, []);

    /**
     * Mở modal xác nhận xóa tài liệu
     * Nếu tài liệu đã ở trạng thái INACTIVE thì sẽ xóa vĩnh viễn, ngược lại chỉ soft delete
     * @param {object} record - Document object cần xóa
     */
    const handleDelete = useCallback(
        async (record) => {
            setPreviewRecord(record);
            setDeleteReason("");
            setDeletePermanent(isRecordInactive(record));
            setDeleteConfirmOpen(true);
        },
        [isRecordInactive]
    );

    /**
     * Xác nhận và thực hiện xóa tài liệu
     * Nếu isPermanent = true thì xóa vĩnh viễn, ngược lại chỉ chuyển sang trạng thái "Ngừng hiển thị"
     */
    const handleDeleteConfirm = async () => {
        if (!previewRecord?.documentId && !previewRecord?.id) {
            setDeleteConfirmOpen(false);
            message.error("Không tìm thấy ID tài liệu");
            return;
        }
        const id = previewRecord.documentId || previewRecord.id;
        const closeLoading = message.loading("Đang xóa...", 0);
        const isPermanent = deletePermanent;
        try {
            await documentsApi.softDelete(id, { reason: deleteReason || undefined });
            closeLoading();
            message.success(isPermanent ? "Đã xóa vĩnh viễn tài liệu" : "Đã chuyển sang trạng thái ngừng hiển thị");
            setDeleteConfirmOpen(false);
            setDeletePermanent(false);
            setDeleteReason("");
            await fetchList();
        } catch (e) {
            closeLoading();
            message.error("Xóa thất bại");
            setDeletePermanent(false);
        }
    };

    /**
     * Mở modal yêu cầu khôi phục tài liệu
     * @param {object} record - Document object cần khôi phục
     */
    const handleRestore = useCallback((record) => {
        setPreviewRecord(record);
        setRestoreReason("");
        setRestoreConfirmOpen(true);
    }, []);

    /**
     * Gửi yêu cầu bật hiển thị lại tài liệu đang ở trạng thái "Ngừng hiển thị"
     * Hiển thị modal xác nhận trước khi gửi yêu cầu
     * Yêu cầu sẽ được chuyển tới Ban quản lý để phê duyệt
     * @param {object} record - Document object cần bật hiển thị lại
     */
    const handleActivate = useCallback(
        (record) => {
            const id = record.documentId || record.id;
            if (!id) {
                message.error("Không tìm thấy ID tài liệu");
                return;
            }

            const requester =
                keycloak?.tokenParsed?.preferred_username ||
                keycloak?.tokenParsed?.name ||
                "receptionist";
            const requesterId = keycloak?.tokenParsed?.sub || null;

            Modal.confirm({
                title: "Xác nhận bật hiển thị lại",
                content:
                    "Bạn có chắc chắn muốn gửi yêu cầu bật hiển thị lại tài liệu này? Yêu cầu sẽ được chuyển tới Ban quản lý để phê duyệt.",
                okText: "Gửi yêu cầu",
                cancelText: "Hủy",
                onOk: async () => {
                    const closeLoading = message.loading("Đang gửi yêu cầu bật hiển thị lại...", 0);
                    try {
                        await documentsApi.changeStatus(id, {
                            status: "PENDING_APPROVAL",
                            actorId: requesterId,
                            detail: `Yêu cầu bật hiển thị lại bởi ${requester}`,
                        });
                        closeLoading();
                        message.success("Đã gửi yêu cầu bật hiển thị lại, chờ Ban quản lý phê duyệt");
                        await fetchList();
                    } catch (error) {
                        closeLoading();
                        console.error("Activate document error:", error);
                        message.error("Không thể gửi yêu cầu bật hiển thị lại");
                    }
                },
            });
        },
        [fetchList]
    );

    return (
        <div>
            <div className="px-8 py-6">
                <h1 className="text-3xl font-bold mb-4">Quản lý tài liệu</h1>

                <DocumentFilters
                    query={query}
                    statusOptions={statusSegments}
                    statusValue={query.status ?? STATUS_ALL}
                    statusAllValue={STATUS_ALL}
                    categoryOptions={categoryOptions}
                    onKeywordChange={(value) => setQuery((q) => ({ ...q, keyword: value }))}
                    onStatusChange={handleStatusSegmentChange}
                    onCategoryChange={handleCategoryFilterChange}
                    onScopeChange={handleScopeFilterChange}
                    onSearch={handleSearch}
                    onReset={handleResetFilters}
                    onCreateClick={() => setCreateOpen(true)}
                />

                <DocumentTable
                    data={data}
                    loading={loading}
                    pagination={{
                        current: query.page,
                        pageSize: query.pageSize,
                        total: data.total,
                        onChange: (p, ps) => setQuery((q) => ({ ...q, page: p, pageSize: ps })),
                        showSizeChanger: true,
                    }}
                    onPreview={openPreview}
                    onUploadVersion={openUploadVersion}
                    onEdit={openEdit}
                    onVersions={openVersions}
                    onDownload={handleDownload}
                    onDelete={handleDelete}
                    onRestore={handleRestore}
                    onActivate={handleActivate}
                />

                <CreateDocumentModal
                    open={createOpen}
                    form={createForm}
                    onOk={handleCreateOk}
                    onCancel={() => setCreateOpen(false)}
                    onBeforeUpload={handleBeforeUpload}
                />

                <EditDocumentModal open={editOpen} form={editForm} onOk={submitEdit} onCancel={() => setEditOpen(false)} />

                <UploadVersionModal
                    open={uploadOpen}
                    form={uploadForm}
                    onOk={submitUploadVersion}
                    onCancel={() => setUploadOpen(false)}
                    onBeforeUpload={handleBeforeUpload}
                />

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

                <DeleteConfirmModal
                    open={deleteConfirmOpen}
                    reason={deleteReason}
                    isPermanent={deletePermanent}
                    onOk={handleDeleteConfirm}
                    onCancel={() => {
                        setDeleteConfirmOpen(false);
                        setDeletePermanent(false);
                        setDeleteReason("");
                    }}
                    onReasonChange={setDeleteReason}
                />

                <RestoreRequestModal
                    open={restoreConfirmOpen}
                    reason={restoreReason}
                    onOk={submitRestoreRequest}
                    onCancel={() => setRestoreConfirmOpen(false)}
                    onReasonChange={setRestoreReason}
                />
            </div>
        </div>
    );
}
