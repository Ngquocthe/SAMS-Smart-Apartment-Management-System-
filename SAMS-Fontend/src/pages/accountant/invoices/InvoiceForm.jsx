import React, { useEffect, useMemo, useState } from "react";
import api from "../../../lib/apiClient";

export const INVOICE_STATUS_OPTIONS = [
  { value: "DRAFT", label: "Nháp" },
  { value: "ISSUED", label: "Đã phát hành" },
  { value: "OVERDUE", label: "Quá hạn" },
  { value: "PAID", label: "Đã thanh toán" },
  { value: "CANCELLED", label: "Đã huỷ" },
];

const STATUS_THEME = {
  DRAFT: "bg-slate-200 text-slate-700",
  ISSUED: "bg-blue-100 text-blue-700",
  OVERDUE: "bg-amber-100 text-amber-800",
  PAID: "bg-emerald-100 text-emerald-700",
  CANCELLED: "bg-rose-100 text-rose-700",
};

const STATUS_LABELS = {
  DRAFT: "Nháp",
  ISSUED: "Đã phát hành",
  OVERDUE: "Quá hạn",
  PAID: "Đã thanh toán",
  CANCELLED: "Đã huỷ",
};

const guidRegex = /^[{(]?[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}[)}]?$/;

const moneyFormat = new Intl.NumberFormat("vi-VN", {
  style: "currency",
  currency: "VND",
  maximumFractionDigits: 0,
});

function StatusBadge({ value }) {
  if (!value) return null;
  const theme = STATUS_THEME[value] || "bg-slate-200 text-slate-700";
  return (
    <span className={`inline-flex items-center rounded-full px-4 py-1 text-xs font-semibold uppercase tracking-widest ${theme}`}>
      {STATUS_LABELS[value] || value}
    </span>
  );
}

export default function InvoiceForm({
  mode = "create",
  initialValues,
  summary,
  onSubmit,
  submitting = false,
  serverMsg = null,
  canEdit = true,
  lineItemsSection = null,
}) {
  const defaults = useMemo(
    () => ({
      invoiceNo: "",
      apartmentId: "",
      issueDate: "",
      dueDate: "",
      status: "DRAFT",
      note: "",
    }),
    []
  );

  const [form, setForm] = useState({ ...defaults, ...(initialValues || {}) });
  const [errors, setErrors] = useState({});
  const [apartments, setApartments] = useState([]);
  const [loadingApartments, setLoadingApartments] = useState(false);

  useEffect(() => {
    setForm({ ...defaults, ...(initialValues || {}) });
  }, [initialValues, defaults]);

  useEffect(() => {
    const fetchApartments = async () => {
      setLoadingApartments(true);
      try {
        const response = await api.get("/Apartment");
        const apartmentsData = Array.isArray(response.data) ? response.data : [];
        setApartments(apartmentsData);
      } catch (error) {
        console.error("Error fetching apartments:", error);
        setApartments([]);
      } finally {
        setLoadingApartments(false);
      }
    };

    fetchApartments();
  }, []);

  const handleChange = (field, value) => {
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  const handleInputChange = (event) => {
    const { name, value } = event.target;
    handleChange(name, value);
  };

  const validate = () => {
    const nextErrors = {};

    if (mode === "create") {
      if (!form.invoiceNo?.trim()) {
        nextErrors.invoiceNo = "Mã hoá đơn là bắt buộc";
      } else if (form.invoiceNo.trim().length < 3 || form.invoiceNo.trim().length > 64) {
        nextErrors.invoiceNo = "Mã hoá đơn phải từ 3 đến 64 ký tự";
      }
      if (!form.status) {
        nextErrors.status = "Vui lòng chọn trạng thái";
      }
    }

    if (!form.apartmentId?.trim()) {
      nextErrors.apartmentId = "Căn hộ là bắt buộc";
    }

    if (!form.issueDate) {
      nextErrors.issueDate = "Ngày phát hành là bắt buộc";
    }
    if (!form.dueDate) {
      nextErrors.dueDate = "Hạn thanh toán là bắt buộc";
    }
    if (form.issueDate && form.dueDate) {
      const issue = new Date(form.issueDate);
      const due = new Date(form.dueDate);
      if (issue.toString() === "Invalid Date" || due.toString() === "Invalid Date") {
        nextErrors.dueDate = "Ngày tháng không hợp lệ";
      } else if (due < issue) {
        nextErrors.dueDate = "Hạn thanh toán không được trước ngày phát hành";
      }
    }

    if (form.note && form.note.trim().length > 1000) {
      nextErrors.note = "Ghi chú không vượt quá 1000 ký tự";
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (mode === "update" && !canEdit) return;
    if (!validate()) return;

    const payloadBase = {
      apartmentId: form.apartmentId?.trim(),
      issueDate: form.issueDate,
      dueDate: form.dueDate,
    };

    if (form.note !== undefined) {
      const trimmed = form.note?.trim();
      payloadBase.note = trimmed || null;
    }

    const payload =
      mode === "create"
        ? {
            invoiceNo: form.invoiceNo.trim(),
            status: form.status,
            ...payloadBase,
          }
        : payloadBase;

    await onSubmit(payload, { raw: form, mode });
  };

  const displaySummary = summary || {
    subtotalAmount: 0,
    taxAmount: 0,
    totalAmount: 0,
    status: form.status,
  };

  const isLocked = mode === "update" && !canEdit;
  const canEditCoreFields = mode === "create" || !isLocked;
  const canEditDates = mode === "create" || !isLocked;
  const canEditNotes = mode === "create" || !isLocked;
  const errorText = "mt-1 text-xs text-rose-500";
  const helperText = "text-xs text-slate-500";
  const statusValue = displaySummary.status || form.status;

  const renderDateField = (name, labelText) => (
    <div>
      <p className="mb-1 text-xs font-semibold uppercase tracking-widest text-slate-500">
        {labelText}
      </p>
      {canEditDates ? (
        <>
          <input
            type="date"
            name={name}
            value={form[name] ?? ""}
            onChange={handleInputChange}
            className="w-full rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-700 shadow-sm focus:border-red-500 focus:outline-none focus:ring-2 focus:ring-red-100 disabled:bg-slate-50 disabled:text-slate-400"
            disabled={isLocked}
          />
          {errors[name] && <p className={errorText}>{errors[name]}</p>}
        </>
      ) : (
        <p className="text-lg font-semibold text-slate-700">
          {form[name] || "—"}
        </p>
      )}
    </div>
  );

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-xl shadow-red-100/40">
        <div className="border-b-4 border-red-600 bg-gradient-to-r from-red-50 to-transparent p-8">
          <div className="flex flex-col gap-8 md:flex-row md:items-start md:justify-between">
            <div className="space-y-4">
              <h1 className="text-4xl font-bold uppercase tracking-[0.6em] text-red-700">
                Hoá đơn
              </h1>
              <div>
                <p className="mb-2 text-xs font-semibold uppercase tracking-widest text-slate-500">
                  Số hoá đơn{mode === "create" ? " *" : ""}
                </p>
                {mode === "create" ? (
                  <>
                    <input
                      name="invoiceNo"
                      value={form.invoiceNo}
                      onChange={handleInputChange}
                      className="w-full rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-700 shadow-sm focus:border-red-500 focus:outline-none focus:ring-2 focus:ring-red-100"
                      placeholder="INV-2025-001"
                      autoComplete="off"
                    />
                    {errors.invoiceNo && <p className={errorText}>{errors.invoiceNo}</p>}
                    <p className={`${helperText} mt-2`}>
                      Gợi ý định dạng: INV-NĂM-SỐ
                    </p>
                  </>
                ) : (
                  <p className="text-lg font-semibold uppercase tracking-[0.3em] text-slate-700">
                    {form.invoiceNo || "—"}
                  </p>
                )}
              </div>
              <div className="space-y-1 text-sm text-slate-500">
                <p className="font-semibold text-slate-700">Tên công ty</p>
                <p>123 Đường ABC</p>
                <p>Quận/Huyện, Thành phố</p>
                <p>+84 000 000 000</p>
                <p className="text-red-600">lienhe@company.com</p>
              </div>
            </div>
            <div className="space-y-4 text-right">
              <div className="flex h-24 w-24 items-center justify-center rounded-full border-2 border-red-600 text-sm font-semibold uppercase tracking-widest text-red-600 md:ml-auto">
                LOGO
              </div>
              <StatusBadge value={statusValue} />
              {mode === "create" && (
                <p className="text-[11px] uppercase tracking-[0.3em] text-slate-400">
                  Khởi tạo ở trạng thái nháp
                </p>
              )}
            </div>
          </div>

          <div className="mt-8 grid grid-cols-1 gap-6 sm:grid-cols-2">
            {renderDateField("issueDate", "Ngày phát hành *")}
            {renderDateField("dueDate", "Hạn thanh toán *")}
          </div>
        </div>

        {serverMsg && (
          <div
            className={`mx-8 mt-6 rounded-2xl border px-4 py-3 text-sm font-medium ${
              serverMsg.type === "success"
                ? "bg-emerald-50 border-emerald-200 text-emerald-700"
                : serverMsg.type === "error"
                ? "bg-rose-50 border-rose-200 text-rose-700"
                : "bg-sky-50 border-sky-200 text-sky-700"
            }`}
          >
            {serverMsg.text}
          </div>
        )}

        <div className="mt-8 grid gap-8 border-b border-slate-200 p-8 md:grid-cols-2">
          <div>
            <p className="mb-3 text-xs font-semibold uppercase tracking-widest text-slate-500">
              Đơn vị phát hành
            </p>
            <div className="space-y-1 text-sm text-slate-500">
              <p className="font-semibold text-slate-700">Tên công ty</p>
              <p>123 Đường ABC</p>
              <p>Quận/Huyện, Thành phố</p>
              <p>+84 000 000 000</p>
              <p className="text-red-600">lienhe@company.com</p>
            </div>
          </div>
          <div>
            <p className="mb-3 text-xs font-semibold uppercase tracking-widest text-slate-500">
              Thông tin thanh toán
            </p>
            <div className="space-y-3">
              <p className="text-sm font-semibold text-slate-700">Tên cư dân</p>
              <div>
                <p className="mb-2 text-xs font-semibold uppercase tracking-widest text-slate-500">
                  Căn hộ *
                </p>
                {canEditCoreFields ? (
                  <>
                    <select
                      name="apartmentId"
                      value={form.apartmentId}
                      onChange={handleInputChange}
                      disabled={loadingApartments}
                      className="w-full rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-700 shadow-sm focus:border-red-500 focus:outline-none focus:ring-2 focus:ring-red-100 disabled:bg-slate-50 disabled:text-slate-400"
                    >
                      <option value="">-- Chọn căn hộ --</option>
                      {apartments.map((apartment) => (
                        <option key={apartment.apartmentId} value={apartment.apartmentId}>
                          {apartment.floorNumber ? `Tầng ${apartment.floorNumber} - ` : ""}
                          {apartment.number || apartment.apartmentId}
                        </option>
                      ))}
                    </select>
                    {errors.apartmentId && <p className={errorText}>{errors.apartmentId}</p>}
                    {loadingApartments && (
                      <p className="mt-1 text-xs text-slate-500">Đang tải danh sách căn hộ...</p>
                    )}
                  </>
                ) : (
                  <p className="rounded-lg bg-slate-50 p-3 font-medium text-sm text-slate-600">
                    {(() => {
                      const apartment = apartments.find((apt) => apt.apartmentId === form.apartmentId);
                      return apartment
                        ? `${apartment.floorNumber ? `Tầng ${apartment.floorNumber} - ` : ""}${apartment.number || form.apartmentId}`
                        : form.apartmentId || "—";
                    })()}
                  </p>
                )}
              </div>
              <p className="text-xs text-slate-500">
                Chọn căn hộ để liên kết với hoá đơn.
              </p>
            </div>
          </div>
        </div>

        <div className="border-b border-slate-200 p-8">
          <h2 className="text-sm font-semibold uppercase tracking-widest text-slate-600">
            Chi tiết dịch vụ
          </h2>
          <div className="mt-4">
            {lineItemsSection ? (
              <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
                {lineItemsSection}
              </div>
            ) : (
              <div className="rounded-xl border border-dashed border-slate-300 bg-slate-50 p-6 text-center text-sm text-slate-500">
                Chưa có dòng dịch vụ. Vui lòng thêm dịch vụ vào hoá đơn.
              </div>
            )}
          </div>
        </div>

        <div className="grid gap-8 border-b border-slate-200 p-8 md:grid-cols-3">
          <div className="md:col-span-2">
            <p className="mb-3 text-xs font-semibold uppercase tracking-widest text-slate-500">
              Ghi chú
            </p>
            {canEditNotes ? (
              <>
                <textarea
                  name="note"
                  value={form.note ?? ""}
                  onChange={handleInputChange}
                  rows={4}
                  className="w-full rounded-lg border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 shadow-sm focus:border-red-500 focus:outline-none focus:ring-2 focus:ring-red-100 resize-none"
                  placeholder="Vui lòng thanh toán trong vòng 30 ngày và ghi rõ mã hoá đơn."
                />
                {errors.note && <p className={errorText}>{errors.note}</p>}
                <p className="mt-2 text-xs text-slate-500">
                  Hiển thị trên bản in của hoá đơn (tối đa 1000 ký tự).
                </p>
              </>
            ) : (
              <div className="rounded-lg border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
                {form.note?.trim() ? form.note : "Không có ghi chú nào."}
              </div>
            )}
          </div>
          <div className="rounded-lg border border-red-100 bg-red-50/70 p-6">
            <h3 className="mb-4 text-sm font-semibold uppercase tracking-widest text-slate-600">
              Tổng hợp
            </h3>
            <div className="space-y-3 text-sm text-slate-600">
              <div className="flex items-center justify-between">
                <span>Tạm tính</span>
                <span className="font-semibold text-slate-800">
                  {moneyFormat.format(displaySummary.subtotalAmount ?? 0)}
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span>Thuế</span>
                <span className="font-semibold text-slate-800">
                  {moneyFormat.format(displaySummary.taxAmount ?? 0)}
                </span>
              </div>
              <div className="border-t border-red-200 pt-3">
                <div className="flex items-center justify-between">
                  <span className="font-semibold text-slate-700">Tổng phải thu</span>
                  <span className="text-xl font-bold text-red-700">
                    {moneyFormat.format(displaySummary.totalAmount ?? 0)}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-slate-50 p-8">
          <div className="grid gap-8 text-sm text-slate-600 md:grid-cols-3">
            <div>
              <p className="mb-2 font-semibold text-slate-700">Phương thức thanh toán</p>
              <p>Chuyển khoản ngân hàng</p>
              <p>Tài khoản: XXXX-XXXX-XXXX</p>
            </div>
            <div>
              <p className="mb-2 font-semibold text-slate-700">Liên hệ</p>
              <p>+84 000 000 000</p>
              <p>lienhe@company.com</p>
            </div>
            <div>
              <p className="mb-2 font-semibold text-slate-700">Điều khoản</p>
              <p>Thanh toán trong 30 ngày</p>
              <p>Phí phạt áp dụng sau hạn</p>
            </div>
          </div>
          <div className="mt-6 border-t border-slate-200 pt-6 text-center text-xs text-slate-500">
            Cảm ơn đã sử dụng dịch vụ. Hoá đơn có hiệu lực trong 30 ngày kể từ ngày phát hành.
          </div>
        </div>
      </div>

      <div className="flex justify-end">
        <button
          type="submit"
          disabled={submitting || isLocked}
          className={`inline-flex items-center gap-2 rounded-full px-6 py-2.5 text-sm font-semibold text-white shadow-lg shadow-red-400/30 transition focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-600 ${
            submitting
              ? "bg-red-300 cursor-not-allowed"
              : isLocked
              ? "bg-slate-300 cursor-not-allowed"
              : "bg-red-600 hover:bg-red-700"
          }`}
        >
          {submitting
            ? mode === "create"
              ? "Đang tạo..."
              : "Đang lưu..."
            : mode === "create"
            ? "Tạo hoá đơn"
            : "Lưu thay đổi"}
        </button>
      </div>
    </form>
  );
}

export { StatusBadge };
