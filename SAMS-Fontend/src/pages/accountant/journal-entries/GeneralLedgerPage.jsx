import React, { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import { getGeneralLedger } from "../../../features/accountant/journalEntryApi";

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

const formatPeriod = (value) => {
  if (!value) return "-";
  const parts = value.split("-");
  if (parts.length >= 2) {
    const [year, month] = parts;
    return `Tháng ${month}/${year}`;
  }
  return value;
};

export default function GeneralLedgerPage() {
  const navigate = useNavigate();
  const [filters, setFilters] = useState(() => {
    const today = new Date();
    const month = String(today.getMonth() + 1).padStart(2, "0");
    return {
      accountCode: "",
      period: `${today.getFullYear()}-${month}`,
    };
  });
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    if (!filters.accountCode) {
      setData(null);
      return;
    }

    setLoading(true);
    setError("");
    try {
      const res = await getGeneralLedger({
        accountCode: filters.accountCode,
        period: filters.period || undefined,
      });
      console.log("📥 General Ledger data:", res);

      const normalized = {
        raw: res,
        accountCode: res?.accountCode || filters.accountCode,
        accountName:
          res?.accountName ||
          res?.account?.name ||
          res?.account?.accountName ||
          "-",
        period: res?.period || filters.period,
        openingBalance:
          res?.openingBalance ?? res?.beginBalance ?? res?.opening ?? 0,
        transactions:
          res?.transactions ||
          res?.entries ||
          res?.items ||
          res?.ledgerEntries ||
          [],
      };

      setData(normalized);
    } catch (err) {
      setError(
        err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          "Không thể tải sổ cái"
      );
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [filters]);

  const onSearch = (e) => {
    e.preventDefault();
    load();
  };

  const onReset = () => {
    const today = new Date();
    const month = String(today.getMonth() + 1).padStart(2, "0");
    setFilters({
      accountCode: "",
      period: `${today.getFullYear()}-${month}`,
    });
    setData(null);
    setError("");
  };

  const handleBackToList = () => {
    navigate(ROUTER_PAGE.ACCOUNTANT.JOURNAL_ENTRIES);
  };

  const handlePrint = () => {
    window.print();
  };

  // Tính tổng nợ, tổng có, và số dư
  const transactions =
    data?.transactions ||
    data?.entries ||
    data?.items ||
    [];

  const getDebitValue = (entry) =>
    entry?.debitAmount ??
    entry?.debit ??
    entry?.debitValue ??
    entry?.amountDebit ??
    entry?.debitSum ??
    0;

  const getCreditValue = (entry) =>
    entry?.creditAmount ??
    entry?.credit ??
    entry?.creditValue ??
    entry?.amountCredit ??
    entry?.creditSum ??
    0;

  const totalDebit = transactions.reduce(
    (sum, entry) => sum + getDebitValue(entry),
    0
  );
  const totalCredit = transactions.reduce(
    (sum, entry) => sum + getCreditValue(entry),
    0
  );
  const balance = (data?.openingBalance || 0) + totalDebit - totalCredit;

  return (
    <div className="max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <button
            onClick={handleBackToList}
            className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600 mb-3"
          >
            ← Quay lại sổ nhật ký
          </button>
          <h1 className="text-3xl font-bold text-slate-800">Sổ cái theo tài khoản</h1>
          <p className="text-sm text-slate-500">
            Xem chi tiết phát sinh nợ, có và số dư của từng tài khoản kế toán.
          </p>
        </div>
        {data && (
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
            In sổ cái
          </button>
        )}
      </div>

      {/* Filters */}
      <form
        onSubmit={onSearch}
        className="grid grid-cols-1 lg:grid-cols-4 gap-4 rounded-3xl border border-slate-200 bg-white/80 p-5 shadow-sm backdrop-blur"
      >
        <div className="lg:col-span-2">
          <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
            Mã tài khoản *
          </label>
          <input
            value={filters.accountCode}
            onChange={(e) => setFilters((prev) => ({ ...prev, accountCode: e.target.value }))}
            className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
            placeholder="Ví dụ: 111, 131, 512..."
            required
          />
        </div>
        <div>
          <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
            Kỳ báo cáo (YYYY-MM)
          </label>
          <input
            type="month"
            value={filters.period}
            onChange={(e) => setFilters((prev) => ({ ...prev, period: e.target.value }))}
            className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
            required
          />
        </div>
        <div className="flex items-end gap-2">
          <button
            type="submit"
            className="inline-flex items-center justify-center rounded-xl bg-slate-900 px-5 py-2.5 text-sm font-semibold text-white shadow hover:bg-black focus:outline-none focus:ring-2 focus:ring-slate-500 focus:ring-offset-2"
          >
            Xem sổ cái
          </button>
          <button
            type="button"
            onClick={onReset}
            className="inline-flex items-center justify-center rounded-xl border border-slate-300 px-5 py-2.5 text-sm font-semibold text-slate-600 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-slate-400 focus:ring-offset-2"
          >
            Đặt lại
          </button>
        </div>
      </form>

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-700">
          {error}
        </div>
      )}

      {loading && (
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600 mb-4"></div>
            <p className="text-slate-600">Đang tải sổ cái...</p>
          </div>
        </div>
      )}

      {!loading && data && (
        <div className="rounded-3xl border border-slate-200 bg-white shadow-xl overflow-hidden">
          {/* Header */}
          <div className="bg-gradient-to-r from-indigo-600 to-indigo-700 px-8 py-6 text-white">
            <h2 className="text-2xl font-bold mb-2">SỔ CÁI TÀI KHOẢN</h2>
            <div className="flex items-center gap-6 text-indigo-100">
              <div>
                <span className="text-sm">Tài khoản:</span>{" "}
                <span className="font-semibold text-white">{data.accountCode || filters.accountCode}</span>
              </div>
              <div>
                <span className="text-sm">Tên tài khoản:</span>{" "}
                <span className="font-semibold text-white">{data.accountName || "-"}</span>
              </div>
            </div>
            <div className="mt-2 text-sm text-indigo-100">
              Kỳ báo cáo:{" "}
              <span className="font-semibold text-white">
                {formatPeriod(data.period || filters.period)}
              </span>
            </div>
          </div>

          {/* Opening Balance */}
          <div className="px-8 py-4 bg-slate-50 border-b border-slate-200">
            <div className="flex items-center justify-between">
              <span className="text-sm font-semibold text-slate-700">Số dư đầu kỳ:</span>
              <span className="text-lg font-bold text-indigo-600">
                {money.format(data.openingBalance || 0)}
              </span>
            </div>
          </div>

          {/* Transactions */}
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-100">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-slate-600">Ngày</th>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-slate-600">Chứng từ</th>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-slate-600">Diễn giải</th>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-slate-600">TK đối ứng</th>
                  <th className="px-6 py-3 text-right text-xs font-semibold text-slate-600">Nợ</th>
                  <th className="px-6 py-3 text-right text-xs font-semibold text-slate-600">Có</th>
                  <th className="px-6 py-3 text-right text-xs font-semibold text-slate-600">Số dư</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white">
                {transactions.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-6 py-12 text-center text-slate-400">
                      Không có giao dịch nào trong khoảng thời gian này
                    </td>
                  </tr>
                ) : (
                  transactions.map((transaction, index) => {
                    const debitValue = getDebitValue(transaction);
                    const creditValue = getCreditValue(transaction);
                    const runningBalance =
                      (data.openingBalance || 0) +
                      transactions
                        .slice(0, index + 1)
                        .reduce(
                          (sum, entry) =>
                            sum + getDebitValue(entry) - getCreditValue(entry),
                          0
                        );

                    return (
                      <tr key={transaction.id || index} className="hover:bg-slate-50">
                        <td className="px-6 py-3 text-sm text-slate-600">
                          {formatDate(
                            transaction.date ||
                              transaction.entryDate ||
                              transaction.transactionDate
                          )}
                        </td>
                        <td className="px-6 py-3 text-sm font-mono font-semibold text-slate-700">
                          {transaction.documentNo ||
                            transaction.entryNo ||
                            transaction.referenceNo ||
                            "-"}
                        </td>
                        <td className="px-6 py-3 text-sm text-slate-700 max-w-md truncate">
                          {transaction.description ||
                            transaction.note ||
                            transaction.remark ||
                            "-"}
                        </td>
                        <td className="px-6 py-3 text-sm font-mono text-slate-600">
                          {transaction.correspondingAccount ||
                            transaction.oppositeAccount ||
                            transaction.counterAccount ||
                            "-"}
                        </td>
                        <td className="px-6 py-3 text-sm text-right font-semibold text-emerald-600">
                          {debitValue
                            ? money.format(debitValue)
                            : "-"}
                        </td>
                        <td className="px-6 py-3 text-sm text-right font-semibold text-rose-600">
                          {creditValue
                            ? money.format(creditValue)
                            : "-"}
                        </td>
                        <td className="px-6 py-3 text-sm text-right font-bold text-indigo-600">
                          {money.format(runningBalance)}
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>

          {/* Summary */}
          <div className="px-8 py-6 bg-slate-50 border-t border-slate-200 space-y-3">
            <div className="grid grid-cols-3 gap-6">
              <div>
                <div className="text-xs font-semibold uppercase tracking-wide text-slate-500 mb-1">
                  Tổng phát sinh Nợ
                </div>
                <div className="text-xl font-bold text-emerald-600">{money.format(totalDebit)}</div>
              </div>
              <div>
                <div className="text-xs font-semibold uppercase tracking-wide text-slate-500 mb-1">
                  Tổng phát sinh Có
                </div>
                <div className="text-xl font-bold text-rose-600">{money.format(totalCredit)}</div>
              </div>
              <div>
                <div className="text-xs font-semibold uppercase tracking-wide text-slate-500 mb-1">
                  Số dư cuối kỳ
                </div>
                <div className="text-xl font-bold text-indigo-600">{money.format(balance)}</div>
              </div>
            </div>
          </div>
        </div>
      )}

      {!loading && !data && !error && (
        <div className="rounded-3xl border border-slate-200 bg-white shadow-lg p-12 text-center">
          <svg
            className="mx-auto h-16 w-16 text-slate-300 mb-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
            />
          </svg>
          <h3 className="text-lg font-semibold text-slate-700 mb-2">Chưa có dữ liệu</h3>
          <p className="text-sm text-slate-500">
            Nhập mã tài khoản và chọn kỳ báo cáo để xem sổ cái
          </p>
        </div>
      )}
    </div>
  );
}

