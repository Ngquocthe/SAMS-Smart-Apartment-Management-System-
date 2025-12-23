import React, { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import api from "../../../lib/apiClient";
import { getReceiptById, deleteReceipt } from "../../../features/accountant/receiptApi";

const money = new Intl.NumberFormat("vi-VN", {
  style: "currency",
  currency: "VND",
  maximumFractionDigits: 0,
});

const formatDate = (value) => {
  if (!value) return "-";
  const str = value.toString();
  const parts = str.split("T")[0]?.split("-");
  if (parts?.length === 3) {
    return `${parts[2]}/${parts[1]}/${parts[0]}`;
  }
  return str;
};

const formatDateTime = (value) => {
  if (!value) return "-";
  try {
    const date = new Date(value);
    return date.toLocaleString("vi-VN", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return formatDate(value);
  }
};

const PAYMENT_METHOD_LABELS = {
  CASH: "Tiền mặt",
  VIETQR: "VietQR",
  BANK_TRANSFER: "Chuyển khoản",
  CARD: "Thẻ",
  OTHER: "Khác",
};

const PAYMENT_METHOD_COLORS = {
  CASH: "bg-emerald-100 text-emerald-700",
  VIETQR: "bg-blue-100 text-blue-700",
  BANK_TRANSFER: "bg-indigo-100 text-indigo-700",
  CARD: "bg-purple-100 text-purple-700",
  OTHER: "bg-slate-100 text-slate-700",
};

export default function ViewReceiptPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [receipt, setReceipt] = useState(null);
  const [invoice, setInvoice] = useState(null);
  const [apartment, setApartment] = useState(null);
  const [voucher, setVoucher] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      setError("");
      try {
        // Fetch receipt
        const receiptData = await getReceiptById(id);
        console.log("📥 Receipt data from API:", receiptData);
        console.log("💰 Amount fields:", {
          amount: receiptData?.amount,
          amountTotal: receiptData?.amountTotal,
          receivedAmount: receiptData?.receivedAmount,
          totalAmount: receiptData?.totalAmount
        });
        setReceipt(receiptData);

        // Fetch invoice if exists
        if (receiptData.invoiceId) {
          try {
            const invoiceRes = await api.get(`/Invoice/${receiptData.invoiceId}`);
            setInvoice(invoiceRes.data);

            // Fetch apartment if invoice has apartmentId
            if (invoiceRes.data?.apartmentId) {
              try {
                const apartmentRes = await api.get(`/Apartment/${invoiceRes.data.apartmentId}`);
                setApartment(apartmentRes.data);
              } catch (err) {
                console.error("Error fetching apartment:", err);
              }
            }
          } catch (err) {
            console.error("Error fetching invoice:", err);
          }
        }

        // Fetch voucher if exists
        if (receiptData.voucherId) {
          try {
            const voucherRes = await api.get(`/Voucher/${receiptData.voucherId}`);
            setVoucher(voucherRes.data);
          } catch (err) {
            console.error("Error fetching voucher:", err);
          }
        }
      } catch (err) {
        setError(
          err?.response?.data?.error ||
            err?.response?.data?.message ||
            err?.message ||
            "Không thể tải thông tin biên lai"
        );
      } finally {
        setLoading(false);
      }
    };

    if (id) {
      fetchData();
    }
  }, [id]);

  const handlePrint = () => {
    // TODO: Implement print functionality
    window.print();
  };

  const handleSendEmail = () => {
    // TODO: Implement send email functionality
    alert("Tính năng gửi email sẽ được thêm sau");
  };

  const handleDelete = async () => {
    if (!window.confirm("Bạn có chắc chắn muốn xóa biên lai này không?")) {
      return;
    }

    setDeleting(true);
    setError("");
    try {
      await deleteReceipt(id);
      alert("Đã xóa biên lai thành công");
      navigate(ROUTER_PAGE.ACCOUNTANT.RECEIPTS);
    } catch (err) {
      setError(
        err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          "Không thể xóa biên lai"
      );
    } finally {
      setDeleting(false);
    }
  };

  const handleViewInvoice = () => {
    if (invoice) {
      navigate(ROUTER_PAGE.ACCOUNTANT.INVOICE_VIEW.replace(":id", invoice.invoiceId), {
        state: { from: 'receipt', receiptId: id }
      });
    }
  };

  const handleBackToList = () => {
    navigate(ROUTER_PAGE.ACCOUNTANT.RECEIPTS);
  };

  const getApartmentName = () => {
    if (!apartment) return "-";
    return `${apartment.floorNumber ? `Tầng ${apartment.floorNumber} - ` : ""}${apartment.number || apartment.apartmentId}`;
  };

  if (loading) {
    return (
      <div className="max-w-4xl mx-auto space-y-6">
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600 mb-4"></div>
            <p className="text-slate-600">Đang tải thông tin biên lai...</p>
          </div>
        </div>
      </div>
    );
  }

  if (error && !receipt) {
    return (
      <div className="max-w-4xl mx-auto space-y-6">
        <button
          onClick={handleBackToList}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
        >
          ← Quay lại danh sách
        </button>
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-700">
          {error}
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <button
          onClick={handleBackToList}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
        >
          ← Quay lại danh sách
        </button>
      </div>

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-700">
          {error}
        </div>
      )}

      {/* Receipt Card */}
      <div className="rounded-3xl border border-slate-200 bg-white shadow-xl overflow-hidden">
        {/* Header */}
        <div className="bg-gradient-to-r from-indigo-600 to-indigo-700 px-8 py-6 text-white">
          <div className="flex items-start justify-between">
            <div>
              <h1 className="text-3xl font-bold mb-2">BIÊN LAI THU TIỀN</h1>
              <p className="text-indigo-100">Receipt / Proof of Payment</p>
            </div>
            <div className="text-right">
              <div className="text-sm text-indigo-200 mb-1">Số biên lai</div>
              <div className="text-2xl font-bold">{receipt?.receiptNo || receipt?.receiptId}</div>
            </div>
          </div>
        </div>

        {/* Content */}
        <div className="p-8 space-y-6">
          {/* Receipt Information */}
          <div className="grid grid-cols-2 gap-6">
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Ngày thu tiền
              </label>
              <div className="mt-1 text-lg font-semibold text-slate-800">
                {formatDateTime(receipt?.receiptDate || receipt?.date || receipt?.createdAt)}
              </div>
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Phương thức thanh toán
              </label>
              <div className="mt-1">
                <span
                  className={`inline-flex items-center rounded-full px-4 py-2 text-sm font-semibold ${
                    PAYMENT_METHOD_COLORS[receipt?.method?.code || receipt?.paymentMethod] || "bg-slate-100 text-slate-700"
                  }`}
                >
                  {receipt?.method?.name || PAYMENT_METHOD_LABELS[receipt?.paymentMethod] || receipt?.paymentMethod || "-"}
                </span>
              </div>
            </div>
          </div>

          {/* Amount */}
          <div className="border-t border-b border-slate-200 py-6">
            <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
              Số tiền đã thu
            </label>
            <div className="mt-2 text-4xl font-bold text-indigo-600">
              {money.format(
                receipt?.amount ?? 
                receipt?.amountTotal ?? 
                receipt?.receivedAmount ?? 
                receipt?.totalAmount ?? 
                0
              )}
            </div>
          </div>

          {/* Invoice Information */}
          {invoice && (
            <div className="rounded-xl bg-slate-50 p-6">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-700">
                  Thông tin hóa đơn
                </h3>
                <button
                  onClick={handleViewInvoice}
                  className="inline-flex items-center gap-1 text-sm font-semibold text-indigo-600 hover:text-indigo-700"
                >
                  Xem chi tiết
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M9 5l7 7-7 7"
                    />
                  </svg>
                </button>
              </div>
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-slate-500">Số hóa đơn:</span>
                  <span className="ml-2 font-semibold text-slate-800">{invoice.invoiceNo}</span>
                </div>
                <div>
                  <span className="text-slate-500">Căn hộ:</span>
                  <span className="ml-2 font-semibold text-slate-800">{getApartmentName()}</span>
                </div>
                <div>
                  <span className="text-slate-500">Ngày phát hành:</span>
                  <span className="ml-2 text-slate-700">{formatDate(invoice.issueDate)}</span>
                </div>
                <div>
                  <span className="text-slate-500">Ngày đến hạn:</span>
                  <span className="ml-2 text-slate-700">{formatDate(invoice.dueDate)}</span>
                </div>
                <div className="col-span-2">
                  <span className="text-slate-500">Tổng tiền hóa đơn:</span>
                  <span className="ml-2 text-lg font-bold text-slate-800">
                    {money.format(invoice.totalAmount ?? 0)}
                  </span>
                </div>
              </div>
            </div>
          )}

          {/* Voucher Information (if exists) */}
          {voucher && (
            <div className="rounded-xl bg-amber-50 border border-amber-200 p-6">
              <div className="flex items-center gap-2 mb-3">
                <svg className="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M15 5v2m0 4v2m0 4v2M5 5a2 2 0 00-2 2v3a2 2 0 110 4v3a2 2 0 002 2h14a2 2 0 002-2v-3a2 2 0 110-4V7a2 2 0 00-2-2H5z"
                  />
                </svg>
                <h3 className="text-sm font-semibold uppercase tracking-wide text-amber-800">
                  Phiếu chi tự động
                </h3>
              </div>
              <div className="text-sm">
                <div className="mb-2">
                  <span className="text-amber-700">Mã phiếu:</span>
                  <span className="ml-2 font-semibold text-amber-900">
                    {voucher.voucherNo || voucher.voucherId}
                  </span>
                </div>
                <p className="text-amber-600 text-xs">
                  Phiếu chi đã được tạo tự động khi thu tiền tại quầy
                </p>
              </div>
            </div>
          )}

          {/* Note */}
          {receipt?.note && (
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Ghi chú
              </label>
              <div className="mt-2 rounded-lg bg-slate-50 p-4 text-sm text-slate-700">
                {receipt.note}
              </div>
            </div>
          )}

          {/* Created Info */}
          <div className="pt-4 border-t border-slate-200 text-xs text-slate-500">
            <div className="flex items-center justify-between">
              <div>
                Người tạo: {receipt?.createdBy || "System"}
              </div>
              <div>
                Ngày tạo: {formatDateTime(receipt?.createdAt)}
              </div>
            </div>
          </div>
        </div>

        {/* Actions Footer */}
        <div className="bg-slate-50 px-8 py-6 border-t border-slate-200">
          <div className="grid grid-cols-4 gap-3">
            <button
              onClick={handlePrint}
              className="inline-flex items-center justify-center gap-2 rounded-xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white shadow hover:bg-black focus:outline-none focus:ring-2 focus:ring-slate-500 focus:ring-offset-2"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"
                />
              </svg>
              In biên lai
            </button>
            <button
              onClick={handleSendEmail}
              className="inline-flex items-center justify-center gap-2 rounded-xl border border-slate-300 px-4 py-2.5 text-sm font-semibold text-slate-600 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-slate-400 focus:ring-offset-2"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                />
              </svg>
              Gửi Email
            </button>
            <button
              onClick={handleViewInvoice}
              disabled={!invoice}
              className="inline-flex items-center justify-center gap-2 rounded-xl border border-slate-300 px-4 py-2.5 text-sm font-semibold text-slate-600 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-slate-400 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                />
              </svg>
              Xem Invoice
            </button>
            <button
              onClick={handleDelete}
              disabled={deleting}
              className="inline-flex items-center justify-center gap-2 rounded-xl bg-rose-600 px-4 py-2.5 text-sm font-semibold text-white shadow hover:bg-rose-700 focus:outline-none focus:ring-2 focus:ring-rose-500 focus:ring-offset-2 disabled:bg-rose-400"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                />
              </svg>
              {deleting ? "Đang xóa..." : "Xóa"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

