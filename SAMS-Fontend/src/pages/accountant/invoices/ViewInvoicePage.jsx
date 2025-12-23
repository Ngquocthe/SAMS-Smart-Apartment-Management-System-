import React, { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams, useLocation } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import { getInvoiceById } from "../../../features/accountant/invoiceApi";
import InvoiceForm from "./InvoiceForm";
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

export default function ViewInvoicePage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const [invoice, setInvoice] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // Check if navigated from Receipt Detail
  const fromReceipt = location.state?.from === 'receipt';
  const receiptId = location.state?.receiptId;

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

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

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

  const handleBack = () => {
    if (fromReceipt && receiptId) {
      navigate(ROUTER_PAGE.ACCOUNTANT.RECEIPT_VIEW.replace(":id", receiptId));
    } else {
      navigate(ROUTER_PAGE.ACCOUNTANT.INVOICES);
    }
  };

  if (loading) {
    return (
      <div className="max-w-5xl mx-auto">
        <button
          onClick={handleBack}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
        >
          ← {fromReceipt ? "Quay lại biên lai" : "Quay lại danh sách hoá đơn"}
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
          onClick={handleBack}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
        >
          ← {fromReceipt ? "Quay lại biên lai" : "Quay lại danh sách hoá đơn"}
        </button>
        <div className="rounded-3xl border border-rose-200 bg-rose-50 px-6 py-10 text-center text-rose-700 shadow">
          {error}
        </div>
      </div>
    );
  }

  if (!invoice || !initialValues) return null;

  const lineItemsSection = (
    <InvoiceLineItems invoiceId={invoice.invoiceId} onDetailsChanged={load} canEdit={false} />
  );

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <button
          onClick={handleBack}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
        >
          ← {fromReceipt ? "Quay lại biên lai" : "Quay lại danh sách hoá đơn"}
        </button>
        {invoice.status === "DRAFT" && (
          <button
            onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.INVOICE_EDIT.replace(":id", invoice.invoiceId))}
            className="inline-flex items-center gap-2 rounded-full bg-slate-900 px-4 py-2 text-sm font-semibold text-white shadow hover:bg-black"
          >
            Chỉnh sửa hoá đơn
          </button>
        )}
      </div>
      <InvoiceForm
        mode="update"
        initialValues={initialValues}
        summary={summary}
        onSubmit={() => {}}
        submitting={false}
        serverMsg={null}
        canEdit={false}
        lineItemsSection={lineItemsSection}
      />
    </div>
  );
}


