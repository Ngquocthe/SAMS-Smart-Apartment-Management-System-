import React, { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import { getIncomeStatement } from "../../../features/accountant/journalEntryApi";

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

export default function IncomeStatementPage() {
  const navigate = useNavigate();
  const [filters, setFilters] = useState(() => {
    const today = new Date();
    const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
    return {
      dateFrom: firstDay.toISOString().split("T")[0],
      dateTo: today.toISOString().split("T")[0],
    };
  });
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const res = await getIncomeStatement({
        dateFrom: filters.dateFrom || undefined,
        dateTo: filters.dateTo || undefined,
      });
      console.log("📥 Income Statement data:", res);
      const normalized = {
        fromDate: res?.fromDate || res?.dateFrom || filters.dateFrom,
        toDate: res?.toDate || res?.dateTo || filters.dateTo,
        generatedAt: res?.generatedAt || res?.generated || null,
        revenueItems:
          res?.revenueItems ||
          res?.revenues ||
          res?.income ||
          res?.incomeItems ||
          [],
        expenseItems:
          res?.expenseItems ||
          res?.expenses ||
          res?.expense ||
          [],
        totalRevenue:
          res?.totalRevenue ??
          res?.revenueTotal ??
          res?.incomeTotal ??
          null,
        totalExpense:
          res?.totalExpense ??
          res?.expenseTotal ??
          res?.totalExpenses ??
          null,
        netIncome:
          res?.netIncome ??
          res?.netProfit ??
          res?.profit ??
          null,
        raw: res,
      };
      setData(normalized);
    } catch (err) {
      setError(
        err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          "Không thể tải báo cáo thu chi"
      );
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    load();
  }, [load]);

  const handleBackToList = () => {
    navigate(ROUTER_PAGE.ACCOUNTANT.JOURNAL_ENTRIES);
  };

  const handlePrint = () => {
    window.print();
  };

  // Tính toán các tổng
  const revenues =
    data?.revenueItems ||
    data?.revenues ||
    data?.income ||
    data?.incomeItems ||
    [];
  const expenses =
    data?.expenseItems ||
    data?.expenses ||
    data?.expense ||
    [];

  const totalRevenue =
    data?.totalRevenue ??
    revenues.reduce((sum, item) => sum + (item.amount || 0), 0);
  const totalExpense =
    data?.totalExpense ??
    expenses.reduce((sum, item) => sum + (item.amount || 0), 0);
  const netIncome =
    data?.netIncome ??
    data?.netProfit ??
    totalRevenue - totalExpense;

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <button
            onClick={handleBackToList}
            className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600 mb-3"
          >
            ← Quay lại sổ nhật ký
          </button>
          <h1 className="text-3xl font-bold text-slate-800">Báo cáo thu chi</h1>
          <p className="text-sm text-slate-500">
            Báo cáo kết quả hoạt động kinh doanh, doanh thu và chi phí.
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
            In báo cáo
          </button>
        )}
      </div>

      {/* Date Filter */}
      <div className="rounded-3xl border border-slate-200 bg-white/80 p-5 shadow-sm backdrop-blur">
        <div className="flex items-end gap-4">
          <div className="flex-1">
            <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
              Từ ngày
            </label>
            <input
              type="date"
              value={filters.dateFrom}
              onChange={(e) => setFilters((prev) => ({ ...prev, dateFrom: e.target.value }))}
              className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
            />
          </div>
          <div className="flex-1">
            <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
              Đến ngày
            </label>
            <input
              type="date"
              value={filters.dateTo}
              onChange={(e) => setFilters((prev) => ({ ...prev, dateTo: e.target.value }))}
              className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
            />
          </div>
          <button
            onClick={load}
            disabled={loading}
            className="inline-flex items-center justify-center rounded-xl bg-slate-900 px-6 py-2.5 text-sm font-semibold text-white shadow hover:bg-black focus:outline-none focus:ring-2 focus:ring-slate-500 focus:ring-offset-2 disabled:bg-slate-400"
          >
            {loading ? "Đang tải..." : "Xem báo cáo"}
          </button>
        </div>
      </div>

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-700">
          {error}
        </div>
      )}

      {loading && (
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600 mb-4"></div>
            <p className="text-slate-600">Đang tải báo cáo thu chi...</p>
          </div>
        </div>
      )}

      {!loading && data && (
        <div className="rounded-3xl border border-slate-200 bg-white shadow-xl overflow-hidden">
          {/* Header */}
          <div className="bg-gradient-to-r from-indigo-600 to-indigo-700 px-8 py-6 text-white text-center">
            <h2 className="text-2xl font-bold mb-2">BÁO CÁO THU CHI</h2>
            <p className="text-indigo-100">Income Statement / Profit & Loss</p>
            <div className="mt-3 text-sm">
              Từ{" "}
              <span className="font-semibold">
                {formatDate(data?.fromDate || filters.dateFrom)}
              </span>{" "}
              đến{" "}
              <span className="font-semibold">
                {formatDate(data?.toDate || filters.dateTo)}
              </span>
            </div>
          </div>

          <div className="p-8 space-y-8">
            {/* Revenue Section */}
            <div>
              <h3 className="text-lg font-bold text-slate-800 mb-4 pb-2 border-b-2 border-emerald-600 flex items-center gap-2">
                <svg className="w-5 h-5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
                DOANH THU
              </h3>
              <div className="space-y-2">
                {revenues.length === 0 ? (
                  <p className="text-sm text-slate-400 py-4">Không có dữ liệu doanh thu</p>
                ) : (
                  revenues.map((item, index) => (
                    <div
                      key={item.accountCode || index}
                      className="flex items-center justify-between py-3 px-4 hover:bg-emerald-50 rounded-lg transition"
                    >
                      <div className="flex-1">
                        <div className="text-sm font-semibold text-slate-700">
                          {item.accountName || item.category || item.name}
                        </div>
                        <div className="text-xs text-slate-500 font-mono">
                          TK: {item.accountCode || item.account || "-"}
                        </div>
                      </div>
                      <div className="text-base font-bold text-emerald-600">
                        {money.format(
                          item.amount ??
                            item.totalAmount ??
                            item.value ??
                            0
                        )}
                      </div>
                    </div>
                  ))
                )}
              </div>
              <div className="mt-4 pt-4 border-t border-slate-200 flex items-center justify-between px-4">
                <span className="text-base font-bold text-slate-700">Tổng doanh thu</span>
                <span className="text-xl font-bold text-emerald-600">
                  {money.format(totalRevenue)}
                </span>
              </div>
            </div>

            {/* Expense Section */}
            <div>
              <h3 className="text-lg font-bold text-slate-800 mb-4 pb-2 border-b-2 border-rose-600 flex items-center gap-2">
                <svg className="w-5 h-5 text-rose-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z"
                  />
                </svg>
                CHI PHÍ
              </h3>
              <div className="space-y-2">
                {expenses.length === 0 ? (
                  <p className="text-sm text-slate-400 py-4">Không có dữ liệu chi phí</p>
                ) : (
                  expenses.map((item, index) => (
                    <div
                      key={item.accountCode || index}
                      className="flex items-center justify-between py-3 px-4 hover:bg-rose-50 rounded-lg transition"
                    >
                      <div className="flex-1">
                        <div className="text-sm font-semibold text-slate-700">
                          {item.accountName || item.category || item.name}
                        </div>
                        <div className="text-xs text-slate-500 font-mono">
                          TK: {item.accountCode || item.account || "-"}
                        </div>
                      </div>
                      <div className="text-base font-bold text-rose-600">
                        {money.format(
                          item.amount ??
                            item.totalAmount ??
                            item.value ??
                            0
                        )}
                      </div>
                    </div>
                  ))
                )}
              </div>
              <div className="mt-4 pt-4 border-t border-slate-200 flex items-center justify-between px-4">
                <span className="text-base font-bold text-slate-700">Tổng chi phí</span>
                <span className="text-xl font-bold text-rose-600">
                  {money.format(totalExpense)}
                </span>
              </div>
            </div>

            {/* Net Income Section */}
            <div className="mt-8 pt-6 border-t-4 border-slate-300">
              <div className="bg-gradient-to-r from-indigo-50 to-slate-50 rounded-2xl p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <div className="text-xs font-semibold uppercase tracking-wide text-slate-500 mb-1">
                      Lợi nhuận ròng / Lỗ
                    </div>
                    <div className="text-2xl font-bold text-slate-800">Net Income / Loss</div>
                  </div>
                  <div className="text-right">
                    <div
                      className={`text-4xl font-bold ${
                        netIncome >= 0 ? "text-emerald-600" : "text-rose-600"
                      }`}
                    >
                      {money.format(netIncome)}
                    </div>
                    {netIncome >= 0 ? (
                      <div className="mt-1 inline-flex items-center gap-1 rounded-full bg-emerald-100 px-3 py-1 text-xs font-semibold text-emerald-700">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6"
                          />
                        </svg>
                        Lãi
                      </div>
                    ) : (
                      <div className="mt-1 inline-flex items-center gap-1 rounded-full bg-rose-100 px-3 py-1 text-xs font-semibold text-rose-700">
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M13 17h8m0 0V9m0 8l-8-8-4 4-6-6"
                          />
                        </svg>
                        Lỗ
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>

            {/* Summary Stats */}
            <div className="grid grid-cols-3 gap-4 mt-6">
              <div className="rounded-xl bg-emerald-50 border border-emerald-200 p-4">
                <div className="text-xs font-semibold uppercase tracking-wide text-emerald-600 mb-1">
                  Tổng thu
                </div>
                <div className="text-2xl font-bold text-emerald-700">{money.format(totalRevenue)}</div>
              </div>
              <div className="rounded-xl bg-rose-50 border border-rose-200 p-4">
                <div className="text-xs font-semibold uppercase tracking-wide text-rose-600 mb-1">
                  Tổng chi
                </div>
                <div className="text-2xl font-bold text-rose-700">{money.format(totalExpense)}</div>
              </div>
              <div
                className={`rounded-xl p-4 ${
                  netIncome >= 0
                    ? "bg-indigo-50 border border-indigo-200"
                    : "bg-amber-50 border border-amber-200"
                }`}
              >
                <div
                  className={`text-xs font-semibold uppercase tracking-wide mb-1 ${
                    netIncome >= 0 ? "text-indigo-600" : "text-amber-600"
                  }`}
                >
                  Biên lợi nhuận
                </div>
                <div
                  className={`text-2xl font-bold ${
                    netIncome >= 0 ? "text-indigo-700" : "text-amber-700"
                  }`}
                >
                  {totalRevenue > 0 ? ((netIncome / totalRevenue) * 100).toFixed(1) : "0"}%
                </div>
              </div>
            </div>
          </div>

          {/* Footer */}
          <div className="px-8 py-4 bg-slate-50 text-xs text-slate-500 text-center border-t border-slate-200">
            Báo cáo được tạo tự động bởi hệ thống SAMS
          </div>
        </div>
      )}
    </div>
  );
}

