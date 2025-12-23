import React, { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import { getBalanceSheet } from "../../../features/accountant/journalEntryApi";

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

export default function BalanceSheetPage() {
  const navigate = useNavigate();
  const [period, setPeriod] = useState(() => {
    const today = new Date();
    const month = String(today.getMonth() + 1).padStart(2, "0");
    return `${today.getFullYear()}-${month}`;
  });
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const res = await getBalanceSheet({
        period: period || undefined,
      });
      console.log("📥 Balance Sheet data:", res);
      const normalized = {
        raw: res,
        period: res?.period || period,
        assets: res?.assets || res?.assetItems || res?.assetGroups || [],
        liabilities:
          res?.liabilities ||
          res?.liabilityItems ||
          res?.liabilityGroups ||
          [],
        equity: res?.equity || res?.equityItems || res?.equityGroups || [],
        openingBalance:
          res?.openingBalance ?? res?.beginBalance ?? res?.opening ?? 0,
        totalAssets:
          res?.totalAssets ??
          res?.assetsTotal ??
          res?.assetTotal ??
          null,
        totalLiabilities:
          res?.totalLiabilities ??
          res?.liabilitiesTotal ??
          res?.liabilityTotal ??
          null,
        totalEquity:
          res?.totalEquity ??
          res?.equityTotal ??
          res?.ownerEquityTotal ??
          null,
      };
      setData(normalized);
    } catch (err) {
      setError(
        err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          "Không thể tải bảng cân đối kế toán"
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

  const handlePrint = () => {
    window.print();
  };

  // Tính tổng tài sản và nguồn vốn
  const assets =
    data?.assets || data?.assetItems || data?.assetGroups || [];
  const liabilities =
    data?.liabilities ||
    data?.liabilityItems ||
    data?.liabilityGroups ||
    [];
  const equity =
    data?.equity || data?.equityItems || data?.equityGroups || [];

  const totalAssets =
    data?.totalAssets ??
    assets.reduce((sum, item) => sum + (item.balance || item.amount || 0), 0);
  const totalLiabilities =
    data?.totalLiabilities ??
    liabilities.reduce(
      (sum, item) => sum + (item.balance || item.amount || 0),
      0
    );
  const totalEquity =
    data?.totalEquity ??
    equity.reduce((sum, item) => sum + (item.balance || item.amount || 0), 0);
  const totalLiabilitiesAndEquity = totalLiabilities + totalEquity;

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
          <h1 className="text-3xl font-bold text-slate-800">Bảng cân đối kế toán</h1>
          <p className="text-sm text-slate-500">
            Báo cáo tình hình tài sản, nợ phải trả và vốn chủ sở hữu.
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
              Kỳ báo cáo (YYYY-MM)
            </label>
            <input
              type="month"
              value={period}
              onChange={(e) => setPeriod(e.target.value)}
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
            <p className="text-slate-600">Đang tải bảng cân đối kế toán...</p>
          </div>
        </div>
      )}

      {!loading && data && (
        <div className="rounded-3xl border border-slate-200 bg-white shadow-xl overflow-hidden">
          {/* Header */}
          <div className="bg-gradient-to-r from-indigo-600 to-indigo-700 px-8 py-6 text-white text-center">
            <h2 className="text-2xl font-bold mb-2">BẢNG CÂN ĐỐI KẾ TOÁN</h2>
            <p className="text-indigo-100">Balance Sheet</p>
            <div className="mt-3 text-sm">
              Kỳ báo cáo:{" "}
              <span className="font-semibold">
                {formatPeriod(data?.period || period)}
              </span>
            </div>
          </div>

          <div className="grid grid-cols-2 divide-x divide-slate-200">
            {/* Left Side: Assets */}
            <div className="p-8">
              <h3 className="text-xl font-bold text-slate-800 mb-6 pb-3 border-b-2 border-indigo-600">
                TÀI SẢN
              </h3>
              
              <div className="space-y-2">
                {assets.length === 0 ? (
                  <p className="text-sm text-slate-400 py-6">Không có dữ liệu tài sản</p>
                ) : (
                  assets.map((item, index) => (
                    <div
                      key={item.accountCode || item.account || index}
                      className="flex items-start justify-between py-2 hover:bg-slate-50 rounded px-2 -mx-2"
                    >
                      <div className="flex-1">
                        <div className="text-sm font-semibold text-slate-700">
                          {item.accountName || item.category || item.name}
                        </div>
                        <div className="text-xs text-slate-500 font-mono">
                          TK: {item.accountCode || item.account || "-"}
                        </div>
                      </div>
                      <div className="text-sm font-bold text-slate-800 text-right">
                        {money.format(
                          item.balance ?? item.amount ?? item.value ?? 0
                        )}
                      </div>
                    </div>
                  ))
                )}
              </div>

              <div className="mt-6 pt-6 border-t-2 border-slate-300">
                <div className="flex items-center justify-between">
                  <span className="text-lg font-bold text-slate-800">TỔNG TÀI SẢN</span>
                  <span className="text-xl font-bold text-indigo-600">
                    {money.format(totalAssets)}
                  </span>
                </div>
              </div>
            </div>

            {/* Right Side: Liabilities & Equity */}
            <div className="p-8 bg-slate-50/50">
              {/* Liabilities */}
              <h3 className="text-xl font-bold text-slate-800 mb-6 pb-3 border-b-2 border-rose-600">
                NỢ PHẢI TRẢ
              </h3>
              
              <div className="space-y-2 mb-8">
                {liabilities.length === 0 ? (
                  <p className="text-sm text-slate-400 py-6">Không có dữ liệu nợ phải trả</p>
                ) : (
                  liabilities.map((item, index) => (
                    <div key={item.accountCode || index} className="flex items-start justify-between py-2 hover:bg-white rounded px-2 -mx-2">
                      <div className="flex-1">
                        <div className="text-sm font-semibold text-slate-700">
                          {item.accountName || item.name}
                        </div>
                        <div className="text-xs text-slate-500 font-mono">
                          TK: {item.accountCode || "-"}
                        </div>
                      </div>
                      <div className="text-sm font-bold text-slate-800 text-right">
                        {money.format(item.balance || 0)}
                      </div>
                    </div>
                  ))
                )}
              </div>

              {/* Equity */}
              <h3 className="text-xl font-bold text-slate-800 mb-6 pb-3 border-b-2 border-emerald-600">
                VỐN CHỦ SỞ HỮU
              </h3>
              
              <div className="space-y-2">
                {equity.length === 0 ? (
                  <p className="text-sm text-slate-400 py-6">Không có dữ liệu vốn chủ sở hữu</p>
                ) : (
                  equity.map((item, index) => (
                    <div key={item.accountCode || index} className="flex items-start justify-between py-2 hover:bg-white rounded px-2 -mx-2">
                      <div className="flex-1">
                        <div className="text-sm font-semibold text-slate-700">
                          {item.accountName || item.name}
                        </div>
                        <div className="text-xs text-slate-500 font-mono">
                          TK: {item.accountCode || "-"}
                        </div>
                      </div>
                      <div className="text-sm font-bold text-slate-800 text-right">
                        {money.format(item.balance || 0)}
                      </div>
                    </div>
                  ))
                )}
              </div>

              <div className="mt-6 pt-6 border-t-2 border-slate-300">
                <div className="flex items-center justify-between">
                  <span className="text-lg font-bold text-slate-800">TỔNG NGUỒN VỐN</span>
                  <span className="text-xl font-bold text-indigo-600">
                    {money.format(totalLiabilitiesAndEquity)}
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Balance Check */}
          <div className="px-8 py-6 bg-slate-100 border-t border-slate-200">
            {Math.abs(totalAssets - totalLiabilitiesAndEquity) < 0.01 ? (
              <div className="flex items-center justify-center gap-2 text-emerald-700">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
                <span className="font-semibold">Bảng cân đối đã cân bằng</span>
              </div>
            ) : (
              <div className="flex items-center justify-center gap-2 text-amber-700">
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                  />
                </svg>
                <span className="font-semibold">
                  Cảnh báo: Tài sản và Nguồn vốn chênh lệch{" "}
                  {money.format(Math.abs(totalAssets - totalLiabilitiesAndEquity))}
                </span>
              </div>
            )}
          </div>

          {/* Footer */}
          <div className="px-8 py-4 bg-white text-xs text-slate-500 text-center border-t border-slate-200">
            Báo cáo được tạo tự động bởi hệ thống SAMS
          </div>
        </div>
      )}
    </div>
  );
}

