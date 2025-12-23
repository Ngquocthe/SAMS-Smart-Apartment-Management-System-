import React, { useEffect, useMemo, useState } from "react";
import {
  getInvoiceDetails,
  createInvoiceDetail,
  updateInvoiceDetail,
  deleteInvoiceDetail,
} from "../../../features/accountant/invoicedetailsApi";
import { listServiceType } from "../../../features/accountant/servicetypesApi";
import ticketsApi from "../../../features/tickets/ticketsApi";
import Toast from "../../../components/Toast";

const money = new Intl.NumberFormat("vi-VN", {
  style: "currency",
  currency: "VND",
  maximumFractionDigits: 0,
});

const initialForm = {
  serviceTypeId: "",
  quantity: "",
  vatRate: "",
  description: "",
  ticketId: "",
};

export default function InvoiceLineItems({ invoiceId, onDetailsChanged, canEdit = false }) {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [toast, setToast] = useState({ show: false, message: "", type: "success" });

  const [modalOpen, setModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState("create");
  const [activeDetail, setActiveDetail] = useState(null);
  const [form, setForm] = useState(() => ({ ...initialForm }));
  const [formErrors, setFormErrors] = useState({});
  const [modalError, setModalError] = useState("");
  const [submitLoading, setSubmitLoading] = useState(false);

  const [services, setServices] = useState([]);
  const [servicesLoading, setServicesLoading] = useState(false);
  const [servicesError, setServicesError] = useState("");

  const [deletingId, setDeletingId] = useState(null);

  // Thêm state lưu ticket options
  const [ticketOptions, setTicketOptions] = useState([]);
  const [ticketLoading, setTicketLoading] = useState(false);
  const [ticketError, setTicketError] = useState("");

  useEffect(() => {
    if (!invoiceId) return;
    let cancelled = false;

    const run = async () => {
      setLoading(true);
      setError("");
      try {
        const res = await getInvoiceDetails(invoiceId);
        if (!cancelled) {
          setItems(Array.isArray(res) ? res : []);
        }
      } catch (err) {
        if (!cancelled) {
          setItems([]);
          setError(
            err?.response?.data?.error ||
              err?.response?.data?.message ||
              err?.message ||
              "Không thể tải chi tiết hoá đơn"
          );
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    run();
    return () => {
      cancelled = true;
    };
  }, [invoiceId]);

  useEffect(() => {
    let cancelled = false;
    const loadServices = async () => {
      setServicesLoading(true);
      setServicesError("");
      try {
        const res = await listServiceType({ page: 1, pageSize: 200, includeInactive: false });
        if (!cancelled) {
          const raw = Array.isArray(res?.items) ? res.items : Array.isArray(res) ? res : [];
          setServices(raw);
        }
      } catch (err) {
        if (!cancelled) {
          setServices([]);
          setServicesError(
            err?.response?.data?.error ||
              err?.response?.data?.message ||
              err?.message ||
              "Không thể tải dịch vụ"
          );
        }
      } finally {
        if (!cancelled) {
          setServicesLoading(false);
        }
      }
    };

    loadServices();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!modalOpen) return;
    setTicketLoading(true);
    setTicketError("");
    ticketsApi.search({ pageSize: 100 }).then(res => {
      const arr = Array.isArray(res?.items) ? res.items : [];
      setTicketOptions(arr.map(t => ({ value: t.ticketId, label: t.subject })));
    }).catch(err => {
      setTicketOptions([]);
      setTicketError("Không tải được danh sách yêu cầu");
    }).finally(() => setTicketLoading(false));
  }, [modalOpen]);

  const serviceOptions = useMemo(() => {
    if (!Array.isArray(services)) return [];
    return services
      .filter((svc) => svc && (svc.serviceTypeId || svc.serviceId))
      .map((svc) => {
        const value = svc.serviceTypeId || svc.serviceId;
        return {
          value,
          label: svc.name,
          code: svc.code,
          unit: svc.unit,
        };
      });
  }, [services]);

  const columnCount = canEdit ? 6 : 5;

  const refreshDetails = async () => {
    try {
      setLoading(true);
      const res = await getInvoiceDetails(invoiceId);
      setItems(Array.isArray(res) ? res : []);
    } catch (err) {
      setItems([]);
      setError(
        err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          "Không thể tải chi tiết hoá đơn"
      );
    } finally {
      setLoading(false);
    }
  };

  const showToast = (message, type = "success") => {
    setToast({ show: true, message, type });
  };

  const closeModal = () => {
    setModalOpen(false);
    setModalMode("create");
    setActiveDetail(null);
    setForm({ ...initialForm });
    setFormErrors({});
    setModalError("");
  };

  const openCreateModal = () => {
    if (!canEdit) return;
    setModalMode("create");
    setActiveDetail(null);
    setForm({ ...initialForm });
    setFormErrors({});
    setModalError("");
    setModalOpen(true);
  };

  const openEditModal = (detail) => {
    if (!canEdit) return;
    setModalMode("edit");
    setActiveDetail(detail);
    setForm({
      serviceTypeId: detail.serviceTypeId || detail.serviceId || "",
      quantity:
        detail.quantity !== undefined && detail.quantity !== null
          ? String(detail.quantity)
          : "",
      vatRate:
        detail.vatRate !== undefined && detail.vatRate !== null
          ? String(detail.vatRate)
          : "",
      description: detail.description || "",
      ticketId: detail.ticketId || "",
    });
    setFormErrors({});
    setModalError("");
    setModalOpen(true);
  };

  const handleFormChange = (event) => {
    const { name, value } = event.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const validateForm = () => {
    const errs = {};
    if (!form.serviceTypeId) errs.serviceTypeId = "Vui lòng chọn dịch vụ";
    const quantityNumber = Number(form.quantity);
    if (!form.quantity || Number.isNaN(quantityNumber) || quantityNumber <= 0) errs.quantity = "Số lượng phải lớn hơn 0";
    if (form.vatRate !== "") {
      const vatNumber = Number(form.vatRate);
      if (Number.isNaN(vatNumber) || vatNumber < 0 || vatNumber > 100) errs.vatRate = "VAT phải nằm trong khoảng 0-100%";
    }
    if (form.description && form.description.length > 255) errs.description = "Mô tả không vượt quá 255 ký tự";
    setFormErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const handleFormSubmit = async (event) => {
    event.preventDefault();
    if (!invoiceId || !canEdit) return;
    if (!validateForm()) return;

    setSubmitLoading(true);
    setModalError("");
    try {
      const quantity = Number(form.quantity);
      const vatRate = form.vatRate === "" || form.vatRate == null ? 0 : Number(form.vatRate);
      const description = form.description?.trim();
      const ticketId = form.ticketId?.trim();

      if (modalMode === "create") {
        const payload = {
          invoiceId,
          serviceId: form.serviceTypeId,
          quantity,
        };
        if (description) payload.description = description;
        payload.vatRate = vatRate;
        if (ticketId) payload.ticketId = ticketId;

        await createInvoiceDetail(payload);
        showToast("Đã thêm dòng dịch vụ.", "success");
      } else if (activeDetail) {
        const payload = {};
        if (form.serviceTypeId) {
          payload.serviceId = form.serviceTypeId;
        }
        if (description !== undefined) payload.description = description || null;
        if (!Number.isNaN(quantity)) payload.quantity = quantity;
        payload.vatRate = vatRate;
        if (ticketId) {
          payload.ticketId = ticketId;
        } else if (activeDetail.ticketId) {
          payload.ticketId = null;
        }

        await updateInvoiceDetail(activeDetail.invoiceDetailId, payload);
        showToast("Đã cập nhật dòng dịch vụ.", "success");
      }

      await refreshDetails();
      onDetailsChanged?.();
      closeModal();
    } catch (err) {
      const errorMessage =
        err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          "Không thể lưu dòng dịch vụ"
      ;
      setModalError(errorMessage);
      showToast(errorMessage, "error");
    } finally {
      setSubmitLoading(false);
    }
  };

  const handleDelete = async (detail) => {
    if (!canEdit) return;
    const confirmed = window.confirm(
      `Gỡ "${detail.serviceName || detail.serviceCode || "dòng dịch vụ"}" khỏi hoá đơn?`
    );
    if (!confirmed) return;

    setDeletingId(detail.invoiceDetailId);
    try {
      await deleteInvoiceDetail(detail.invoiceDetailId);
      showToast("Đã xoá dòng dịch vụ.", "success");
      await refreshDetails();
      onDetailsChanged?.();
    } catch (err) {
      const errorMessage =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        "Không thể xoá dòng dịch vụ";
      showToast(errorMessage, "error");
    } finally {
      setDeletingId(null);
    }
  };

  return (
    <>
      <Toast
        show={toast.show}
        message={toast.message}
        type={toast.type}
        onClose={() => setToast((prev) => ({ ...prev, show: false }))}
      />
      <section className="rounded-3xl border border-slate-200 bg-white shadow-lg shadow-slate-200/30 overflow-hidden">
      <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">
            Dòng dịch vụ
          </p>
          <h2 className="text-lg font-semibold text-slate-700">
            {loading ? "Đang tải chi tiết..." : `${items.length} dịch vụ`}
          </h2>
        </div>
        <div className="flex items-center gap-3">
          <div className="text-xs text-slate-400">
            Tổng tiền sẽ tự đồng bộ với hoá đơn
          </div>
          {canEdit && (
            <button
              onClick={openCreateModal}
              className="inline-flex items-center gap-2 rounded-full bg-indigo-600 px-4 py-2 text-xs font-semibold text-white shadow hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
            >
              + Thêm dòng dịch vụ
            </button>
          )}
        </div>
      </div>

      {error && (
        <div className="border-b border-rose-200 bg-rose-50 px-6 py-3 text-sm text-rose-700">
          {error}
        </div>
      )}

      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-slate-100 text-sm">
          <thead className="bg-slate-50/70">
            <tr>
              <th className="px-6 py-3 text-left font-semibold text-slate-600">Dịch vụ</th>
              <th className="px-6 py-3 text-left font-semibold text-slate-600">Sử dụng</th>
              <th className="px-6 py-3 text-left font-semibold text-slate-600">Đơn giá</th>
              <th className="px-6 py-3 text-left font-semibold text-slate-600">Thành tiền</th>
              <th className="px-6 py-3 text-left font-semibold text-slate-600">VAT</th>
              {canEdit && <th className="px-6 py-3 text-right font-semibold text-slate-600">Thao tác</th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 bg-white">
            {loading ? (
              <tr>
                  <td colSpan={columnCount} className="px-6 py-10 text-center text-slate-500">
                  Đang tải dữ liệu...
                </td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={columnCount} className="px-6 py-12 text-center text-slate-400">
                  Chưa có chi tiết. Hãy thêm nước, điện hoặc các dịch vụ sau khi lưu hoá đơn.
                </td>
              </tr>
            ) : (
              items.map((item) => (
                <tr key={item.invoiceDetailId} className="hover:bg-slate-50/70 transition">
                  <td className="px-6 py-4 align-top">
                    <div className="font-semibold text-slate-700">
                      {item.serviceName || "Dịch vụ chưa có tên"}
                    </div>
                    <div className="text-xs text-slate-400">{item.description || item.serviceCode}</div>
                  </td>
                  <td className="px-6 py-4 align-top text-sm text-slate-600">
                    <div>{item.quantity ?? 0}</div>
                    <div className="text-xs text-slate-400">{item.serviceUnit || "đơn vị"}</div>
                  </td>
                  <td className="px-6 py-4 align-top text-sm text-slate-600">
                    {money.format(item.unitPrice ?? 0)}
                  </td>
                  <td className="px-6 py-4 align-top text-sm font-semibold text-slate-700">
                    {money.format(item.amount ?? 0)}
                  </td>
                  <td className="px-6 py-4 align-top text-sm text-slate-600">
                    {item.vatRate != null ? `${item.vatRate}%` : "-"}
                    <div className="text-xs text-slate-400">
                      {money.format(item.vatAmount ?? 0)}
                    </div>
                  </td>
                  {canEdit && (
                    <td className="px-6 py-4 align-top text-right">
                      <div className="flex justify-end gap-2">
                        <button
                          onClick={() => openEditModal(item)}
                          className="inline-flex items-center rounded-full border border-slate-300 px-3 py-1.5 text-xs font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
                        >
                          Sửa
                        </button>
                        <button
                          onClick={() => handleDelete(item)}
                          disabled={deletingId === item.invoiceDetailId}
                          className={`inline-flex items-center rounded-full border px-3 py-1.5 text-xs font-semibold transition focus:outline-none focus:ring-2 focus:ring-offset-1 ${
                            deletingId === item.invoiceDetailId
                              ? "border-rose-200 bg-rose-100 text-rose-400 cursor-not-allowed"
                              : "border-rose-200 text-rose-600 hover:bg-rose-50"
                          }`}
                        >
                          {deletingId === item.invoiceDetailId ? "Đang xoá..." : "Xoá"}
                        </button>
                      </div>
                    </td>
                  )}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur">
          <div className="w-full max-w-3xl rounded-3xl bg-white shadow-2xl">
            <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
              <div>
                <h3 className="text-xl font-semibold text-slate-800">
                  {modalMode === "create" ? "Thêm dòng dịch vụ" : "Chỉnh sửa dòng dịch vụ"}
                </h3>
                <p className="text-xs text-slate-500 mt-1">
                  Đơn giá sẽ được lấy theo bảng giá dịch vụ đang hoạt động mới nhất.
                </p>
              </div>
              <button
                onClick={closeModal}
                className="rounded-full border border-slate-200 w-8 h-8 flex items-center justify-center text-xl text-slate-500 hover:bg-slate-100 focus:outline-none"
                aria-label="Đóng"
              >
                ×
              </button>
            </div>

            {modalError && (
              <div className="border-b border-rose-200 bg-rose-50 px-6 py-3 text-sm text-rose-700">
                {modalError}
              </div>
            )}

            <div className="px-6 py-6 space-y-5">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                <div>
                  <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Dịch vụ *
                  </label>
                  <select
                    name="serviceTypeId"
                    value={form.serviceTypeId}
                    onChange={handleFormChange}
                    className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
                    disabled={servicesLoading}
                  >
                    <option value="">-- Chọn dịch vụ --</option>
                    {serviceOptions.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label} {option.code ? `(${option.code})` : ""}
                      </option>
                    ))}
                  </select>
                  {servicesError && (
                    <p className="mt-1 text-xs text-rose-500">{servicesError}</p>
                  )}
                  {servicesLoading && (
                    <p className="mt-1 text-xs text-slate-400">Đang tải dịch vụ...</p>
                  )}
                  {formErrors.serviceTypeId && (
                    <p className="mt-1 text-xs text-rose-500">{formErrors.serviceTypeId}</p>
                  )}
                </div>

                <div>
                  <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Số lượng *
                  </label>
                  <input
                    type="number"
                    min="0"
                    step="0.0001"
                    name="quantity"
                    value={form.quantity}
                    onChange={handleFormChange}
                    className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
                    placeholder="Nhập số lượng sử dụng"
                  />
                  {formErrors.quantity && (
                    <p className="mt-1 text-xs text-rose-500">{formErrors.quantity}</p>
                  )}
                </div>

                <div>
                  <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Thuế VAT %
                  </label>
                  <input
                    type="number"
                    min="0"
                    max="100"
                    step="0.01"
                    name="vatRate"
                    value={form.vatRate}
                    onChange={handleFormChange}
                    className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
                    placeholder="Để trống nếu không áp dụng"
                  />
                  {formErrors.vatRate && (
                    <p className="mt-1 text-xs text-rose-500">{formErrors.vatRate}</p>
                  )}
                </div>

                <div>
                  <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                    Yêu cầu (ticket)
                  </label>
                  <select
                    name="ticketId"
                    value={form.ticketId}
                    onChange={handleFormChange}
                    className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
                    disabled={ticketLoading}
                  >
                    <option value="">-- Chọn yêu cầu --</option>
                    {ticketOptions.map((option) => (
                      <option key={option.value} value={option.value}>{option.label}</option>
                    ))}
                  </select>
                  {ticketError && (
                    <p className="mt-1 text-xs text-rose-500">{ticketError}</p>
                  )}
                  {ticketLoading && <p className="mt-1 text-xs text-slate-400">Đang tải yêu cầu...</p>}
                </div>
              </div>

              <div>
                <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                  Mô tả
                </label>
                <textarea
                  name="description"
                  value={form.description}
                  onChange={handleFormChange}
                  rows={3}
                  className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200 resize-none"
                  placeholder="Tuỳ chọn: mô tả thêm cho khoản phí"
                  maxLength={255}
                />
                {formErrors.description && (
                  <p className="mt-1 text-xs text-rose-500">{formErrors.description}</p>
                )}
              </div>

              <div className="flex justify-end gap-3">
                <button
                  type="button"
                  onClick={closeModal}
                  className="inline-flex items-center rounded-full border border-slate-300 px-5 py-2 text-sm font-semibold text-slate-600 hover:border-slate-400"
                >
                  Huỷ
                </button>
                <button
                  type="button"
                  onClick={handleFormSubmit}
                  disabled={submitLoading}
                  className={`inline-flex items-center rounded-full px-5 py-2 text-sm font-semibold text-white shadow focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 ${
                    submitLoading
                      ? "bg-indigo-300 cursor-not-allowed"
                      : "bg-indigo-600 hover:bg-indigo-700"
                  }`}
                >
                  {submitLoading
                    ? modalMode === "create"
                      ? "Đang tạo..."
                      : "Đang lưu..."
                    : modalMode === "create"
                    ? "Thêm mới"
                    : "Lưu thay đổi"}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </section>
  </>
  );
}
