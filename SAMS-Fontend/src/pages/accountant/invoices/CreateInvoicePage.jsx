import React, { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import { createInvoice } from "../../../features/accountant/invoiceApi";
import InvoiceForm from "./InvoiceForm";
import Toast from "../../../components/Toast";

const isoDate = (date) => date.toISOString().split("T")[0];

export default function CreateInvoicePage() {
  const navigate = useNavigate();
  const [submitting, setSubmitting] = useState(false);
  const [serverMsg, setServerMsg] = useState(null);
  const [createdInvoice, setCreatedInvoice] = useState(null);
  const [toast, setToast] = useState({ show: false, message: "", type: "success" });

  const showToast = (message, type = "success") => {
    setToast({ show: true, message, type });
  };

  const initialValues = useMemo(() => {
    const today = new Date();
    const due = new Date(today);
    due.setDate(today.getDate() + 14);
    return {
      status: "DRAFT",
      issueDate: isoDate(today),
      dueDate: isoDate(due),
    };
  }, []);

  const handleSubmit = async (payload) => {
    setSubmitting(true);
    setServerMsg(null);
    try {
      const created = await createInvoice(payload);
      setCreatedInvoice(created);
      setServerMsg({
        type: "success",
        text: `Đã tạo hoá đơn ${created.invoiceNo}.`,
      });
      showToast(`Đã tạo hoá đơn ${created.invoiceNo}`, "success");
    } catch (err) {
      const message =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        "Tạo hoá đơn thất bại";
      setServerMsg({ type: "error", text: message });
      showToast(message, "error");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <button
          onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.INVOICES)}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
        >
          ← Quay lại danh sách hoá đơn
        </button>
        {createdInvoice && (
          <div className="flex items-center gap-2">
            <button
              onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.INVOICE_DETAIL.replace(":id", createdInvoice.invoiceId))}
              className="inline-flex items-center gap-2 rounded-full bg-slate-900 px-4 py-2 text-sm font-semibold text-white shadow hover:bg-black"
            >
              Mở hoá đơn
            </button>
            <button
              onClick={() => {
                setCreatedInvoice(null);
                setServerMsg(null);
              }}
              className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
            >
              Tạo hoá đơn khác
            </button>
          </div>
        )}
      </div>

      <Toast
        show={toast.show}
        message={toast.message}
        type={toast.type}
        onClose={() => setToast((prev) => ({ ...prev, show: false }))}
      />

      <InvoiceForm
        mode="create"
        initialValues={initialValues}
        onSubmit={handleSubmit}
        submitting={submitting}
        serverMsg={serverMsg}
      />
    </div>
  );
}
