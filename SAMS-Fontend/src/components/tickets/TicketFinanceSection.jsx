import React from "react";
import { Card, Row, Col, Button, Typography } from "antd";

const { Text } = Typography;

export default function TicketFinanceSection({
    ticket,
    invoices = [],
    vouchers = [],
    onInvoiceClick,
    onVoucherClick,
    onCreateInvoice,
    onCreateVoucher,
    disabled = false
}) {
    // Normalize scope để xử lý các trường hợp khác nhau về dấu
    const rawScope = ticket?.scope || 'Tòa nhà';
    const scopeLower = rawScope.toLowerCase().trim();
    const isApartment = scopeLower === 'theo căn hộ' || scopeLower === 'theo can ho';
    const isBuilding = scopeLower === 'tòa nhà' || scopeLower === 'toà nhà' || scopeLower === 'toa nha';

    return (
        <Card
            title={
                <div className="flex items-center space-x-2">
                    <span>Thu/Chi</span>
                </div>
            }
            className="shadow-sm"
            extra={
                <div className="flex gap-2">
                    {isApartment && (
                        <Button
                            size="small"
                            type="primary"
                            disabled={disabled}
                            onClick={onCreateInvoice}
                        >
                            Tạo hóa đơn
                        </Button>
                    )}
                    {isBuilding && (
                        <Button
                            size="small"
                            onClick={onCreateVoucher}
                            disabled={disabled}
                        >
                            Tạo phiếu chi
                        </Button>
                    )}
                </div>
            }
        >
            <Row gutter={[12, 12]}>
                {isApartment && (
                    <Col xs={24}>
                        <Card size="small" title="Hóa đơn" className="h-full">
                            {invoices && invoices.length > 0 ? (
                                <div className="space-y-2">
                                    {invoices.map(inv => (
                                        <div key={inv.invoiceId || inv.id} className="flex justify-between items-center text-sm">
                                            <div className="flex items-center gap-2">
                                                <span>#{(inv.number || inv.code || '').toString()}</span>
                                                <Button
                                                    type="link"
                                                    size="small"
                                                    onClick={() => onInvoiceClick(inv)}
                                                >
                                                    Xem chi tiết
                                                </Button>
                                            </div>
                                            <span>{Number(inv.amount || 0).toLocaleString('vi-VN')} đ</span>
                                        </div>
                                    ))}
                                </div>
                            ) : (
                                <Text type="secondary">Chưa có hóa đơn</Text>
                            )}
                        </Card>
                    </Col>
                )}
                {isBuilding && (
                    <Col xs={24}>
                        <Card size="small" title="Phiếu chi" className="h-full">
                            {vouchers && vouchers.length > 0 ? (
                                <div className="space-y-2">
                                    {vouchers.map(v => (
                                        <div key={v.voucherId || v.id} className="flex justify-between items-center text-sm">
                                            <div className="flex items-center gap-2">
                                                <span>#{(v.number || v.code || '').toString()}</span>
                                                <Button
                                                    type="link"
                                                    size="small"
                                                    onClick={() => onVoucherClick(v)}
                                                >
                                                    Xem chi tiết
                                                </Button>
                                            </div>
                                            <span>{Number(v.amount || 0).toLocaleString('vi-VN')} đ</span>
                                        </div>
                                    ))}
                                </div>
                            ) : (
                                <Text type="secondary">Chưa có phiếu chi</Text>
                            )}
                        </Card>
                    </Col>
                )}
            </Row>
        </Card>
    );
}


