import React, { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import { listJournalEntries } from "../../../features/accountant/journalEntryApi";

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

const initialFilters = {
  search: "",
  accountCode: "",
  dateFrom: "",
  dateTo: "",
  page: 1,
  pageSize: 20,
};

export default function JournalEntryListPage() {
  const navigate = useNavigate();
  const [filters, setFilters] = useState(initialFilters);
  const [searchInput, setSearchInput] = useState("");
  const [data, setData] = useState({ items: [], totalItems: 0, page: 1, pageSize: 20, totalPages: 1 });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const totalPages = useMemo(() => {
    if (data.totalPages) return data.totalPages;
    if (!data.totalItems || !data.pageSize) return 1;
    return Math.max(1, Math.ceil(data.totalItems / data.pageSize));
  }, [data.totalItems, data.pageSize, data.totalPages]);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const res = await listJournalEntries({
        search: filters.search || undefined,
        accountCode: filters.accountCode || undefined,
        dateFrom: filters.dateFrom || undefined,
        dateTo: filters.dateTo || undefined,
        page: filters.page,
        pageSize: filters.pageSize,
      });

      const totalItems =
        res?.totalItems ??
        res?.total ??
        res?.totalCount ??
        (Array.isArray(res?.items) ? res.items.length : 0);

      setData({
        items: res?.items ?? [],
        totalItems,
        page: res?.page ?? filters.page,
        pageSize: res?.pageSize ?? filters.pageSize,
        totalPages: res?.totalPages ?? null,
      });
    } catch (err) {
      setError(
        err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          "Không thể tải danh sách bút toán"
      );
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    load();
  }, [load]);

  const onSearchSubmit = (event) => {
    event.preventDefault();
    const trimmedSearch = searchInput.trim();
    setFilters((prev) => ({ ...prev, search: trimmedSearch, page: 1 }));
  };

  const onReset = () => {
    setSearchInput("");
    setFilters(initialFilters);
  };

  const onChangeFilter = (field, value) => {
    setFilters((prev) => ({ ...prev, [field]: value, page: 1 }));
  };

  const onChangePage = (nextPage) => {
    setFilters((prev) => ({ ...prev, page: nextPage }));
  };

  const onViewEntry = (entryId) => {
    navigate(ROUTER_PAGE.ACCOUNTANT.JOURNAL_ENTRY_VIEW.replace(":id", entryId));
  };

  const getEntryTypeLabel = (type) => {
    const typeMap = {
      RECEIPT: "Phiếu thu",
      VOUCHER: "Phiếu chi",
      PAYMENT: "Phiếu chi",
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
      PAYMENT: "bg-rose-100 text-rose-700",
      INVOICE: "bg-blue-100 text-blue-700",
      ADJUSTMENT: "bg-amber-100 text-amber-700",
      OTHER: "bg-slate-100 text-slate-700",
    };
    return colorMap[type] || "bg-slate-100 text-slate-700";
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-slate-800">Sổ nhật ký chung</h1>
          <p className="text-sm text-slate-500">
            Quản lý các bút toán kế toán, theo dõi ghi sổ và kiểm tra số liệu tài chính.
          </p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.GENERAL_LEDGER)}
            className="inline-flex items-center justify-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
          >
            📖 Sổ cái
          </button>
          <button
            onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.BALANCE_SHEET)}
            className="inline-flex items-center justify-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
          >
            📊 Bảng cân đối
          </button>
          <button
            onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.INCOME_STATEMENT)}
            className="inline-flex items-center justify-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
          >
            💰 Báo cáo thu chi
          </button>
        </div>
      </div>

      <form
        onSubmit={onSearchSubmit}
        className="grid grid-cols-1 lg:grid-cols-6 gap-4 rounded-3xl border border-slate-200 bg-white/80 p-5 shadow-sm backdrop-blur"
      >
        <div className="lg:col-span-2">
          <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">Tìm kiếm</label>
          <input
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
            placeholder="Mã bút toán, diễn giải..."
          />
        </div>
        <div>
          <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">Mã tài khoản</label>
          <input
            value={filters.accountCode}
            onChange={(e) => onChangeFilter("accountCode", e.target.value)}
            className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
            placeholder="Ví dụ: 111, 512..."
          />
        </div>
        <div>
          <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">Từ ngày</label>
          <input
            type="date"
            value={filters.dateFrom}
            onChange={(e) => onChangeFilter("dateFrom", e.target.value)}
            className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
          />
        </div>
        <div>
          <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">Đến ngày</label>
          <input
            type="date"
            value={filters.dateTo}
            onChange={(e) => onChangeFilter("dateTo", e.target.value)}
            className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
          />
        </div>
        <div className="flex items-end gap-2">
          <button
            type="submit"
            className="inline-flex items-center justify-center rounded-xl bg-slate-900 px-5 py-2.5 text-sm font-semibold text-white shadow hover:bg-black focus:outline-none focus:ring-2 focus:ring-slate-500 focus:ring-offset-2"
          >
            Tìm kiếm
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

      <div className="rounded-3xl border border-slate-200 bg-white shadow-lg shadow-slate-200/40 overflow-hidden">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Tổng quan</p>
            <h2 className="text-lg font-semibold text-slate-700">
              {loading ? "Đang tải..." : `${data.totalItems} bút toán`}
            </h2>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-slate-100 text-sm">
            <thead className="bg-slate-50/70">
              <tr>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">Ngày</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">Mã bút toán</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">Loại</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">Diễn giải</th>
                <th className="px-6 py-3 text-right font-semibold text-slate-600">Nợ</th>
                <th className="px-6 py-3 text-right font-semibold text-slate-600">Có</th>
                <th className="px-6 py-3 text-right font-semibold text-slate-600">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 bg-white">
              {loading ? (
                <tr>
                  <td colSpan={7} className="px-6 py-12 text-center text-slate-500">
                    Đang tải bút toán...
                  </td>
                </tr>
              ) : data.items.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-6 py-12 text-center text-slate-400">
                    Không có bút toán nào phù hợp với bộ lọc hiện tại.
                  </td>
                </tr>
              ) : (
                data.items.map((entry) => (
                  <tr key={entry.journalEntryId || entry.id} className="group hover:bg-slate-50/70 transition">
                    <td className="px-6 py-4 align-top">
                      <div className="text-sm text-slate-600">
                        {formatDate(entry.entryDate || entry.date || entry.createdAt)}
                      </div>
                    </td>
                    <td className="px-6 py-4 align-top">
                      <div className="font-semibold text-slate-700">
                        {entry.entryNo || entry.journalEntryId || entry.id}
                      </div>
                    </td>
                    <td className="px-6 py-4 align-top">
                      <span
                        className={`inline-flex items-center rounded-full px-3 py-1 text-xs font-medium ${getEntryTypeColor(
                          entry.entryType || entry.type
                        )}`}
                      >
                        {getEntryTypeLabel(entry.entryType || entry.type)}
                      </span>
                    </td>
                    <td className="px-6 py-4 align-top">
                      <div className="text-sm text-slate-700 max-w-md truncate">
                        {entry.description || entry.note || "-"}
                      </div>
                    </td>
                    <td className="px-6 py-4 align-top text-right">
                      <div className="text-sm font-semibold text-emerald-600">
                        {entry.debitAmount ? money.format(entry.debitAmount) : "-"}
                      </div>
                    </td>
                    <td className="px-6 py-4 align-top text-right">
                      <div className="text-sm font-semibold text-rose-600">
                        {entry.creditAmount ? money.format(entry.creditAmount) : "-"}
                      </div>
                    </td>
                    <td className="px-6 py-4 align-top text-right">
                      <button
                        onClick={() => onViewEntry(entry.journalEntryId || entry.id)}
                        className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-xs font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
                      >
                        Xem
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3 border-t border-slate-100 px-6 py-4 text-sm text-slate-600">
          <div>
            Trang {data.page} / {totalPages}
          </div>
          <div className="flex items-center gap-2">
            <button
              disabled={data.page <= 1}
              onClick={() => onChangePage(Math.max(1, data.page - 1))}
              className="rounded-full border border-slate-300 px-4 py-1.5 transition disabled:opacity-40 disabled:cursor-not-allowed hover:border-indigo-400 hover:text-indigo-600"
            >
              Trước
            </button>
            <button
              disabled={data.page >= totalPages}
              onClick={() => onChangePage(Math.min(totalPages, data.page + 1))}
              className="rounded-full border border-slate-300 px-4 py-1.5 transition disabled:opacity-40 disabled:cursor-not-allowed hover:border-indigo-400 hover:text-indigo-600"
            >
              Sau
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

