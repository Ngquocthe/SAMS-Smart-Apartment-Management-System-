import React, { useCallback, useMemo } from "react";
import { Table, Space, Tag, Dropdown, Button } from "antd";
import { MoreOutlined, EyeOutlined, UploadOutlined, EditOutlined, FileTextOutlined, DownloadOutlined, RollbackOutlined, DeleteOutlined } from "@ant-design/icons";
import { getCategoryLabel, stringToCategoryEnum } from "../../../../features/documents/documentCategories";
import { getScopeLabel, VisibilityScope } from "../../../../features/documents/visibilityScopes";

const statusColors = {
    ACTIVE: "green",
    INACTIVE: "volcano",
    PENDING_APPROVAL: "gold",
    PENDING_DELETE: "gold",
    REJECTED: "red",
    // ĐÃ XÓA cũ hiển thị giống Ngừng hiển thị
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

const scopeShortLabels = {
    [VisibilityScope.Public]: "Tất cả",
    [VisibilityScope.Accounting]: "Kế toán",
    [VisibilityScope.Receptionist]: "Lễ tân",
    [VisibilityScope.Resident]: "Cư dân",
};

export default function DocumentTable({
    data,
    loading,
    pagination,
    onPreview,
    onUploadVersion,
    onEdit,
    onVersions,
    onDownload,
    onDelete,
    onRestore,
    onActivate,
}) {
    const getRecordScope = useCallback((record) => {
        return record?.visibilityScope ?? record?.visibility_scope ?? record?.VisibilityScope ?? null;
    }, []);

    const renderVersionSummary = (record) => {
        const latestVersion = record.latestVersionNo;
        const currentVersion = record.currentVersion;
        const hasPublished = typeof currentVersion === "number";

        return (
            <Space direction="vertical" size={4}>
                <span>
                    Mới nhất: <strong>{latestVersion ? `v${latestVersion}` : "—"}</strong>
                </span>
                <Tag color={hasPublished ? "green" : "default"}>
                    {hasPublished ? `Đang hiển thị v${currentVersion}` : "Chưa phát hành"}
                </Tag>
            </Space>
        );
    };

    const columns = useMemo(
        () => [
            {
                title: "Tên tài liệu",
                dataIndex: "title",
                key: "title",
                width: 260,
                render: (value, record) => (
                    <Space direction="vertical" size={0}>
                        <strong>{value}</strong>
                        {record.code && <span className="text-muted" style={{ fontSize: 12 }}>{record.code}</span>}
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
                    const categoryValue = typeof category === "number" ? category : stringToCategoryEnum(category);
                    return getCategoryLabel(categoryValue);
                },
            },
            {
                title: "Phạm vi",
                dataIndex: "visibilityScope",
                key: "visibilityScope",
                width: 200,
                render: (_, record) => {
                    const scopeValue = getRecordScope(record) ?? VisibilityScope.Public;
                    const label = scopeShortLabels[scopeValue] || getScopeLabel(scopeValue);
                    return <Tag>{label}</Tag>;
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
                width: 130,
                render: (value) => <Tag color={statusColors[String(value).toUpperCase()] || "default"}>{formatStatus(value)}</Tag>,
            },
            {
                title: "Trạng thái phiên bản",
                key: "versionSummary",
                width: 220,
                render: (_, record) => renderVersionSummary(record),
            },
            {
                title: "",
                key: "actions",
                width: 80,
                align: "center",
                render: (_, record) => {
                    const statusValue = String(record.status || "").toUpperCase();
                    const isDeletedDocument = statusValue === "DELETED";
                    const isPendingContent = statusValue === "PENDING_APPROVAL" || record.status === "Chờ duyệt";
                    const isPendingDelete = statusValue === "PENDING_DELETE" || record.status === "Chờ duyệt xóa";
                    const isInactive = statusValue === "INACTIVE" || record.status === "Ngừng hiển thị";

                    // Khi tài liệu đang ngừng hiển thị -> cho phép bật lại + xem/tải xuống
                    if (isInactive) {
                        const inactiveItems = [
                            ...(record.fileId
                                ? [
                                    {
                                        key: "view",
                                        label: "Xem",
                                        icon: <EyeOutlined />,
                                        onClick: () => onPreview(record),
                                    },
                                ]
                                : []),
                            ...(record.fileId
                                ? [
                                    {
                                        key: "download",
                                        label: "Tải xuống",
                                        icon: <DownloadOutlined />,
                                        onClick: () => onDownload(record.fileId),
                                    },
                                ]
                                : []),
                            {
                                key: "delete",
                                label: "Xóa vĩnh viễn",
                                icon: <DeleteOutlined />,
                                danger: true,
                                onClick: () => onDelete(record),
                            },
                            {
                                key: "activate",
                                label: "Bật hiển thị lại",
                                icon: <RollbackOutlined />,
                                onClick: () => onActivate && onActivate(record),
                            },
                        ];

                        return (
                            <Dropdown menu={{ items: inactiveItems }} trigger={["click"]} placement="bottomRight">
                                <Button type="text" icon={<MoreOutlined />} size="small" style={{ border: "none", boxShadow: "none" }} />
                            </Dropdown>
                        );
                    }

                    // Khi đang chờ duyệt (nội dung hoặc xóa) -> chỉ cho Xem + Tải xuống
                    if (isPendingContent || isPendingDelete) {
                        const pendingItems = [
                            ...(record.fileId
                                ? [
                                    {
                                        key: "view",
                                        label: "Xem",
                                        icon: <EyeOutlined />,
                                        onClick: () => onPreview(record),
                                    },
                                ]
                                : []),
                            ...(record.fileId
                                ? [
                                    {
                                        key: "download",
                                        label: "Tải xuống",
                                        icon: <DownloadOutlined />,
                                        onClick: () => onDownload(record.fileId),
                                    },
                                ]
                                : []),
                        ];

                        return (
                            <Dropdown menu={{ items: pendingItems }} trigger={["click"]} placement="bottomRight">
                                <Button type="text" icon={<MoreOutlined />} size="small" style={{ border: "none", boxShadow: "none" }} />
                            </Dropdown>
                        );
                    }

                    // Các trạng thái khác: đầy đủ thao tác
                    const items = [
                        ...(record.fileId
                            ? [
                                {
                                    key: "view",
                                    label: "Xem",
                                    icon: <EyeOutlined />,
                                    onClick: () => onPreview(record),
                                },
                            ]
                            : []),
                        {
                            key: "upload",
                            label: "Tải phiên bản mới",
                            icon: <UploadOutlined />,
                            onClick: () => onUploadVersion(record),
                        },
                        {
                            key: "edit",
                            label: "Sửa thông tin",
                            icon: <EditOutlined />,
                            onClick: () => onEdit(record),
                        },
                        {
                            key: "versions",
                            label: "Xem phiên bản",
                            icon: <FileTextOutlined />,
                            onClick: () => onVersions(record),
                        },
                        ...(record.fileId
                            ? [
                                {
                                    key: "download",
                                    label: "Tải xuống",
                                    icon: <DownloadOutlined />,
                                    onClick: () => onDownload(record.fileId),
                                },
                            ]
                            : []),
                        ...(isDeletedDocument
                            ? [
                                {
                                    key: "requestRestore",
                                    label: "Yêu cầu hiển thị lại",
                                    icon: <RollbackOutlined />,
                                    onClick: () => onRestore(record),
                                },
                            ]
                            : []),
                        {
                            key: "delete",
                            label: "Ngừng hiển thị",
                            danger: true,
                            onClick: () => onDelete(record),
                        },
                    ];

                    return (
                        <Dropdown menu={{ items }} trigger={["click"]} placement="bottomRight">
                            <Button type="text" icon={<MoreOutlined />} size="small" style={{ border: "none", boxShadow: "none" }} />
                        </Dropdown>
                    );
                },
            },
        ],
        [getRecordScope, onPreview, onUploadVersion, onEdit, onVersions, onDownload, onDelete, onRestore, onActivate]
    );

    return (
        <div className="bg-gray-100 border border-gray-200 rounded-md p-0">
            <div className="bg-white p-0 rounded-md">
                <Table rowKey={(r) => r.documentId || r.id} columns={columns} dataSource={data.items} loading={loading} pagination={pagination} />
            </div>
        </div>
    );
}

