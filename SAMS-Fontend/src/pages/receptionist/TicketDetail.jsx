import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { Button, Card, message, Form, Spin, Row, Col, Typography, Input, Select, DatePicker, Alert } from "antd";
import { ArrowLeftOutlined } from "@ant-design/icons";
import dayjs from "dayjs";
import ticketsApi from "../../features/tickets/ticketsApi";
import api from "../../lib/apiClient";
import { useUser } from "../../hooks/useUser";
import { deleteInvoiceDetail } from "../../features/accountant/invoicedetailsApi";
import CreateInvoiceModal from "../../components/tickets/CreateInvoiceModal";
import CreateVoucherModal from "../../components/tickets/CreateVoucherModal";
import InvoiceDetailModal from "../../components/tickets/InvoiceDetailModal";
import VoucherDetailModal from "../../components/tickets/VoucherDetailModal";
import TicketHeader from "../../components/tickets/TicketHeader";
import TicketAttachments from "../../components/tickets/TicketAttachments";
import TicketComments from "../../components/tickets/TicketComments";
import TicketStatusFlow, {
    STATUS_FLOW as STATUS_SEQUENCE
} from "../../components/tickets/TicketStatusFlow";
import TicketFinanceSection from "../../components/tickets/TicketFinanceSection";

export default function TicketDetail() {
    const { id } = useParams();
    const navigate = useNavigate();
    const { userId } = useUser();
    const [ticket, setTicket] = useState(null);
    const [loading, setLoading] = useState(true);
    const [commentLoading, setCommentLoading] = useState(false);
    const [commentForm] = Form.useForm();
    const [metaForm] = Form.useForm();
    const [savingMeta, setSavingMeta] = useState(false);
    const [selectedPriority, setSelectedPriority] = useState(null);
    /**
     * Lấy index của status trong STATUS_SEQUENCE
     * Dùng để xác định bước hiện tại trong quy trình xử lý ticket
     * @param {string} s - Status của ticket
     * @returns {number} Index của status (mặc định là 0 nếu không tìm thấy)
     */
    const getStatusIndex = (s) => {
        const idx = STATUS_SEQUENCE.indexOf(String(s || ""));
        return idx >= 0 ? idx : 0;
    };
    const [currentStep, setCurrentStep] = useState(0);
    const [statusUpdating, setStatusUpdating] = useState(false);
    const [invoices, setInvoices] = useState([]);
    const [vouchers, setVouchers] = useState([]);
    const [creatingInv, setCreatingInv] = useState(false);
    const [creatingVou, setCreatingVou] = useState(false);
    const [invForm] = Form.useForm();
    const [vouForm] = Form.useForm();
    const [serviceTypes, setServiceTypes] = useState([]);
    const [serviceTypesLoading, setServiceTypesLoading] = useState(false);
    const [invoiceDetailOpen, setInvoiceDetailOpen] = useState(false);
    const [voucherDetailOpen, setVoucherDetailOpen] = useState(false);
    const [invoiceDetail, setInvoiceDetail] = useState(null);
    const [voucherDetail, setVoucherDetail] = useState(null);
    const [detailDeleting, setDetailDeleting] = useState(null);

    useEffect(() => {
        if (id) {
            fetchTicketDetails();
        }
    }, [id]); // eslint-disable-line

    useEffect(() => {
        if (ticket) {
            const priority = ticket.priority;
            setSelectedPriority(priority);
            metaForm.setFieldsValue({
                priority: priority,
                expectedCompletionAt: ticket.expectedCompletionAt ? dayjs(ticket.expectedCompletionAt) : null,
            });
        }
    }, [ticket]); // eslint-disable-line

    /**
     * Lấy chi tiết ticket từ API
     * Bao gồm: thông tin ticket, số căn hộ (nếu có), danh sách hóa đơn và phiếu chi liên quan
     * Tự động fetch apartment number nếu thiếu
     */
    const fetchTicketDetails = async () => {
        setLoading(true);
        try {
            const ticketData = await ticketsApi.getById(id);

            // Nếu không có apartmentNumber nhưng có apartmentId, fetch từ API
            if (ticketData?.apartmentId && !ticketData?.apartmentNumber) {
                try {
                    const aptRes = await api.get(`/Apartment/${ticketData.apartmentId}`);
                    if (aptRes?.data?.number) {
                        ticketData.apartmentNumber = aptRes.data.number;
                    }
                } catch (e) {
                    // Failed to fetch apartment number
                }
            }

            setTicket(ticketData);
            setCurrentStep(getStatusIndex(ticketData?.status));
            // Load invoice & voucher theo ticket
            try {
                const [invRes, vouRes] = await Promise.all([
                    api.get(`/Invoice/by-ticket/${id}`),
                    api.get(`/Voucher/by-ticket/${id}`)
                ]);
                setInvoices(invRes?.data?.items || invRes?.data || []);
                setVouchers(vouRes?.data?.items || vouRes?.data || []);
            } catch (e) {
                setInvoices([]);
                setVouchers([]);
            }
        } catch (error) {
            message.error("Không thể tải chi tiết ticket");
            navigate("/receptionist/tickets");
        } finally {
            setLoading(false);
        }
    };

    /**
     * Xử lý thay đổi trạng thái ticket
     * Chuyển ticket sang trạng thái tiếp theo trong quy trình (Mới tạo → Đã tiếp nhận → Đang xử lý → Hoàn thành → Đã đóng)
     * @param {number} nextIdx - Index của trạng thái tiếp theo trong STATUS_SEQUENCE
     */
    const handleChangeStatus = async (nextIdx) => {
        if (!ticket) return;
        const nextStatus = STATUS_SEQUENCE[nextIdx];
        if (!nextStatus || nextStatus === ticket.status) return;

        setStatusUpdating(true);
        try {
            await ticketsApi.changeStatus({ ticketId: ticket.ticketId, status: nextStatus, changedByUserId: userId });
            message.success("Đã cập nhật trạng thái");
            await fetchTicketDetails();
        } catch (e) {
            const raw = e?.response?.data;
            const msg = (typeof raw === 'string' ? raw : raw?.message) || e?.message || 'Cập nhật trạng thái thất bại';
            message.error(msg);
        } finally {
            setStatusUpdating(false);
        }
    };

    /**
     * Thêm bình luận mới vào ticket
     */
    const handleAddComment = async (values) => {
        setCommentLoading(true);
        try {
            const payload = {
                ticketId: id,
                content: values.content,
                commentedBy: userId
            };

            await ticketsApi.addComment(payload);
            message.success("Đã thêm bình luận");
            commentForm.resetFields();

            // Refresh ticket details to get updated comments
            await fetchTicketDetails();
        } catch (error) {
            message.error("Không thể thêm bình luận");
        } finally {
            setCommentLoading(false);
        }
    };

    /**
     * Cập nhật mức độ ưu tiên và ngày hoàn thành dự kiến (SLA) của ticket
     * Chỉ cho phép chỉnh sửa khi ticket đã ở trạng thái "Đã tiếp nhận" trở đi và chưa hoàn thành/đóng
     * @param {object} values - Form values chứa priority và expectedCompletionAt
     */
    const handleMetaSubmit = async (values) => {
        if (!ticket) return;
        if (isCompleted) {
            message.warning("Ticket đã hoàn thành, không thể chỉnh mức độ ưu tiên/SLA.");
            return;
        }
        setSavingMeta(true);
        try {
            // Đảm bảo priority luôn có giá trị hợp lệ
            // Nếu user chưa chọn priority mới, giữ nguyên priority cũ của ticket
            const priority = values.priority || ticket.priority;
            if (!priority) {
                message.error("Vui lòng chọn mức độ ưu tiên");
                setSavingMeta(false);
                return;
            }

            // Khi chỉ cập nhật priority, không gửi scope và apartmentId
            // Backend sẽ tự lấy từ entity hiện tại và normalize
            const payload = {
                ticketId: ticket.ticketId,
                category: ticket.category,
                priority: priority,
                subject: ticket.subject,
                description: ticket.description,
                hasInvoice: ticket.hasInvoice,
                updatedByUserId: userId,
                // Chỉ gửi expectedCompletionAt nếu có priority
                expectedCompletionAt: priority && values.expectedCompletionAt
                    ? values.expectedCompletionAt.toISOString()
                    : null
            };
            await ticketsApi.update(payload);
            message.success("Đã cập nhật mức độ ưu tiên và ngày hoàn thành dự kiến");
            await fetchTicketDetails();
        } catch (error) {
            // Hiển thị thông báo lỗi cụ thể từ backend
            const errorMessage = error?.response?.data?.message
                || error?.response?.data?.errors?.join?.(' | ')
                || error?.message
                || "Không thể cập nhật mức độ ưu tiên";
            message.error(errorMessage);
        } finally {
            setSavingMeta(false);
        }
    };
    if (loading) {
        return (
            <div className="p-6">
                <div className="flex justify-center items-center h-64">
                    <Spin size="large" />
                </div>
            </div>
        );
    }

    if (!ticket) {
        return (
            <div className="p-6">
                <div className="text-center">
                    <h2 className="text-xl font-semibold text-gray-600">Ticket không tồn tại</h2>
                    <Button onClick={() => navigate("/receptionist/tickets")} className="mt-4">
                        Quay lại danh sách
                    </Button>
                </div>
            </div>
        );
    }

    // Check if ticket is closed
    const currentStatusIndex = getStatusIndex(ticket?.status);
    const currentStatus = STATUS_SEQUENCE[currentStatusIndex];
    const priorityRequiredIndex = STATUS_SEQUENCE.indexOf("Đã tiếp nhận");
    const isCompleted = currentStatus === "Hoàn thành";
    const isClosed = currentStatus === "Đã đóng";
    const canEditPriority = currentStatusIndex >= priorityRequiredIndex && !isClosed && !isCompleted;

    return (
        <div className="p-6">
            {/* Alert when closed */}
            {isClosed && (
                <div className="mb-4">
                    <Card style={{ background: '#fff0f0', border: '1px solid #ffccc7' }}>
                        <b style={{ color: '#cf1322' }}>Ticket đã bị đóng. Không thể thay đổi, bình luận, hoặc tạo hóa đơn/phiếu chi.</b>
                    </Card>
                </div>
            )}
            {/* Header */}
            <div className="mb-8">
                <Button
                    icon={<ArrowLeftOutlined />}
                    onClick={() => navigate("/receptionist/tickets")}
                    className="mb-6"
                    size="large"
                >
                    Quay lại danh sách
                </Button>
                <TicketHeader ticket={ticket} />
            </div>

            <Row gutter={[24, 24]}>
                {/* Main Content */}
                <Col xs={24} lg={16}>
                    <div className="space-y-6">
                        {/* Description */}
                        <Card
                            title={
                                <div className="flex items-center space-x-2">
                                    <span>Mô tả chi tiết</span>
                                </div>
                            }
                            className="shadow-sm"
                        >
                            <Typography.Text className="!mb-0">
                                {ticket.description || (
                                    <Typography.Text type="secondary" italic>Không có mô tả</Typography.Text>
                                )}
                            </Typography.Text>
                        </Card>

                        {/* Attachments */}
                        <TicketAttachments attachments={ticket.attachments} />

                        {/* Process Step Bar */}
                        <TicketStatusFlow
                            ticket={ticket}
                            currentStep={currentStep}
                            onStatusChange={handleChangeStatus}
                            statusUpdating={statusUpdating}
                            disabled={isClosed}
                        />

                        {/* Thu/Chi: Invoice & Voucher */}
                        <TicketFinanceSection
                            ticket={ticket}
                            invoices={invoices}
                            vouchers={vouchers}
                            onInvoiceClick={async (inv) => {
                                try {
                                    const res = await api.get(`/Invoice/by-invoice/${inv.invoiceId || inv.id}`);
                                    setInvoiceDetail(res.data);
                                    setInvoiceDetailOpen(true);
                                } catch (e) {
                                    message.error('Không thể tải chi tiết hóa đơn');
                                }
                            }}
                            onVoucherClick={async (v) => {
                                try {
                                    const res = await api.get(`/Voucher/${v.voucherId || v.id}`);
                                    setVoucherDetail(res.data);
                                    setVoucherDetailOpen(true);
                                } catch (e) {
                                    message.error('Không thể tải chi tiết phiếu chi');
                                }
                            }}
                            onCreateInvoice={async () => {
                                setServiceTypesLoading(true);
                                try {
                                    const res = await api.get('/ServiceTypes/options');
                                    const items = res?.data || [];
                                    setServiceTypes(items.map(st => ({
                                        label: st.label || st.name,
                                        value: st.value || st.serviceTypeId,
                                        isMandatory: st.isMandatory || false
                                    })));
                                } catch (e) {
                                    setServiceTypes([]);
                                } finally {
                                    setServiceTypesLoading(false);
                                    setCreatingInv(true);
                                }
                            }}
                            onCreateVoucher={async () => {
                                setServiceTypesLoading(true);
                                try {
                                    const res = await api.get('/ServiceTypes/options');
                                    const items = res?.data || [];
                                    setServiceTypes(items.map(st => ({
                                        label: st.label || st.name,
                                        value: st.value || st.serviceTypeId,
                                        isMandatory: st.isMandatory || false
                                    })));
                                } catch (e) {
                                    setServiceTypes([]);
                                } finally {
                                    setServiceTypesLoading(false);
                                    setCreatingVou(true);
                                }
                            }}
                            disabled={isClosed}
                        />

                        {/* Comments Timeline */}
                        <TicketComments comments={ticket.ticketComments || []} />
                    </div>
                </Col>

                {/* Sidebar */}
                <Col xs={24} lg={8}>
                    <div className="space-y-6">
                        <Card title="Mức độ & SLA" className="shadow-sm">
                            <Form form={metaForm} onFinish={handleMetaSubmit} layout="vertical">
                                <Form.Item
                                    name="priority"
                                    label="Mức độ ưu tiên"
                                    rules={canEditPriority ? [{ required: true, message: "Chọn mức độ ưu tiên" }] : []}
                                >
                                    <Select
                                        options={["Thấp", "Bình thường", "Khẩn cấp"].map((name) => ({
                                            label: name,
                                            value: name
                                        }))}
                                        disabled={!canEditPriority}
                                        placeholder={
                                            canEditPriority
                                                ? undefined
                                                : isCompleted
                                                    ? "Không thể chỉnh khi ticket đã hoàn thành"
                                                    : "Chỉ chỉnh sau khi chuyển sang 'Đã tiếp nhận'"
                                        }
                                        onChange={(value) => {
                                            setSelectedPriority(value);
                                            if (value) {
                                                // Tính số ngày dựa trên mức độ ưu tiên
                                                let days = 3; // Mặc định "Bình thường"
                                                if (value === "Thấp") {
                                                    days = 5;
                                                } else if (value === "Khẩn cấp") {
                                                    days = 1;
                                                }

                                                // Tính ngày hoàn thành dự kiến = ngày hiện tại + số ngày
                                                const now = dayjs();
                                                const expectedDate = now.add(days, 'day');

                                                // Cập nhật giá trị trong form
                                                metaForm.setFieldsValue({
                                                    expectedCompletionAt: expectedDate
                                                });
                                            } else {
                                                // Nếu xóa mức độ ưu tiên, xóa luôn ngày hoàn thành dự kiến
                                                metaForm.setFieldsValue({
                                                    expectedCompletionAt: null
                                                });
                                            }
                                        }}
                                    />
                                </Form.Item>
                                {selectedPriority && (
                                    <Form.Item label="Ngày hoàn thành dự kiến" name="expectedCompletionAt">
                                        <DatePicker
                                            showTime
                                            format="DD/MM/YYYY HH:mm"
                                            style={{ width: "100%" }}
                                            allowClear
                                            disabled={!canEditPriority}
                                            // Chỉ cho phép chọn từ thời điểm hiện tại + 30 phút trở đi
                                            disabledDate={(current) => {
                                                if (!current) return false;
                                                const now = dayjs();
                                                const min = now.add(30, "minute");
                                                // Chặn tất cả ngày trước ngày của min
                                                if (current.isBefore(min.startOf("day"))) return true;
                                                return false;
                                            }}
                                            disabledTime={(current) => {
                                                const now = dayjs();
                                                const min = now.add(30, "minute");
                                                if (!current) return {};

                                                // Nếu ngày trước ngày min thì chặn toàn bộ giờ/phút
                                                if (current.isBefore(min, "day")) {
                                                    const allHours = Array.from({ length: 24 }, (_, h) => h);
                                                    const allMinutes = Array.from({ length: 60 }, (_, m) => m);
                                                    return {
                                                        disabledHours: () => allHours,
                                                        disabledMinutes: () => allMinutes
                                                    };
                                                }

                                                const disabledHours = () => {
                                                    const hours = [];
                                                    for (let h = 0; h < 24; h++) {
                                                        const hourEnd = current.hour(h).minute(59).second(59);
                                                        if (hourEnd.isBefore(min)) {
                                                            hours.push(h);
                                                        }
                                                    }
                                                    return hours;
                                                };

                                                const disabledMinutes = (selectedHour) => {
                                                    const minutes = [];
                                                    for (let m = 0; m < 60; m++) {
                                                        const candidate = current.hour(selectedHour).minute(m).second(0);
                                                        if (candidate.isBefore(min)) {
                                                            minutes.push(m);
                                                        }
                                                    }
                                                    return minutes;
                                                };

                                                return {
                                                    disabledHours,
                                                    disabledMinutes
                                                };
                                            }}
                                        />
                                    </Form.Item>
                                )}
                                <Form.Item className="!mb-0">
                                    <Button type="primary" htmlType="submit" block loading={savingMeta} disabled={!canEditPriority}>
                                        Lưu thông tin SLA
                                    </Button>
                                </Form.Item>
                            </Form>
                        </Card>
                        {/* Add Comment */}
                        <Card
                            title="Thêm bình luận"
                            className="shadow-sm"
                        >
                            <Alert
                                message="Lưu ý: Khi thay đổi bất kỳ thông tin nào, vui lòng để lại bình luận để mọi người nắm rõ quy trình xử lý."
                                type="info"
                                size="small"
                                className="mb-3"
                            />
                            <Form form={commentForm} onFinish={handleAddComment} layout="vertical">
                                <Form.Item
                                    name="content"
                                    rules={[{ required: true, message: "Nhập nội dung bình luận" }]}
                                >
                                    <Input.TextArea
                                        rows={4}
                                        placeholder="Nhập bình luận của bạn..."
                                        disabled={commentLoading || isClosed}
                                        className="resize-none"
                                    />
                                </Form.Item>
                                <Form.Item className="!mb-0">
                                    <Button
                                        type="primary"
                                        htmlType="submit"
                                        loading={commentLoading}
                                        block
                                        size="large"
                                        disabled={isClosed}
                                    >
                                        Gửi bình luận
                                    </Button>
                                </Form.Item>
                            </Form>
                        </Card>

                        <CreateInvoiceModal
                            open={creatingInv}
                            onCancel={() => { setCreatingInv(false); invForm.resetFields(); }}
                            onOk={async () => {
                                try {
                                    const vals = await invForm.validateFields();
                                    const payload = {
                                        ticketId: ticket.ticketId,
                                        unitPrice: vals.unitPrice != null ? Number(vals.unitPrice) : 0,
                                        quantity: vals.quantity != null ? Number(vals.quantity) : 1,
                                        note: vals.note,
                                        serviceTypeId: vals.serviceTypeId,
                                        createdByUserId: userId || null
                                    };
                                    await api.post('/Invoice/from-ticket', payload);
                                    message.success('Đã tạo hóa đơn');
                                    setCreatingInv(false);
                                    invForm.resetFields();
                                    // reload
                                    const invRes = await api.get(`/Invoice/by-ticket/${id}`);
                                    setInvoices(invRes?.data?.items || invRes?.data || []);
                                    await fetchTicketDetails();
                                } catch (e) {
                                    if (e?.errorFields) return;
                                    message.error(e?.response?.data?.message || 'Tạo hóa đơn thất bại');
                                }
                            }}
                            form={invForm}
                            serviceTypes={serviceTypes}
                            serviceTypesLoading={serviceTypesLoading}
                            disabled={isClosed}
                        />
                        <CreateVoucherModal
                            open={creatingVou}
                            onCancel={() => { setCreatingVou(false); vouForm.resetFields(); }}
                            onOk={async () => {
                                try {
                                    const vals = await vouForm.validateFields();
                                    await api.post('/Voucher/from-ticket', {
                                        ticketId: ticket.ticketId,
                                        amount: Number(vals.amount || 0),
                                        note: vals.note,
                                        serviceTypeId: vals.serviceTypeId,
                                        createdByUserId: userId || null
                                    });
                                    message.success('Đã tạo phiếu chi');
                                    setCreatingVou(false);
                                    vouForm.resetFields();
                                    const vouRes = await api.get(`/Voucher/by-ticket/${id}`);
                                    setVouchers(vouRes?.data?.items || vouRes?.data || []);
                                    await fetchTicketDetails();
                                } catch (e) {
                                    if (e?.errorFields) return;
                                    message.error(e?.response?.data?.message || 'Tạo phiếu chi thất bại');
                                }
                            }}
                            form={vouForm}
                            serviceTypes={serviceTypes}
                            serviceTypesLoading={serviceTypesLoading}
                            disabled={isClosed}
                        />
                        <InvoiceDetailModal
                            open={invoiceDetailOpen}
                            invoice={invoiceDetail}
                            onClose={() => {
                                setInvoiceDetailOpen(false);
                                setInvoiceDetail(null);
                            }}
                            canDelete={!isClosed}
                            onDelete={async (detailId) => {
                                try {
                                    setDetailDeleting(detailId);
                                    await deleteInvoiceDetail(detailId);
                                    // Reload invoice detail để kiểm tra còn detail nào không
                                    const res = await api.get(`/Invoice/by-invoice/${invoiceDetail.invoiceId}`);
                                    const updatedInvoice = res.data;

                                    // Nếu không còn detail nào thì xóa luôn invoice
                                    if (!updatedInvoice.details || updatedInvoice.details.length === 0) {
                                        await api.delete(`/Invoice/${invoiceDetail.invoiceId}`);
                                        message.success('Đã xóa chi tiết hóa đơn và hóa đơn');
                                        setInvoiceDetailOpen(false);
                                        setInvoiceDetail(null);
                                    } else {
                                        message.success('Đã xóa chi tiết hóa đơn');
                                        setInvoiceDetail(updatedInvoice);
                                    }

                                    // Reload invoice list
                                    const invRes = await api.get(`/Invoice/by-ticket/${id}`);
                                    setInvoices(invRes?.data?.items || invRes?.data || []);
                                } catch (e) {
                                    message.error(e?.response?.data?.message || e?.response?.data?.error || 'Xóa thất bại');
                                } finally {
                                    setDetailDeleting(null);
                                }
                            }}
                            detailDeleting={detailDeleting}
                        />
                        <VoucherDetailModal
                            open={voucherDetailOpen}
                            voucher={voucherDetail}
                            onClose={() => {
                                setVoucherDetailOpen(false);
                                setVoucherDetail(null);
                            }}
                            canDelete={!isClosed}
                            onDelete={async (itemId) => {
                                try {
                                    setDetailDeleting(itemId);
                                    await api.delete(`/VoucherItem/${itemId}`);
                                    // Reload voucher detail để kiểm tra còn item nào không
                                    const res = await api.get(`/Voucher/${voucherDetail.voucherId}`);
                                    const updatedVoucher = res.data;

                                    // Nếu không còn item nào thì xóa luôn voucher
                                    if (!updatedVoucher.items || updatedVoucher.items.length === 0) {
                                        await api.delete(`/Voucher/${voucherDetail.voucherId}`);
                                        message.success('Đã xóa chi tiết phiếu chi và phiếu chi');
                                        setVoucherDetailOpen(false);
                                        setVoucherDetail(null);
                                    } else {
                                        message.success('Đã xóa chi tiết phiếu chi');
                                        setVoucherDetail(updatedVoucher);
                                    }

                                    // Reload voucher list
                                    const vouRes = await api.get(`/Voucher/by-ticket/${id}`);
                                    setVouchers(vouRes?.data?.items || vouRes?.data || []);
                                } catch (e) {
                                    message.error(e?.response?.data?.message || e?.response?.data?.error || 'Xóa thất bại');
                                } finally {
                                    setDetailDeleting(null);
                                }
                            }}
                            detailDeleting={detailDeleting}
                        />
                    </div>
                </Col>
            </Row>
        </div>
    );
}
