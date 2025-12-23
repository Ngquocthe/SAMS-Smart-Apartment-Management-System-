import React, { useCallback, useEffect, useMemo, useState, useRef } from "react";
import { Row, Col, Input, Select, Empty, message, Spin, Typography } from "antd";
import { SearchOutlined } from "@ant-design/icons";
import documentsApi from "../../features/documents/documentsApi";
import { getCategoryOptions } from "../../features/documents/documentCategories";
import ResidentDocumentCard from "./documents/ResidentDocumentCard";
import ResidentDocumentPreviewModal from "./documents/ResidentDocumentPreviewModal";

const { Title } = Typography;

const defaultQuery = {
    keyword: "",
    category: undefined,
};

export default function ResidentDocuments() {
    const [query, setQuery] = useState(defaultQuery);
    const [loading, setLoading] = useState(false);
    const [documents, setDocuments] = useState([]);
    const [previewRecord, setPreviewRecord] = useState(null);
    const [previewOpen, setPreviewOpen] = useState(false);
    const [previewIndex, setPreviewIndex] = useState(-1);
    const searchTimeoutRef = useRef(null);

    const categoryOptions = useMemo(() => [{ label: "Tất cả", value: undefined }, ...getCategoryOptions()], []);

    const fetchDocuments = useCallback(async () => {
        setLoading(true);
        try {
            const result = await documentsApi.getResidentDocuments({
                Title: query.keyword || undefined,
                Category: query.category || undefined,
            });
            setDocuments(Array.isArray(result) ? result : []);
        } catch (error) {
            console.error("Load resident documents error:", error);
            message.error("Không tải được danh sách tài liệu cư dân");
        } finally {
            setLoading(false);
        }
    }, [query]);

    // Debounce search
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

    const handleDownload = (fileId) => {
        const url = documentsApi.buildDownloadUrl(fileId);
        window.open(url, "_blank");
    };

    const handlePreview = (record) => {
        const index = documents.findIndex((d) => d.documentId === record.documentId);
        setPreviewIndex(index);
        setPreviewRecord(record);
        setPreviewOpen(true);
    };

    const handleNext = () => {
        if (previewIndex < documents.length - 1) {
            const nextIndex = previewIndex + 1;
            setPreviewIndex(nextIndex);
            setPreviewRecord(documents[nextIndex]);
        }
    };

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
                        Tài liệu
                    </Title>
                    <p style={{ color: "#8c8c8c", margin: 0 }}>Xem và tải xuống các tài liệu được chia sẻ</p>
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
                                            : "Chưa có tài liệu dành cho cư dân"}
                                    </span>
                                }
                            />
                        </div>
                    ) : (
                        <Row gutter={[20, 20]}>
                            {documents.map((doc) => (
                                <Col key={doc.documentId} xs={24} sm={12} md={8} lg={6}>
                                    <ResidentDocumentCard doc={doc} onPreview={handlePreview} onDownload={handleDownload} />
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



