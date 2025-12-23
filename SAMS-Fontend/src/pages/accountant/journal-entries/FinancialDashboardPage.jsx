import React, { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import { getFinancialDashboard } from "../../../features/accountant/journalEntryApi";

const money = new Intl.NumberFormat("vi-VN", {
  style: "currency",
  currency: "VND",
  maximumFractionDigits: 0,
});

export default function FinancialDashboardPage() {
  const navigate = useNavigate();
  const [period, setPeriod] = useState("month"); // month, quarter, year
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const res = await getFinancialDashboard({
        period: period || undefined,
      });
      console.log("📥 Financial Dashboard data:", res);
      setData(res);
    } catch (err) {
      setError(
        err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          "Không thể tải dashboard tài chính"
      );
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [period]);

  useEffect(() => {
    load();
  }, [load]);

  const handleBackToList = () => {
    navigate(ROUTER_PAGE.ACCOUNTANT.JOURNAL_ENTRIES);
  };

  const handleRefresh = () => {
    load();
  };

  const periodLabels = {
    month: "Tháng này",
    quarter: "Quý này",
    year: "Năm nay",
  };

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
          <h1 className="text-3xl font-bold text-slate-800">Dashboard tài chính</h1>
          <p className="text-sm text-slate-500">
            Tổng quan tình hình tài chính, dòng tiền và các chỉ số quan trọng.
          </p>
        </div>
        <button
          onClick={handleRefresh}
          disabled={loading}
          className="inline-flex items-center gap-2 rounded-full bg-slate-900 px-4 py-2 text-sm font-semibold text-white shadow hover:bg-black focus:outline-none focus:ring-2 focus:ring-slate-500 focus:ring-offset-2 disabled:bg-slate-400"
        >
          <svg
            className={`w-4 h-4 ${loading ? "animate-spin" : ""}`}
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
            />
          </svg>
          {loading ? "Đang tải..." : "Làm mới"}
        </button>
      </div>

      {/* Period Selector */}
      <div className="flex items-center gap-2 rounded-3xl border border-slate-200 bg-white/80 p-2 shadow-sm backdrop-blur w-fit">
        {["month", "quarter", "year"].map((p) => (
          <button
            key={p}
            onClick={() => setPeriod(p)}
            className={`rounded-full px-6 py-2 text-sm font-semibold transition ${
              period === p
                ? "bg-indigo-600 text-white shadow-lg shadow-indigo-400/30"
                : "text-slate-600 hover:bg-slate-100"
            }`}
          >
            {periodLabels[p]}
          </button>
        ))}
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
            <p className="text-slate-600">Đang tải dashboard...</p>
          </div>
        </div>
      )}

      {!loading && data && (
        <>
          {/* Key Metrics */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            {/* Total Revenue */}
            <div className="rounded-2xl border border-slate-200 bg-gradient-to-br from-emerald-50 to-white p-6 shadow-lg">
              <div className="flex items-start justify-between mb-4">
                <div className="p-3 rounded-xl bg-emerald-100">
                  <svg className="w-6 h-6 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                    />
                  </svg>
                </div>
                {data.revenueGrowth && (
                  <span
                    className={`text-xs font-semibold ${
                      data.revenueGrowth >= 0 ? "text-emerald-600" : "text-rose-600"
                    }`}
                  >
                    {data.revenueGrowth >= 0 ? "+" : ""}
                    {data.revenueGrowth.toFixed(1)}%
                  </span>
                )}
              </div>
              <div className="text-sm font-semibold text-slate-500 mb-1">Tổng doanh thu</div>
              <div className="text-2xl font-bold text-slate-800">
                {money.format(data.totalRevenue || 0)}
              </div>
            </div>

            {/* Total Expense */}
            <div className="rounded-2xl border border-slate-200 bg-gradient-to-br from-rose-50 to-white p-6 shadow-lg">
              <div className="flex items-start justify-between mb-4">
                <div className="p-3 rounded-xl bg-rose-100">
                  <svg className="w-6 h-6 text-rose-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z"
                    />
                  </svg>
                </div>
                {data.expenseGrowth && (
                  <span
                    className={`text-xs font-semibold ${
                      data.expenseGrowth <= 0 ? "text-emerald-600" : "text-rose-600"
                    }`}
                  >
                    {data.expenseGrowth >= 0 ? "+" : ""}
                    {data.expenseGrowth.toFixed(1)}%
                  </span>
                )}
              </div>
              <div className="text-sm font-semibold text-slate-500 mb-1">Tổng chi phí</div>
              <div className="text-2xl font-bold text-slate-800">
                {money.format(data.totalExpense || 0)}
              </div>
            </div>

            {/* Net Income */}
            <div className="rounded-2xl border border-slate-200 bg-gradient-to-br from-indigo-50 to-white p-6 shadow-lg">
              <div className="flex items-start justify-between mb-4">
                <div className="p-3 rounded-xl bg-indigo-100">
                  <svg className="w-6 h-6 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z"
                    />
                  </svg>
                </div>
              </div>
              <div className="text-sm font-semibold text-slate-500 mb-1">Lợi nhuận ròng</div>
              <div
                className={`text-2xl font-bold ${
                  (data.netIncome || 0) >= 0 ? "text-emerald-600" : "text-rose-600"
                }`}
              >
                {money.format(data.netIncome || 0)}
              </div>
            </div>

            {/* Cash Balance */}
            <div className="rounded-2xl border border-slate-200 bg-gradient-to-br from-amber-50 to-white p-6 shadow-lg">
              <div className="flex items-start justify-between mb-4">
                <div className="p-3 rounded-xl bg-amber-100">
                  <svg className="w-6 h-6 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"
                    />
                  </svg>
                </div>
              </div>
              <div className="text-sm font-semibold text-slate-500 mb-1">Số dư tiền mặt</div>
              <div className="text-2xl font-bold text-slate-800">
                {money.format(data.cashBalance || 0)}
              </div>
            </div>
          </div>

          {/* Charts and Details */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Top Revenue Sources */}
            <div className="rounded-2xl border border-slate-200 bg-white shadow-lg overflow-hidden">
              <div className="px-6 py-4 bg-slate-50 border-b border-slate-200">
                <h3 className="text-lg font-bold text-slate-800">Nguồn doanh thu hàng đầu</h3>
              </div>
              <div className="p-6 space-y-3">
                {data.topRevenueAccounts && data.topRevenueAccounts.length > 0 ? (
                  data.topRevenueAccounts.map((account, index) => (
                    <div key={account.accountCode || index} className="flex items-center gap-4">
                      <div className="flex-shrink-0 w-8 h-8 rounded-full bg-emerald-100 flex items-center justify-center text-sm font-bold text-emerald-600">
                        {index + 1}
                      </div>
                      <div className="flex-1">
                        <div className="text-sm font-semibold text-slate-700">
                          {account.accountName || account.name}
                        </div>
                        <div className="text-xs text-slate-500">TK: {account.accountCode}</div>
                      </div>
                      <div className="text-right">
                        <div className="text-sm font-bold text-emerald-600">
                          {money.format(account.amount || 0)}
                        </div>
                        {data.totalRevenue > 0 && (
                          <div className="text-xs text-slate-500">
                            {((account.amount / data.totalRevenue) * 100).toFixed(1)}%
                          </div>
                        )}
                      </div>
                    </div>
                  ))
                ) : data.topRevenueSources && data.topRevenueSources.length > 0 ? (
                  data.topRevenueSources.map((source, index) => (
                    <div key={source.source || index} className="flex items-center gap-4">
                      <div className="flex-shrink-0 w-8 h-8 rounded-full bg-emerald-100 flex items-center justify-center text-sm font-bold text-emerald-600">
                        {index + 1}
                      </div>
                      <div className="flex-1">
                        <div className="text-sm font-semibold text-slate-700">
                          {source.source}
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="text-sm font-bold text-emerald-600">
                          {money.format(source.amount || 0)}
                        </div>
                        {data.totalRevenue > 0 && (
                          <div className="text-xs text-slate-500">
                            {((source.amount / data.totalRevenue) * 100).toFixed(1)}%
                          </div>
                        )}
                      </div>
                    </div>
                  ))
                ) : (
                  <p className="text-center text-slate-400 py-8">Không có dữ liệu</p>
                )}
              </div>
            </div>

            {/* Top Expenses */}
            <div className="rounded-2xl border border-slate-200 bg-white shadow-lg overflow-hidden">
              <div className="px-6 py-4 bg-slate-50 border-b border-slate-200">
                <h3 className="text-lg font-bold text-slate-800">Chi phí hàng đầu</h3>
              </div>
              <div className="p-6 space-y-3">
                {data.topExpenseAccounts && data.topExpenseAccounts.length > 0 ? (
                  data.topExpenseAccounts.map((account, index) => (
                    <div key={account.accountCode || index} className="flex items-center gap-4">
                      <div className="flex-shrink-0 w-8 h-8 rounded-full bg-rose-100 flex items-center justify-center text-sm font-bold text-rose-600">
                        {index + 1}
                      </div>
                      <div className="flex-1">
                        <div className="text-sm font-semibold text-slate-700">
                          {account.accountName || account.name}
                        </div>
                        <div className="text-xs text-slate-500">TK: {account.accountCode}</div>
                      </div>
                      <div className="text-right">
                        <div className="text-sm font-bold text-rose-600">
                          {money.format(account.amount || 0)}
                        </div>
                        {data.totalExpense > 0 && (
                          <div className="text-xs text-slate-500">
                            {((account.amount / data.totalExpense) * 100).toFixed(1)}%
                          </div>
                        )}
                      </div>
                    </div>
                  ))
                ) : data.topExpenseSources && data.topExpenseSources.length > 0 ? (
                  data.topExpenseSources.map((source, index) => (
                    <div key={source.source || index} className="flex items-center gap-4">
                      <div className="flex-shrink-0 w-8 h-8 rounded-full bg-rose-100 flex items-center justify-center text-sm font-bold text-rose-600">
                        {index + 1}
                      </div>
                      <div className="flex-1">
                        <div className="text-sm font-semibold text-slate-700">
                          {source.source}
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="text-sm font-bold text-rose-600">
                          {money.format(source.amount || 0)}
                        </div>
                        {data.totalExpense > 0 && (
                          <div className="text-xs text-slate-500">
                            {((source.amount / data.totalExpense) * 100).toFixed(1)}%
                          </div>
                        )}
                      </div>
                    </div>
                  ))
                ) : (
                  <p className="text-center text-slate-400 py-8">Không có dữ liệu</p>
                )}
              </div>
            </div>
          </div>

          {/* Financial Ratios */}
          <div className="rounded-2xl border border-slate-200 bg-white shadow-lg overflow-hidden">
            <div className="px-6 py-4 bg-slate-50 border-b border-slate-200">
              <h3 className="text-lg font-bold text-slate-800">Các chỉ số tài chính</h3>
            </div>
            <div className="p-6 grid grid-cols-1 md:grid-cols-3 gap-6">
              <div className="text-center">
                <div className="text-3xl font-bold text-indigo-600">
                  {data.totalRevenue > 0
                    ? (((data.netIncome || 0) / data.totalRevenue) * 100).toFixed(1)
                    : "0"}
                  %
                </div>
                <div className="text-sm font-semibold text-slate-600 mt-2">Biên lợi nhuận ròng</div>
                <div className="text-xs text-slate-500 mt-1">Net Profit Margin</div>
              </div>
              <div className="text-center">
              <div className="text-3xl font-bold text-emerald-600">
                {data.totalExpense > 0
                  ? ((data.totalRevenue / data.totalExpense) * 100).toFixed(1)
                  : "0"}
                %
              </div>
                <div className="text-sm font-semibold text-slate-600 mt-2">Tỷ lệ doanh thu/chi phí</div>
                <div className="text-xs text-slate-500 mt-1">Revenue to Expense Ratio</div>
              </div>
              <div className="text-center">
                <div className="text-3xl font-bold text-amber-600">
                  {data.cashBalance ? money.format(data.cashBalance).replace("₫", "") : "0"}
                </div>
                <div className="text-sm font-semibold text-slate-600 mt-2">Tiền mặt hiện có</div>
                <div className="text-xs text-slate-500 mt-1">Available Cash</div>
              </div>
            </div>
          </div>

          {/* Quick Actions */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <button
              onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.INCOME_STATEMENT)}
              className="flex items-center gap-4 rounded-2xl border border-slate-200 bg-white p-6 shadow hover:shadow-lg transition"
            >
              <div className="p-3 rounded-xl bg-emerald-100">
                <svg className="w-6 h-6 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z"
                  />
                </svg>
              </div>
              <div className="flex-1 text-left">
                <div className="font-semibold text-slate-800">Báo cáo thu chi</div>
                <div className="text-xs text-slate-500">Xem chi tiết</div>
              </div>
            </button>
            <button
              onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.BALANCE_SHEET)}
              className="flex items-center gap-4 rounded-2xl border border-slate-200 bg-white p-6 shadow hover:shadow-lg transition"
            >
              <div className="p-3 rounded-xl bg-indigo-100">
                <svg className="w-6 h-6 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
                  />
                </svg>
              </div>
              <div className="flex-1 text-left">
                <div className="font-semibold text-slate-800">Bảng cân đối</div>
                <div className="text-xs text-slate-500">Xem chi tiết</div>
              </div>
            </button>
            <button
              onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.GENERAL_LEDGER)}
              className="flex items-center gap-4 rounded-2xl border border-slate-200 bg-white p-6 shadow hover:shadow-lg transition"
            >
              <div className="p-3 rounded-xl bg-amber-100">
                <svg className="w-6 h-6 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"
                  />
                </svg>
              </div>
              <div className="flex-1 text-left">
                <div className="font-semibold text-slate-800">Sổ cái</div>
                <div className="text-xs text-slate-500">Xem chi tiết</div>
              </div>
            </button>
          </div>
        </>
      )}
    </div>
  );
}

