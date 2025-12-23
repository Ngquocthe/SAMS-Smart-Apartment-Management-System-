import React, { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import { listInvoices } from "../../../features/accountant/invoiceApi";
import { StatusBadge } from "./InvoiceForm";
import api from "../../../lib/apiClient";

const statusFilters = [
  { value: "", label: "Tất cả trạng thái" },
  { value: "DRAFT", label: "Nháp" },
  { value: "ISSUED", label: "Đã phát hành" },
  { value: "OVERDUE", label: "Quá hạn" },
  { value: "PAID", label: "Đã thanh toán" },
  { value: "CANCELLED", label: "Đã huỷ" },
];

const money = new Intl.NumberFormat("vi-VN", {
  style: "currency",
  currency: "VND",
  maximumFractionDigits: 0,
});

const formatDate = (value) => {
  if (!value) return "-";
  if (value instanceof Date && !Number.isNaN(value.getTime())) {
    return value.toLocaleDateString();
  }
  const str = value.toString();
  const parts = str.split("T")[0]?.split("-");
  if (parts?.length === 3) {
    return `${parts[2]}/${parts[1]}/${parts[0]}`;
  }
  return str;
};

const initialFilters = {
  search: "",
  status: "",
  dueFrom: "",
  dueTo: "",
  sortBy: "DueDate",
  sortDir: "desc",
  page: 1,
  pageSize: 10,
};

export default function InvoiceListPage() {
  const [filters, setFilters] = useState(initialFilters);
  const [searchInput, setSearchInput] = useState("");
  const [data, setData] = useState({ items: [], totalItems: 0, page: 1, pageSize: 10, totalPages: 1 });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [actionMsg, setActionMsg] = useState(null);
  const [apartments, setApartments] = useState([]);
  const navigate = useNavigate();

  const totalPages = useMemo(() => {
    if (data.totalPages) return data.totalPages;
    if (!data.totalItems || !data.pageSize) return 1;
    return Math.max(1, Math.ceil(data.totalItems / data.pageSize));
  }, [data.totalItems, data.pageSize, data.totalPages]);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const res = await listInvoices({
        search: filters.search || undefined,
        status: filters.status || undefined,
        dueFrom: filters.dueFrom || undefined,
        dueTo: filters.dueTo || undefined,
        sortBy: filters.sortBy,
        sortDir: filters.sortDir,
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
          "Không thể tải danh sách hoá đơn"
      );
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    load();
  }, [load]);

  useEffect(() => {
    const fetchApartments = async () => {
      try {
        const response = await api.get("/Apartment");
        const apartmentsData = Array.isArray(response.data) ? response.data : [];
        setApartments(apartmentsData);
      } catch (error) {
        console.error("Error fetching apartments:", error);
        setApartments([]);
      }
    };

    fetchApartments();
  }, []);

  const getApartmentName = (apartmentId) => {
    if (!apartmentId) return "-";
    const apartment = apartments.find((apt) => apt.apartmentId === apartmentId);
    if (apartment) {
      return `${apartment.floorNumber ? `Tầng ${apartment.floorNumber} - ` : ""}${apartment.number || apartmentId}`;
    }
    return apartmentId;
  };

  const onSearchSubmit = (event) => {
    event.preventDefault();
    setFilters((prev) => ({ ...prev, search: searchInput.trim(), page: 1 }));
  };

  const onReset = () => {
    setSearchInput("");
    setFilters(initialFilters);
    setActionMsg(null);
  };

  const onChangeFilter = (field, value) => {
    setFilters((prev) => ({ ...prev, [field]: value, page: 1 }));
  };

  const onChangePage = (nextPage) => {
    setFilters((prev) => ({ ...prev, page: nextPage }));
  };

  const onToggleSort = (field) => {
    setFilters((prev) => ({
      ...prev,
      sortBy: field,
      sortDir: prev.sortBy === field && prev.sortDir === "desc" ? "asc" : "desc",
      page: 1,
    }));
  };

  const onOpenCreate = () => {
    navigate(ROUTER_PAGE.ACCOUNTANT.INVOICE_CREATE);
  };

  const onView = (invoiceId) => {
    navigate(ROUTER_PAGE.ACCOUNTANT.INVOICE_VIEW.replace(":id", invoiceId));
  };
  const onEdit = (invoiceId) => {
    navigate(ROUTER_PAGE.ACCOUNTANT.INVOICE_EDIT.replace(":id", invoiceId));
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-slate-800">Hoá đơn</h1>
          <p className="text-sm text-slate-500">
            Quản lý chu kỳ thu phí, theo dõi hạn thanh toán và dòng tiền.
          </p>
        </div>
        <button
          onClick={onOpenCreate}
          className="inline-flex items-center justify-center gap-2 rounded-full bg-indigo-600 px-6 py-2.5 text-sm font-semibold text-white shadow-lg shadow-indigo-400/30 transition hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
        >
          <span className="text-lg">＋</span>
          Tạo hoá đơn
        </button>
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
            placeholder="Mã hoá đơn, ghi chú..."
          />
        </div>
        <div>
          <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">Trạng thái</label>
          <select
            value={filters.status}
            onChange={(e) => setFilters((prev) => ({ ...prev, status: e.target.value, page: 1 }))}
            className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
          >
            {statusFilters.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">Hạn thanh toán từ</label>
          <input
            type="date"
            value={filters.dueFrom}
            onChange={(e) => onChangeFilter("dueFrom", e.target.value)}
            className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
          />
        </div>
        <div>
          <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">Hạn thanh toán đến</label>
          <input
            type="date"
            value={filters.dueTo}
            onChange={(e) => onChangeFilter("dueTo", e.target.value)}
            className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
          />
        </div>
        <div className="lg:col-span-2 flex items-end gap-2">
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

      <div className="rounded-3xl border border-slate-200 bg-white shadow-lg shadow-slate-200/40 overflow-hidden">
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Tổng quan</p>
            <h2 className="text-lg font-semibold text-slate-700">
              {loading ? "Đang tải hoá đơn..." : `${data.totalItems} hoá đơn`}
            </h2>
          </div>
          <div className="flex items-center gap-2 text-xs text-slate-500">
            <span className="font-medium">Sắp xếp theo:</span>
            <button
              type="button"
              onClick={() => onToggleSort("DueDate")}
              className={`rounded-full px-3 py-1 transition ${
                filters.sortBy === "DueDate" ? "bg-indigo-100 text-indigo-600" : "hover:bg-slate-100"
              }`}
            >
              Hạn thanh toán {filters.sortBy === "DueDate" ? (filters.sortDir === "desc" ? "↓" : "↑") : ""}
            </button>
            <button
              type="button"
              onClick={() => onToggleSort("IssueDate")}
              className={`rounded-full px-3 py-1 transition ${
                filters.sortBy === "IssueDate" ? "bg-indigo-100 text-indigo-600" : "hover:bg-slate-100"
              }`}
            >
              Ngày phát hành {filters.sortBy === "IssueDate" ? (filters.sortDir === "desc" ? "↓" : "↑") : ""}
            </button>
            <button
              type="button"
              onClick={() => onToggleSort("TotalAmount")}
              className={`rounded-full px-3 py-1 transition ${
                filters.sortBy === "TotalAmount" ? "bg-indigo-100 text-indigo-600" : "hover:bg-slate-100"
              }`}
            >
              Số tiền {filters.sortBy === "TotalAmount" ? (filters.sortDir === "desc" ? "↓" : "↑") : ""}
            </button>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-slate-100 text-sm">
            <thead className="bg-slate-50/70">
              <tr>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">Hoá đơn</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">Căn hộ</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">Ngày tháng</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">Số tiền</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">Trạng thái</th>
                <th className="px-6 py-3 text-right font-semibold text-slate-600">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 bg-white">
              {loading ? (
                <tr>
                  <td colSpan={6} className="px-6 py-12 text-center text-slate-500">
                    Đang tải danh sách hoá đơn...
                  </td>
                </tr>
              ) : data.items.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-12 text-center text-slate-400">
                    Không có hoá đơn nào phù hợp bộ lọc hiện tại.
                  </td>
                </tr>
              ) : (
                data.items.map((invoice) => (
                  <tr key={invoice.invoiceId} className="group hover:bg-slate-50/70 transition">
                    <td className="px-6 py-4 align-top">
                      <div className="font-semibold text-slate-700">{invoice.invoiceNo}</div>
                      <div className="text-xs text-slate-400 mt-0.5">
                        Tạo lúc {formatDate(invoice.createdAt)}
                      </div>
                    </td>
                    <td className="px-6 py-4 align-top">
                      <div className="text-sm font-medium text-slate-700">
                        {getApartmentName(invoice.apartmentId)}
                      </div>
                    </td>
                    <td className="px-6 py-4 align-top text-sm text-slate-600">
                      <div>
                        <span className="text-xs uppercase text-slate-400">Phát hành</span>
                        <div>{formatDate(invoice.issueDate)}</div>
                      </div>
                      <div className="mt-2">
                        <span className="text-xs uppercase text-slate-400">Hạn</span>
                        <div className="font-medium text-slate-700">{formatDate(invoice.dueDate)}</div>
                      </div>
                    </td>
                    <td className="px-6 py-4 align-top">
                      <div className="text-sm font-semibold text-slate-700">
                        {money.format(invoice.totalAmount ?? 0)}
                      </div>
                      <div className="text-xs text-slate-400">
                        Thuế VAT {money.format(invoice.taxAmount ?? 0)}
                      </div>
                    </td>
                    <td className="px-6 py-4 align-top">
                      <StatusBadge value={invoice.status} />
                    </td>
                    <td className="px-6 py-4 align-top text-right">
                      <div className="inline-flex items-center gap-2">
                        <button
                          onClick={() => onView(invoice.invoiceId)}
                          className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-xs font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
                        >
                          Xem
                        </button>
                        {invoice.status === "DRAFT" && (
                          <button
                            onClick={() => onEdit(invoice.invoiceId)}
                            className="inline-flex items-center gap-2 rounded-full bg-slate-900 px-4 py-2 text-xs font-semibold text-white shadow hover:bg-black"
                          >
                            Sửa
                          </button>
                        )}
                      </div>
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
