import React from "react";
import { Modal, Card, Row, Col, Typography, Tag, Table, Button, Popconfirm } from "antd";

const { Text } = Typography;

const INVOICE_STATUS_LABELS = {
    DRAFT: "Nháp",
    PENDING: "Chờ duyệt",
    ISSUED: "Đã phát hành",
    PAID: "Đã thanh toán",
    CANCELLED: "Đã hủy",
};

export default function InvoiceDetailModal({ open, invoice, onClose, onDelete, detailDeleting, canDelete = true }) {
    const isDraft = invoice?.status?.toUpperCase() === 'DRAFT';
    const allowDelete = canDelete && isDraft;
    const detailColumns = [
        {
            title: 'Dịch vụ',
            dataIndex: 'description',
            key: 'description',
            render: (text, record) => text || record.serviceName || '-'
        },
        {
            title: 'Số lượng',
            dataIndex: 'quantity',
            key: 'quantity',
            align: 'right',
        },
        {
            title: 'Đơn giá',
            dataIndex: 'unitPrice',
            key: 'unitPrice',
            align: 'right',
            render: (val) => Number(val || 0).toLocaleString('vi-VN') + ' đ'
        },
        {
            title: 'Thành tiền',
            dataIndex: 'amount',
            key: 'amount',
            align: 'right',
            render: (val) => Number(val || 0).toLocaleString('vi-VN') + ' đ'
        },
        {
            title: 'Thao tác',
            key: 'action',
            render: (_, record) => allowDelete ? (
                <Popconfirm
                    title="Xóa chi tiết hóa đơn"
                    description="Bạn có chắc chắn muốn xóa chi tiết này?"
                    onConfirm={() => onDelete(record.invoiceDetailId)}
                    okText="Xóa"
                    cancelText="Hủy"
                    okButtonProps={{ danger: true, loading: detailDeleting === record.invoiceDetailId }}
                >
                    <Button type="link" size="small" danger loading={detailDeleting === record.invoiceDetailId}>
                        Xóa
                    </Button>
                </Popconfirm>
            ) : null
        }
    ];

    return (
        <Modal
            open={open}
            onCancel={onClose}
            title={`Chi tiết hóa đơn ${invoice?.invoiceNo || ''}`}
            footer={null}
            width={800}
        >
            {invoice && (
                <div>
                    <Card size="small" className="mb-4">
                        <Row gutter={[16, 8]}>
                            <Col span={12}>
                                <Text strong>Mã hóa đơn:</Text> <Text>{invoice.invoiceNo || "—"}</Text>
                            </Col>
                            <Col span={12}>
                                <Text strong>Căn hộ:</Text> <Text>{invoice.apartmentNumber || "—"}</Text>
                            </Col>
                            <Col span={12}>
                                <Text strong>Ngày phát hành:</Text> <Text>{invoice.issueDate}</Text>
                            </Col>
                            <Col span={12}>
                                <Text strong>Hạn thanh toán:</Text> <Text>{invoice.dueDate}</Text>
                            </Col>
                            <Col span={12}>
                                <Text strong>Trạng thái:</Text> <Tag color={invoice.status === 'DRAFT' ? 'orange' : 'green'}>{INVOICE_STATUS_LABELS[invoice.status] || invoice.status}</Tag>
                            </Col>
                            <Col span={12}>
                                <Text strong>Tổng tiền:</Text> <Text strong className="text-lg text-red-600">{Number(invoice.totalAmount || 0).toLocaleString('vi-VN')} đ</Text>
                            </Col>

                        </Row>
                    </Card>

                    <Table
                        columns={detailColumns}
                        dataSource={invoice.details || []}
                        rowKey="invoiceDetailId"
                        pagination={false}
                        size="small"
                    />
                </div>
            )}
        </Modal>
    );
}


