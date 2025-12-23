import React, { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import { getInvoiceById, updateInvoice, updateInvoiceStatus } from "../../../features/accountant/invoiceApi";
import InvoiceForm, { INVOICE_STATUS_OPTIONS } from "./InvoiceForm";
import Toast from "../../../components/Toast";
import InvoiceLineItems from "./InvoiceLineItems";

const toIsoDate = (value) => {
  if (!value) return "";
  if (value instanceof Date && !Number.isNaN(value.getTime())) {
    return value.toISOString().split("T")[0];
  }
  if (typeof value === "string") {
    return value.split("T")[0];
  }
  try {
    const date = new Date(value);
    if (!Number.isNaN(date.getTime())) {
      return date.toISOString().split("T")[0];
    }
  } catch (err) {
    return "";
  }
  return "";
};

export default function UpdateInvoicePage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [invoice, setInvoice] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [serverMsg, setServerMsg] = useState(null);
  const [statusForm, setStatusForm] = useState({ status: "", note: "" });
  const [statusSubmitting, setStatusSubmitting] = useState(false);
  const [statusMsg, setStatusMsg] = useState(null);
  const [toast, setToast] = useState({ show: false, message: "", type: "success" });

  const showToast = (message, type = "success") => {
    setToast({ show: true, message, type });
  };

  const load = async () => {
    if (!id) {
      setError("Thiếu mã hoá đơn");
      setLoading(false);
      return;
    }
    setLoading(true);
    setError("");
    try {
      const res = await getInvoiceById(id);
      setInvoice(res);
    } catch (err) {
      setError(
        err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          "Không thể tải hoá đơn"
      );
    } finally {
      setLoading(false);
    }
  };

  const refreshInvoice = async () => {
    if (!id) return;
    try {
      const res = await getInvoiceById(id);
      setInvoice(res);
    } catch (err) {
      console.error("Không thể tải lại hoá đơn", err);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  useEffect(() => {
    if (invoice) {
      setStatusForm({ status: invoice.status, note: "" });
      setStatusMsg(null);
      // Redirect non-DRAFT to view-only page
      if (invoice.status && invoice.status !== "DRAFT") {
        navigate(ROUTER_PAGE.ACCOUNTANT.INVOICE_VIEW.replace(":id", invoice.invoiceId));
      }
    }
  }, [invoice]);

  const initialValues = useMemo(() => {
    if (!invoice) return null;
    return {
      invoiceNo: invoice.invoiceNo,
      apartmentId: invoice.apartmentId,
      issueDate: toIsoDate(invoice.issueDate),
      dueDate: toIsoDate(invoice.dueDate),
      status: invoice.status,
      note: invoice.note ?? "",
    };
  }, [invoice]);

  const summary = useMemo(() => {
    if (!invoice) return null;
    return {
      subtotalAmount: invoice.subtotalAmount,
      taxAmount: invoice.taxAmount,
      totalAmount: invoice.totalAmount,
      status: invoice.status,
    };
  }, [invoice]);

  const canEditInvoice = invoice?.status === "DRAFT";

  const handleSubmit = async (payload) => {
    if (!id) return;
    setSubmitting(true);
    setServerMsg(null);
    try {
      const updated = await updateInvoice(id, payload);
      setInvoice(updated);
      setServerMsg({ type: "success", text: "Cập nhật hoá đơn thành công." });
      showToast("Cập nhật hoá đơn thành công.", "success");
    } catch (err) {
      setServerMsg({
        type: "error",
        text:
          err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          "Không thể cập nhật hoá đơn",
      });
      const errorText =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        "Không thể cập nhật hoá đơn";
      showToast(errorText, "error");
    } finally {
      setSubmitting(false);
    }
  };

  const handleStatusFieldChange = (event) => {
    const { name, value } = event.target;
    setStatusForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleStatusSubmit = async (event) => {
    event.preventDefault();
    if (!id || !invoice) return;
    if (!statusForm.status) {
      setStatusMsg({ type: "error", text: "Vui lòng chọn trạng thái." });
      return;
    }
    if (statusForm.status === invoice.status) {
      setStatusMsg({ type: "info", text: "Hoá đơn đã ở trạng thái này." });
      return;
    }

    setStatusSubmitting(true);
    setStatusMsg(null);
    try {
      const payload = { status: statusForm.status };
      const note = statusForm.note?.trim();
      if (note) {
        payload.note = note;
      }

      const updated = await updateInvoiceStatus(id, payload);
      setInvoice(updated);
      const successText = `Đã chuyển trạng thái sang ${statusForm.status}.`;
      setStatusMsg({
        type: "success",
        text: successText,
      });
      showToast(successText, "success");
    } catch (err) {
      const errorText =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        "Không thể cập nhật trạng thái";
      setStatusMsg({
        type: "error",
        text: errorText,
      });
      showToast(errorText, "error");
    } finally {
      setStatusSubmitting(false);
    }
  };

  const handleStatusChange = async (newStatus) => {
    if (!id || !invoice) return;
    if (newStatus === invoice.status) {
      setStatusMsg({ type: "info", text: "Hoá đơn đã ở trạng thái này." });
      return;
    }

    setStatusSubmitting(true);
    setStatusMsg(null);
    try {
      // Tự động tạo note với format "DRAFT → NEW_STATUS + time"
      const now = new Date();
      const timeString = now.toLocaleString("vi-VN", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
      });
      const statusLabel = newStatus === "ISSUED" ? "ISSUED" : "CANCELLED";
      const note = `DRAFT → ${statusLabel} ${timeString}`;

      const payload = { 
        status: newStatus,
        note: note
      };

      const updated = await updateInvoiceStatus(id, payload);
      setInvoice(updated);
      const successText = `Đã chuyển trạng thái sang ${newStatus}.`;
      setStatusMsg({
        type: "success",
        text: successText,
      });
      showToast(successText, "success");
      // Reset note after successful status change
      setStatusForm((prev) => ({ ...prev, note: "" }));
    } catch (err) {
      const errorText =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        "Không thể cập nhật trạng thái";
      setStatusMsg({
        type: "error",
        text: errorText,
      });
      showToast(errorText, "error");
    } finally {
      setStatusSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="max-w-5xl mx-auto">
        <button
          onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.INVOICES)}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
        >
          ← Quay lại danh sách hoá đơn
        </button>
        <div className="mt-10 rounded-3xl border border-slate-200 bg-white px-6 py-12 text-center text-slate-500 shadow">
          Đang tải hoá đơn...
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="max-w-3xl mx-auto space-y-4">
        <button
          onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.INVOICES)}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
        >
          ← Quay lại danh sách hoá đơn
        </button>
        <div className="rounded-3xl border border-rose-200 bg-rose-50 px-6 py-10 text-center text-rose-700 shadow">
          {error}
        </div>
      </div>
    );
  }

  if (!invoice || !initialValues) {
    return null;
  }

  const lineItemsSection = (
    <InvoiceLineItems
      invoiceId={invoice.invoiceId}
      onDetailsChanged={refreshInvoice}
      canEdit={canEditInvoice}
    />
  );

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <button
          onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.INVOICES)}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
        >
          ← Quay lại danh sách hoá đơn
        </button>
        <div className="flex items-center gap-2">
          {invoice?.status === "DRAFT" && (
            <>
              <button
                type="button"
                onClick={() => handleStatusChange("ISSUED")}
                disabled={statusSubmitting}
                className={`inline-flex items-center justify-center gap-2 rounded-full px-4 py-2 text-sm font-semibold text-white shadow transition focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 ${
                  statusSubmitting
                    ? "bg-indigo-300 cursor-not-allowed"
                    : "bg-indigo-600 hover:bg-indigo-700"
                }`}
              >
                {statusSubmitting ? "Đang cập nhật..." : "Xuất Hoá Đơn"}
              </button>
              <button
                type="button"
                onClick={() => handleStatusChange("CANCELLED")}
                disabled={statusSubmitting}
                className={`inline-flex items-center justify-center gap-2 rounded-full px-4 py-2 text-sm font-semibold text-white shadow transition focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 ${
                  statusSubmitting
                    ? "bg-slate-300 cursor-not-allowed"
                    : "bg-rose-600 hover:bg-rose-700"
                }`}
              >
                {statusSubmitting ? "Đang cập nhật..." : "Huỷ Hoá Đơn"}
              </button>
            </>
          )}
          <button
            onClick={() => window.print()}
            className="inline-flex items-center gap-2 rounded-full bg-slate-900 px-4 py-2 text-sm font-semibold text-white shadow hover:bg-black"
          >
            In hoá đơn
          </button>
        </div>
      </div>

      <Toast
        show={toast.show}
        message={toast.message}
        type={toast.type}
        onClose={() => setToast((prev) => ({ ...prev, show: false }))}
      />

      {invoice?.status === "DRAFT" && statusMsg && (
        <div
          className={`rounded-3xl border px-6 py-3 text-sm font-medium ${
            statusMsg.type === "success"
              ? "border-emerald-200 bg-emerald-50 text-emerald-700"
              : statusMsg.type === "error"
              ? "border-rose-200 bg-rose-50 text-rose-700"
              : "border-sky-200 bg-sky-50 text-sky-700"
          }`}
        >
          {statusMsg.text}
        </div>
      )}

      {invoice?.status !== "DRAFT" && (
        <section className="rounded-3xl border border-slate-200 bg-white shadow-lg shadow-slate-200/40 overflow-hidden">

          {statusMsg && (
            <div
              className={`px-6 py-3 text-sm font-medium border-b ${
                statusMsg.type === "success"
                  ? "bg-emerald-50 border-emerald-200 text-emerald-700"
                  : statusMsg.type === "error"
                  ? "bg-rose-50 border-rose-200 text-rose-700"
                  : "bg-sky-50 border-sky-200 text-sky-700"
              }`}
            >
              {statusMsg.text}
            </div>
          )}

          <form onSubmit={handleStatusSubmit} className="px-6 py-5 grid grid-cols-1 lg:grid-cols-[minmax(0,240px)_1fr_auto] gap-4 items-start">
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Trạng thái mới
              </label>
              <select
                name="status"
                value={statusForm.status}
                onChange={handleStatusFieldChange}
                className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
              >
                {INVOICE_STATUS_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Ghi chú chuyển trạng thái
              </label>
              <textarea
                name="note"
                value={statusForm.note}
                onChange={handleStatusFieldChange}
                rows={3}
                className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200 resize-none"
                placeholder="Tuỳ chọn: mô tả lý do đổi trạng thái (tối đa 500 ký tự)."
                maxLength={500}
              />
            </div>
            <div className="flex items-end">
              <button
                type="submit"
                disabled={statusSubmitting || statusForm.status === invoice.status}
                className={`inline-flex items-center justify-center rounded-full px-5 py-2.5 text-sm font-semibold text-white shadow transition focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 ${
                  statusSubmitting
                    ? "bg-indigo-300 cursor-not-allowed"
                    : statusForm.status === invoice.status
                    ? "bg-slate-300 cursor-not-allowed"
                    : "bg-indigo-600 hover:bg-indigo-700"
                }`}
              >
                {statusSubmitting ? "Đang cập nhật..." : "Cập nhật trạng thái"}
              </button>
            </div>
          </form>
        </section>
      )}

      <InvoiceForm
        mode="update"
        initialValues={initialValues}
        summary={summary}
        onSubmit={handleSubmit}
        submitting={submitting}
        serverMsg={serverMsg}
        canEdit={canEditInvoice}
        lineItemsSection={lineItemsSection}
      />
    </div>
  );
}
