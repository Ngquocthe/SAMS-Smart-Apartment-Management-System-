import React from "react";
import { Modal, Card, Row, Col, Typography, Tag, Table, Button, Popconfirm } from "antd";
import { DeleteOutlined } from "@ant-design/icons";

const { Text } = Typography;

const VOUCHER_STATUS_LABELS = {
    DRAFT: "Nháp",
    APPROVED: "Đã duyệt",
    PENDING: "Chờ duyệt",
    PAID: "Đã chi",
    CANCELLED: "Đã hủy",
};

export default function VoucherDetailModal({ open, voucher, onClose, onDelete, detailDeleting, canDelete = true }) {
    const isDraft = voucher?.status?.toUpperCase() === 'DRAFT';
    const allowDelete = canDelete && isDraft;
    const itemColumns = [
        {
            title: 'Dịch vụ',
            dataIndex: 'serviceTypeName',
            key: 'serviceTypeName',
        },
        {
            title: 'Số tiền',
            dataIndex: 'amount',
            key: 'amount',
            align: 'right',
            render: (val) => Number(val || 0).toLocaleString('vi-VN') + ' đ'
        },
        {
            title: 'Mô tả',
            dataIndex: 'description',
            key: 'description',
        },
        {
            title: 'Thao tác',
            key: 'action',
            render: (_, record) => allowDelete ? (
                <Popconfirm
                    title="Xóa chi tiết phiếu chi"
                    description="Bạn có chắc chắn muốn xóa chi tiết này?"
                    onConfirm={() => onDelete(record.voucherItemsId)}
                    okText="Xóa"
                    cancelText="Hủy"
                    okButtonProps={{ danger: true, loading: detailDeleting === record.voucherItemsId }}
                >
                    <Button type="link" size="small" danger icon={<DeleteOutlined />} loading={detailDeleting === record.voucherItemsId}>
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
            title={`Chi tiết phiếu chi ${voucher?.voucherNumber || ''}`}
            footer={null}
            width={700}
        >
            {voucher && (
                <div>
                    <Card size="small" className="mb-4">
                        <Row gutter={[16, 8]}>
                            <Col span={12}>
                                <Text strong>Mã phiếu chi:</Text> <Text>{voucher.voucherNumber || "—"}</Text>
                            </Col>
                            <Col span={12}>
                                <Text strong>Ngày:</Text> <Text>{voucher.date}</Text>
                            </Col>
                            <Col span={12}>
                                <Text strong>Loại:</Text> <Text>{voucher.type || "—"}</Text>
                            </Col>
                            <Col span={12}>
                                <Text strong>Trạng thái:</Text> <Tag color={voucher.status === 'DRAFT' ? 'orange' : 'green'}>{VOUCHER_STATUS_LABELS[voucher.status] || voucher.status}</Tag>
                            </Col>
                            <Col span={12}>
                                <Text strong>Tổng tiền:</Text> <Text strong className="text-lg text-red-600">{Number(voucher.totalAmount || 0).toLocaleString('vi-VN')} đ</Text>
                            </Col>
                            {voucher.description && (
                                <Col span={24}>
                                    <Text strong>Ghi chú:</Text> <Text>{voucher.description}</Text>
                                </Col>
                            )}
                        </Row>
                    </Card>

                    <Table
                        columns={itemColumns}
                        dataSource={voucher.items || []}
                        rowKey="voucherItemsId"
                        pagination={false}
                        size="small"
                    />
                </div>
            )}
        </Modal>
    );
}


