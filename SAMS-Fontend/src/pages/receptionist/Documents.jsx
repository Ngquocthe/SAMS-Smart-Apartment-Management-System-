import React, { useCallback, useEffect, useMemo, useState, useRef } from "react";
import { Row, Col, Input, Select, Empty, message, Spin, Typography } from "antd";
import { SearchOutlined } from "@ant-design/icons";
import documentsApi from "../../features/documents/documentsApi";
import { getCategoryOptions } from "../../features/documents/documentCategories";
import ResidentDocumentCard from "../resident/documents/ResidentDocumentCard";
import ResidentDocumentPreviewModal from "../resident/documents/ResidentDocumentPreviewModal";

const { Title } = Typography;

const defaultQuery = {
    keyword: "",
    category: undefined,
};

export default function ReceptionistDocuments() {
    const [query, setQuery] = useState(defaultQuery);
    const [loading, setLoading] = useState(false);
    const [documents, setDocuments] = useState([]);
    const [previewRecord, setPreviewRecord] = useState(null);
    const [previewOpen, setPreviewOpen] = useState(false);
    const [previewIndex, setPreviewIndex] = useState(-1);
    const searchTimeoutRef = useRef(null);

    const categoryOptions = useMemo(() => [{ label: "Tất cả", value: undefined }, ...getCategoryOptions()], []);

    /**
     * Lấy danh sách tài liệu dành cho lễ tân từ API
     * Hỗ trợ tìm kiếm theo tiêu đề và lọc theo danh mục
     * Tự động cập nhật state documents và loading
     */
    const fetchDocuments = useCallback(async () => {
        setLoading(true);
        try {
            const result = await documentsApi.getReceptionistDocuments({
                Title: query.keyword || undefined,
                Category: query.category || undefined,
            });
            setDocuments(Array.isArray(result) ? result : []);
        } catch (error) {
            console.error("Load receptionist documents error:", error);
            message.error("Không tải được danh sách tài liệu cho lễ tân");
        } finally {
            setLoading(false);
        }
    }, [query]);

    /**
     * Debounce search: Tự động tìm kiếm sau 500ms khi người dùng ngừng nhập
     * Tránh gọi API quá nhiều lần khi người dùng đang gõ
     * Cleanup timeout khi component unmount hoặc query thay đổi
     */
    useEffect(() => {
        if (searchTimeoutRef.current) {
            clearTimeout(searchTimeoutRef.current);
        }
        searchTimeoutRef.current = setTimeout(() => {
            fetchDocuments();
        }, 500);

        return () => {
            if (searchTimeoutRef.current) {
                clearTimeout(searchTimeoutRef.current);
            }
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [query.keyword, query.category]);

    /**
     * Xử lý tải xuống file tài liệu
     * Mở link download trong tab mới
     * @param {string} fileId - ID của file cần tải xuống
     */
    const handleDownload = (fileId) => {
        const url = documentsApi.buildDownloadUrl(fileId);
        window.open(url, "_blank");
    };

    /**
     * Mở modal preview tài liệu
     * Tìm index của document trong danh sách để hỗ trợ điều hướng (next/prev)
     * @param {object} record - Document object cần preview
     */
    const handlePreview = (record) => {
        const index = documents.findIndex((d) => d.documentId === record.documentId);
        setPreviewIndex(index);
        setPreviewRecord(record);
        setPreviewOpen(true);
    };

    /**
     * Chuyển sang tài liệu tiếp theo trong danh sách khi đang preview
     * Chỉ chuyển nếu chưa phải tài liệu cuối cùng
     */
    const handleNext = () => {
        if (previewIndex < documents.length - 1) {
            const nextIndex = previewIndex + 1;
            setPreviewIndex(nextIndex);
            setPreviewRecord(documents[nextIndex]);
        }
    };

    /**
     * Chuyển về tài liệu trước đó trong danh sách khi đang preview
     * Chỉ chuyển nếu chưa phải tài liệu đầu tiên
     */
    const handlePrev = () => {
        if (previewIndex > 0) {
            const prevIndex = previewIndex - 1;
            setPreviewIndex(prevIndex);
            setPreviewRecord(documents[prevIndex]);
        }
    };

    return (
        <div style={{ padding: "24px", backgroundColor: "#f5f5f5", minHeight: "100vh" }}>
            <div style={{ maxWidth: 1400, margin: "0 auto" }}>
                {/* Header */}
                <div style={{ marginBottom: 24 }}>
                    <Title level={2} style={{ margin: 0, marginBottom: 16 }}>
                        Tài liệu cho lễ tân
                    </Title>
                    <p style={{ color: "#8c8c8c", margin: 0 }}>
                        Xem và tải xuống các tài liệu, quy trình làm việc dành cho lễ tân
                    </p>
                </div>

                {/* Search and Filter */}
                <div
                    style={{
                        backgroundColor: "#fff",
                        padding: 20,
                        borderRadius: 12,
                        marginBottom: 24,
                        boxShadow: "0 2px 8px rgba(0,0,0,0.1)",
                    }}
                >
                    <Row gutter={[16, 16]}>
                        <Col xs={24} sm={12} md={10}>
                            <Input
                                prefix={<SearchOutlined style={{ color: "#bfbfbf" }} />}
                                placeholder="Tìm kiếm tài liệu..."
                                value={query.keyword}
                                onChange={(e) => setQuery((q) => ({ ...q, keyword: e.target.value }))}
                                allowClear
                                size="large"
                                style={{ borderRadius: 8 }}
                            />
                        </Col>
                        <Col xs={24} sm={12} md={8}>
                            <Select
                                placeholder="Chọn phân loại"
                                value={query.category}
                                options={categoryOptions}
                                onChange={(value) => setQuery((q) => ({ ...q, category: value }))}
                                allowClear
                                size="large"
                                style={{ width: "100%", borderRadius: 8 }}
                            />
                        </Col>
                    </Row>
                </div>

                {/* Documents Grid */}
                <Spin spinning={loading}>
                    {documents.length === 0 && !loading ? (
                        <div
                            style={{
                                backgroundColor: "#fff",
                                padding: 60,
                                borderRadius: 12,
                                textAlign: "center",
                                boxShadow: "0 2px 8px rgba(0,0,0,0.1)",
                            }}
                        >
                            <Empty
                                description={
                                    <span style={{ color: "#8c8c8c" }}>
                                        {query.keyword || query.category
                                            ? "Không tìm thấy tài liệu phù hợp"
                                            : "Chưa có tài liệu dành cho lễ tân"}
                                    </span>
                                }
                            />
                        </div>
                    ) : (
                        <Row gutter={[20, 20]}>
                            {documents.map((doc) => (
                                <Col key={doc.documentId} xs={24} sm={12} md={8} lg={6}>
                                    <ResidentDocumentCard
                                        doc={doc}
                                        onPreview={handlePreview}
                                        onDownload={handleDownload}
                                    />
                                </Col>
                            ))}
                        </Row>
                    )}
                </Spin>

                {/* Preview Modal */}
                <ResidentDocumentPreviewModal
                    open={previewOpen}
                    record={previewRecord}
                    onClose={() => {
                        setPreviewOpen(false);
                        setPreviewIndex(-1);
                    }}
                    onDownload={handleDownload}
                    onNext={previewIndex < documents.length - 1 ? handleNext : null}
                    onPrev={previewIndex > 0 ? handlePrev : null}
                    currentIndex={previewIndex}
                    totalCount={documents.length}
                />
            </div>
        </div>
    );
}




