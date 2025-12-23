import React, { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import {
  approveVoucher,
  deleteVoucher,
  getVoucherById,
  getVoucherItems,
  updateVoucherStatus,
} from "../../../features/accountant/voucherApi";

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

const VOUCHER_TYPE_LABELS = {
  RECEIPT: "Phiếu thu",
  PAYMENT: "Phiếu chi",
  JOURNAL: "Bút toán",
};

const VOUCHER_TYPE_COLORS = {
  RECEIPT: "bg-emerald-100 text-emerald-700",
  PAYMENT: "bg-rose-100 text-rose-700",
  JOURNAL: "bg-blue-100 text-blue-700",
};

const STATUS_COLORS = {
  DRAFT: "bg-slate-100 text-slate-700",
  PENDING: "bg-amber-100 text-amber-700",
  APPROVED: "bg-green-100 text-green-700",
  CANCELLED: "bg-red-100 text-red-700",
};

const STATUS_LABELS = {
  DRAFT: "Nháp",
  PENDING: "Chờ duyệt",
  APPROVED: "Đã duyệt",
  CANCELLED: "Đã hủy",
};

export default function ViewVoucherPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [voucher, setVoucher] = useState(null);
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [actionMsg, setActionMsg] = useState(null);
  const [processingAction, setProcessingAction] = useState("");

  const load = async () => {
    if (!id) return;
    setLoading(true);
    setError("");
    try {
      const voucherData = await getVoucherById(id);
      setVoucher(voucherData);
      try {
        const itemsData = await getVoucherItems(id);
        setItems(Array.isArray(itemsData) ? itemsData : []);
      } catch (err) {
        console.error("Error fetching voucher items:", err);
        setItems([]);
      }
    } catch (err) {
      setError(
        err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          "Không thể tải thông tin chứng từ"
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (id) {
      load();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const runAction = async (action, task, successMessage) => {
    if (!voucher?.voucherId) return;
    setProcessingAction(action);
    setActionMsg(null);
    try {
      await task();
      await load();
      setActionMsg({ type: "success", text: successMessage });
    } catch (err) {
      setActionMsg({
        type: "error",
        text:
          err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          "Thao tác thất bại",
      });
    } finally {
      setProcessingAction("");
    }
  };

  const handleBackToList = () => {
    navigate(ROUTER_PAGE.ACCOUNTANT.VOUCHERS);
  };

  const handleViewReceipt = () => {
    if (voucher?.receiptId) {
      navigate(ROUTER_PAGE.ACCOUNTANT.RECEIPT_VIEW.replace(":id", voucher.receiptId));
    }
  };

  const handleViewJournal = () => {
    if (voucher?.journalEntryId) {
      // Navigate to journal entry detail if route exists
      alert("Xem Journal Entry: " + voucher.journalEntryId);
    }
  };

  const handleSubmitForApproval = () => {
    if (!voucher) return;
    if (
      !window.confirm(
        `Gửi phiếu chi ${voucher.voucherNo || voucher.voucherId} sang trạng thái Chờ duyệt?`
      )
    )
      return;
    runAction("submit", () => updateVoucherStatus(voucher.voucherId, { status: "PENDING" }), "Đã gửi duyệt.");
  };

  const handleApprove = () => {
    if (!voucher) return;
    if (
      !window.confirm(
        `Duyệt phiếu chi ${voucher.voucherNo || voucher.voucherId}? Hệ thống sẽ tự động tạo bút toán.`
      )
    )
      return;
    runAction("approve", () => approveVoucher(voucher.voucherId), "Đã duyệt phiếu chi.");
  };

  const handleCancelVoucher = () => {
    if (!voucher) return;
    const note = window.prompt(
      `Nhập lý do huỷ phiếu chi ${voucher.voucherNo || voucher.voucherId} (tuỳ chọn):`
    );
    runAction(
      "cancel",
      () => updateVoucherStatus(voucher.voucherId, { status: "CANCELLED", note: note?.trim() || null }),
      "Đã huỷ phiếu chi."
    );
  };

  const handleDeleteVoucher = () => {
    if (!voucher) return;
    if (
      !window.confirm(
        `Xoá phiếu chi ${voucher.voucherNo || voucher.voucherId}? Hành động không thể hoàn tác.`
      )
    )
      return;
    const performDelete = async () => {
      setProcessingAction("delete");
      setActionMsg(null);
      try {
        await deleteVoucher(voucher.voucherId);
        navigate(ROUTER_PAGE.ACCOUNTANT.VOUCHERS);
      } catch (err) {
        setActionMsg({
          type: "error",
          text:
            err?.response?.data?.error ||
            err?.response?.data?.message ||
            err?.message ||
            "Không thể xoá phiếu chi",
        });
      } finally {
        setProcessingAction("");
      }
    };
    performDelete();
  };

  const handleEditVoucher = () => {
    if (!voucher) return;
    navigate(ROUTER_PAGE.ACCOUNTANT.VOUCHER_EDIT.replace(":id", voucher.voucherId));
  };

  if (loading) {
    return (
      <div className="max-w-6xl mx-auto space-y-6">
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600 mb-4"></div>
            <p className="text-slate-600">Đang tải thông tin chứng từ...</p>
          </div>
        </div>
      </div>
    );
  }

  if (error && !voucher) {
    return (
      <div className="max-w-6xl mx-auto space-y-6">
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

  const totalAmount = voucher?.totalAmount ?? voucher?.amount ?? 0;

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <div className="flex items-center gap-3">
          <button
            onClick={handleBackToList}
            className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
          >
            ← Quay lại danh sách
          </button>
          <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-slate-100 text-sm font-semibold text-slate-600">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
              />
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
              />
            </svg>
            Chế độ xem
          </div>
        </div>
        <div className="flex flex-wrap gap-2">
          {voucher?.status === "DRAFT" && (
            <>
              <button
                onClick={handleEditVoucher}
                className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 hover:border-indigo-300 hover:text-indigo-600"
              >
                Sửa
              </button>
              <button
                onClick={handleSubmitForApproval}
                disabled={processingAction === "submit"}
                className={`inline-flex items-center gap-2 rounded-full px-4 py-2 text-sm font-semibold text-white shadow focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 ${
                  processingAction === "submit"
                    ? "bg-indigo-300 cursor-not-allowed"
                    : "bg-indigo-600 hover:bg-indigo-700"
                }`}
              >
                {processingAction === "submit" ? "Đang gửi..." : "Gửi duyệt"}
              </button>
              <button
                onClick={handleCancelVoucher}
                disabled={processingAction === "cancel"}
                className={`inline-flex items-center gap-2 rounded-full px-4 py-2 text-sm font-semibold text-white shadow focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-amber-500 ${
                  processingAction === "cancel"
                    ? "bg-amber-200 cursor-not-allowed"
                    : "bg-amber-500 hover:bg-amber-600"
                }`}
              >
                {processingAction === "cancel" ? "Đang huỷ..." : "Huỷ"}
              </button>
              <button
                onClick={handleDeleteVoucher}
                disabled={processingAction === "delete"}
                className={`inline-flex items-center gap-2 rounded-full px-4 py-2 text-sm font-semibold text-white shadow focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-rose-500 ${
                  processingAction === "delete"
                    ? "bg-rose-200 cursor-not-allowed"
                    : "bg-rose-600 hover:bg-rose-700"
                }`}
              >
                {processingAction === "delete" ? "Đang xoá..." : "Xoá"}
              </button>
            </>
          )}
          {voucher?.status === "PENDING" && (
            <>
              <button
                onClick={handleApprove}
                disabled={processingAction === "approve"}
                className={`inline-flex items-center gap-2 rounded-full px-4 py-2 text-sm font-semibold text-white shadow focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-emerald-500 ${
                  processingAction === "approve"
                    ? "bg-emerald-200 cursor-not-allowed"
                    : "bg-emerald-600 hover:bg-emerald-700"
                }`}
              >
                {processingAction === "approve" ? "Đang duyệt..." : "Duyệt"}
              </button>
              <button
                onClick={handleCancelVoucher}
                disabled={processingAction === "cancel"}
                className={`inline-flex items-center gap-2 rounded-full px-4 py-2 text-sm font-semibold text-white shadow focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-rose-500 ${
                  processingAction === "cancel"
                    ? "bg-rose-200 cursor-not-allowed"
                    : "bg-rose-600 hover:bg-rose-700"
                }`}
              >
                {processingAction === "cancel" ? "Đang huỷ..." : "Huỷ"}
              </button>
            </>
          )}
        </div>
      </div>

      {actionMsg && (
        <div
          className={`rounded-2xl border px-4 py-3 text-sm font-medium ${
            actionMsg.type === "success"
              ? "bg-emerald-50 border-emerald-200 text-emerald-700"
              : "bg-rose-50 border-rose-200 text-rose-700"
          }`}
        >
          {actionMsg.text}
        </div>
      )}

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-700">
          {error}
        </div>
      )}

      {/* Voucher Card */}
      <div className="rounded-3xl border border-slate-200 bg-white shadow-xl overflow-hidden">
        {/* Header */}
        <div className="bg-gradient-to-r from-slate-700 to-slate-800 px-8 py-6 text-white">
          <div className="flex items-start justify-between">
            <div>
              <div className="flex items-center gap-3 mb-2">
                <h1 className="text-3xl font-bold">CHỨNG TỪ KẾ TOÁN</h1>
                <span
                  className={`inline-flex items-center rounded-full px-4 py-1.5 text-sm font-semibold ${
                    VOUCHER_TYPE_COLORS[voucher?.voucherType] || "bg-slate-100 text-slate-700"
                  }`}
                >
                  {VOUCHER_TYPE_LABELS[voucher?.voucherType] || voucher?.voucherType}
                </span>
              </div>
              <p className="text-slate-300">Voucher / Accounting Document</p>
            </div>
            <div className="text-right">
              <div className="text-sm text-slate-300 mb-1">Số chứng từ</div>
              <div className="text-2xl font-bold">{voucher?.voucherNo || voucher?.voucherId}</div>
            </div>
          </div>
        </div>

        {/* Content */}
        <div className="p-8 space-y-6">
          {/* Voucher Information */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Ngày chứng từ
              </label>
              <div className="mt-1 text-lg font-semibold text-slate-800">
                {formatDate(voucher?.voucherDate || voucher?.date)}
              </div>
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Trạng thái
              </label>
              <div className="mt-1">
                <span
                  className={`inline-flex items-center rounded-full px-4 py-2 text-sm font-semibold ${
                    STATUS_COLORS[voucher?.status] || "bg-slate-100 text-slate-700"
                  }`}
                >
                  {STATUS_LABELS[voucher?.status] || voucher?.status}
                </span>
              </div>
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Tổng số tiền
              </label>
              <div className="mt-1 text-xl font-bold text-indigo-600">
                {money.format(totalAmount)}
              </div>
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Đối tác
              </label>
              <div className="mt-1 text-sm font-semibold text-slate-700">
                {voucher?.companyInfo || voucher?.note || "-"}
              </div>
            </div>
          </div>

          {/* Description */}
          {voucher?.description && (
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Diễn giải
              </label>
              <div className="mt-2 rounded-lg bg-slate-50 p-4 text-sm text-slate-700">
                {voucher.description}
              </div>
            </div>
          )}

          {/* Links Section */}
          {(voucher?.receiptId || voucher?.journalEntryId) && (
            <div className="border-t border-b border-slate-200 py-4">
              <div className="flex items-center gap-4">
                {voucher?.receiptId && (
                  <button
                    onClick={handleViewReceipt}
                    className="inline-flex items-center gap-2 rounded-xl bg-emerald-50 border border-emerald-200 px-4 py-2 text-sm font-semibold text-emerald-700 hover:bg-emerald-100 transition"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                      />
                    </svg>
                    Xem biên lai liên quan
                  </button>
                )}
                {voucher?.journalEntryId && (
                  <button
                    onClick={handleViewJournal}
                    className="inline-flex items-center gap-2 rounded-xl bg-blue-50 border border-blue-200 px-4 py-2 text-sm font-semibold text-blue-700 hover:bg-blue-100 transition"
                  >
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"
                      />
                    </svg>
                    Xem bút toán
                  </button>
                )}
              </div>
            </div>
          )}

          {/* Voucher Items Table */}
          <div>
            <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-700 mb-4">
              Chi tiết chứng từ
            </h3>
          <div className="overflow-x-auto rounded-xl border border-slate-200">
            <table className="min-w-full divide-y divide-slate-200 text-sm">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-4 py-3 text-left font-semibold text-slate-600 w-12">#</th>
                  <th className="px-4 py-3 text-left font-semibold text-slate-600">Mô tả</th>
                  <th className="px-4 py-3 text-left font-semibold text-slate-600">Số lượng</th>
                  <th className="px-4 py-3 text-left font-semibold text-slate-600">Đơn giá</th>
                  <th className="px-4 py-3 text-right font-semibold text-slate-600">Thành tiền</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white">
                {items.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="px-4 py-8 text-center text-slate-400">
                      Không có chi tiết chứng từ
                    </td>
                  </tr>
                ) : (
                  items.map((item, index) => (
                    <tr key={item.voucherItemId || index} className="hover:bg-slate-50">
                      <td className="px-4 py-3 text-slate-500">{index + 1}</td>
                      <td className="px-4 py-3">
                        <div className="font-semibold text-slate-700">
                          {item.serviceTypeName || item.description || "-"}
                        </div>
                        {item.description && (
                          <div className="text-xs text-slate-400">{item.description}</div>
                        )}
                      </td>
                      <td className="px-4 py-3 text-sm text-slate-600">
                        {item.quantity ?? "-"}
                      </td>
                      <td className="px-4 py-3 text-sm text-slate-600">
                        {money.format(item.unitPrice ?? 0)}
                      </td>
                      <td className="px-4 py-3 text-right">
                        <span className="font-semibold text-slate-800">
                          {money.format(item.amount ?? 0)}
                        </span>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
              </table>
            </div>
          </div>

          {/* Note */}
          {voucher?.note && (
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Ghi chú
              </label>
              <div className="mt-2 rounded-lg bg-slate-50 p-4 text-sm text-slate-700">
                {voucher.note}
              </div>
            </div>
          )}

          {/* Metadata */}
          <div className="pt-4 border-t border-slate-200 grid grid-cols-2 gap-4 text-xs text-slate-500">
            <div>
              <div>Người tạo: {voucher?.createdBy || "System"}</div>
              <div>Ngày tạo: {formatDateTime(voucher?.createdAt)}</div>
            </div>
            {voucher?.approvedBy && (
              <div>
                <div>Người duyệt: {voucher.approvedBy}</div>
                {voucher?.approvedAt && <div>Ngày duyệt: {formatDateTime(voucher.approvedAt)}</div>}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

