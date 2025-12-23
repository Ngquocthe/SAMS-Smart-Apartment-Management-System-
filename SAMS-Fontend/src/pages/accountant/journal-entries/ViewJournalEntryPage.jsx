import React, { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import { getJournalEntryById } from "../../../features/accountant/journalEntryApi";

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

export default function ViewJournalEntryPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [entry, setEntry] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      setError("");
      try {
        const entryData = await getJournalEntryById(id);
        console.log("📥 Journal Entry data:", entryData);
        setEntry(entryData);
      } catch (err) {
        setError(
          err?.response?.data?.error ||
            err?.response?.data?.message ||
            err?.message ||
            "Không thể tải thông tin bút toán"
        );
      } finally {
        setLoading(false);
      }
    };

    if (id) {
      fetchData();
    }
  }, [id]);

  const handleBackToList = () => {
    navigate(ROUTER_PAGE.ACCOUNTANT.JOURNAL_ENTRIES);
  };

  const handlePrint = () => {
    window.print();
  };

  const getEntryTypeLabel = (type) => {
    const typeMap = {
      RECEIPT: "Phiếu thu",
      VOUCHER: "Phiếu chi",
      INVOICE: "Hóa đơn",
      ADJUSTMENT: "Điều chỉnh",
      OTHER: "Khác",
    };
    return typeMap[type] || type || "-";
  };

  const getEntryTypeColor = (type) => {
    const colorMap = {
      RECEIPT: "bg-emerald-100 text-emerald-700",
      VOUCHER: "bg-rose-100 text-rose-700",
      INVOICE: "bg-blue-100 text-blue-700",
      ADJUSTMENT: "bg-amber-100 text-amber-700",
      OTHER: "bg-slate-100 text-slate-700",
    };
    return colorMap[type] || "bg-slate-100 text-slate-700";
  };

  if (loading) {
    return (
      <div className="max-w-5xl mx-auto space-y-6">
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600 mb-4"></div>
            <p className="text-slate-600">Đang tải thông tin bút toán...</p>
          </div>
        </div>
      </div>
    );
  }

  if (error && !entry) {
    return (
      <div className="max-w-5xl mx-auto space-y-6">
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

  // Tính tổng nợ và tổng có
  const lines = entry?.lines || entry?.details || [];
  const totalDebit = lines.reduce((sum, line) => sum + (line.debitAmount || 0), 0);
  const totalCredit = lines.reduce((sum, line) => sum + (line.creditAmount || 0), 0);

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <button
          onClick={handleBackToList}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
        >
          ← Quay lại danh sách
        </button>
        <button
          onClick={handlePrint}
          className="inline-flex items-center gap-2 rounded-full bg-slate-900 px-4 py-2 text-sm font-semibold text-white shadow hover:bg-black focus:outline-none focus:ring-2 focus:ring-slate-500 focus:ring-offset-2"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"
            />
          </svg>
          In bút toán
        </button>
      </div>

      {/* Entry Card */}
      <div className="rounded-3xl border border-slate-200 bg-white shadow-xl overflow-hidden">
        {/* Header */}
        <div className="bg-gradient-to-r from-indigo-600 to-indigo-700 px-8 py-6 text-white">
          <div className="flex items-start justify-between">
            <div>
              <h1 className="text-3xl font-bold mb-2">BÚT TOÁN KẾ TOÁN</h1>
              <p className="text-indigo-100">Journal Entry / Accounting Entry</p>
            </div>
            <div className="text-right">
              <div className="text-sm text-indigo-200 mb-1">Mã bút toán</div>
              <div className="text-2xl font-bold">{entry?.entryNo || entry?.journalEntryId || entry?.id}</div>
            </div>
          </div>
        </div>

        {/* Content */}
        <div className="p-8 space-y-6">
          {/* Entry Information */}
          <div className="grid grid-cols-2 gap-6">
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Ngày ghi sổ
              </label>
              <div className="mt-1 text-lg font-semibold text-slate-800">
                {formatDate(entry?.entryDate || entry?.date || entry?.createdAt)}
              </div>
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Loại bút toán
              </label>
              <div className="mt-1">
                <span
                  className={`inline-flex items-center rounded-full px-4 py-2 text-sm font-semibold ${getEntryTypeColor(
                    entry?.entryType || entry?.type
                  )}`}
                >
                  {getEntryTypeLabel(entry?.entryType || entry?.type)}
                </span>
              </div>
            </div>
          </div>

          {/* Description */}
          {entry?.description && (
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Diễn giải
              </label>
              <div className="mt-2 rounded-lg bg-slate-50 p-4 text-sm text-slate-700">
                {entry.description}
              </div>
            </div>
          )}

          {/* Account Lines */}
          <div>
            <label className="text-xs font-semibold uppercase tracking-wide text-slate-500 mb-3 block">
              Chi tiết bút toán
            </label>
            <div className="rounded-xl border border-slate-200 overflow-hidden">
              <table className="min-w-full divide-y divide-slate-200">
                <thead className="bg-slate-50">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-slate-600">TK</th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-slate-600">Tên tài khoản</th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-slate-600">Diễn giải</th>
                    <th className="px-4 py-3 text-right text-xs font-semibold text-slate-600">Nợ</th>
                    <th className="px-4 py-3 text-right text-xs font-semibold text-slate-600">Có</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 bg-white">
                  {lines.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="px-4 py-8 text-center text-sm text-slate-400">
                        Không có chi tiết bút toán
                      </td>
                    </tr>
                  ) : (
                    lines.map((line, index) => (
                      <tr key={line.id || index} className="hover:bg-slate-50">
                        <td className="px-4 py-3 text-sm font-mono font-semibold text-slate-700">
                          {line.accountCode || line.account?.code || "-"}
                        </td>
                        <td className="px-4 py-3 text-sm text-slate-700">
                          {line.accountName || line.account?.name || "-"}
                        </td>
                        <td className="px-4 py-3 text-sm text-slate-600">
                          {line.description || line.note || "-"}
                        </td>
                        <td className="px-4 py-3 text-sm text-right font-semibold text-emerald-600">
                          {line.debitAmount ? money.format(line.debitAmount) : "-"}
                        </td>
                        <td className="px-4 py-3 text-sm text-right font-semibold text-rose-600">
                          {line.creditAmount ? money.format(line.creditAmount) : "-"}
                        </td>
                      </tr>
                    ))
                  )}
                  {/* Total Row */}
                  <tr className="bg-slate-50 font-semibold">
                    <td colSpan={3} className="px-4 py-3 text-sm text-slate-700">
                      Tổng cộng
                    </td>
                    <td className="px-4 py-3 text-sm text-right text-emerald-700">
                      {money.format(totalDebit)}
                    </td>
                    <td className="px-4 py-3 text-sm text-right text-rose-700">
                      {money.format(totalCredit)}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            {/* Balance Check */}
            {totalDebit !== totalCredit && (
              <div className="mt-3 rounded-lg bg-amber-50 border border-amber-200 px-4 py-3">
                <div className="flex items-center gap-2">
                  <svg
                    className="w-5 h-5 text-amber-600"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                    />
                  </svg>
                  <div className="text-sm text-amber-800">
                    <strong>Cảnh báo:</strong> Tổng nợ không bằng tổng có. Chênh lệch:{" "}
                    <strong>{money.format(Math.abs(totalDebit - totalCredit))}</strong>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Reference Information */}
          {(entry?.referenceId || entry?.referenceType) && (
            <div className="rounded-xl bg-blue-50 border border-blue-200 p-4">
              <div className="flex items-center gap-2 mb-2">
                <svg className="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"
                  />
                </svg>
                <span className="text-sm font-semibold text-blue-800">Tham chiếu</span>
              </div>
              <div className="text-sm text-blue-700">
                <div>
                  <span className="text-blue-600">Loại:</span>{" "}
                  <span className="font-medium">{entry.referenceType || "-"}</span>
                </div>
                <div>
                  <span className="text-blue-600">Mã:</span>{" "}
                  <span className="font-medium">{entry.referenceId || "-"}</span>
                </div>
              </div>
            </div>
          )}

          {/* Created Info */}
          <div className="pt-4 border-t border-slate-200 text-xs text-slate-500">
            <div className="flex items-center justify-between">
              <div>Người tạo: {entry?.createdBy || "System"}</div>
              <div>Ngày tạo: {formatDateTime(entry?.createdAt)}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

