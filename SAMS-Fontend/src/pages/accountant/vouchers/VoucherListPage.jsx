import React, { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import {
  deleteVoucher,
  listVouchers,
  updateVoucherStatus,
} from "../../../features/accountant/voucherApi";

const statusFilters = [
  { value: "", label: "Tất cả trạng thái" },
  { value: "DRAFT", label: "Nháp" },
  { value: "PENDING", label: "Chờ duyệt" },
  { value: "APPROVED", label: "Đã duyệt" },
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
    return value.toLocaleDateString("vi-VN");
  }
  const str = value.toString();
  const parts = str.split("T")[0]?.split("-");
  if (parts?.length === 3) {
    return `${parts[2]}/${parts[1]}/${parts[0]}`;
  }
  return str;
};

const STATUS_COLORS = {
  DRAFT: "bg-slate-100 text-slate-700",
  PENDING: "bg-amber-100 text-amber-700",
  APPROVED: "bg-emerald-100 text-emerald-700",
  CANCELLED: "bg-rose-100 text-rose-700",
};

const STATUS_LABELS = {
  DRAFT: "Nháp",
  PENDING: "Chờ duyệt",
  APPROVED: "Đã duyệt",
  CANCELLED: "Đã huỷ",
};

const initialFilters = {
  search: "",
  status: "",
  dateFrom: "",
  dateTo: "",
  sortBy: "Date",
  sortDir: "desc",
  page: 1,
  pageSize: 10,
};

export default function VoucherListPage() {
  const navigate = useNavigate();
  const [filters, setFilters] = useState(initialFilters);
  const [searchInput, setSearchInput] = useState("");
  const [data, setData] = useState({
    items: [],
    totalItems: 0,
    page: 1,
    pageSize: 10,
    totalPages: 1,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [actionMsg, setActionMsg] = useState(null);
  const [processing, setProcessing] = useState({ id: null, action: null });

  const totalPages = useMemo(() => {
    if (data.totalPages) return data.totalPages;
    if (!data.totalItems || !data.pageSize) return 1;
    return Math.max(1, Math.ceil(data.totalItems / data.pageSize));
  }, [data.totalItems, data.pageSize, data.totalPages]);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const res = await listVouchers({
        search: filters.search || undefined,
        status: filters.status || undefined,
        dateFrom: filters.dateFrom || undefined,
        dateTo: filters.dateTo || undefined,
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
          "Không thể tải danh sách phiếu chi"
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
    setFilters((prev) => ({ ...prev, search: searchInput.trim(), page: 1 }));
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

  const onToggleSort = (field) => {
    setFilters((prev) => ({
      ...prev,
      sortBy: field,
      sortDir: prev.sortBy === field && prev.sortDir === "desc" ? "asc" : "desc",
      page: 1,
    }));
  };

  const getVoucherId = (voucher) => voucher?.voucherId ?? voucher?.id;

  const getVoucherDisplay = (voucher) =>
    voucher?.number || voucher?.voucherNo || voucher?.voucherId || voucher?.id || "-";

  const withProcessing = async (voucher, action, task) => {
    const voucherId = getVoucherId(voucher);
    if (!voucherId) return;
    setProcessing({ id: voucherId, action });
    setActionMsg(null);
    try {
      await task();
      setActionMsg({ type: "success", text: "Thao tác thành công." });
      await load();
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
      setProcessing({ id: null, action: null });
    }
  };

  const handleSubmitApproval = (voucher) => {
    if (
      !window.confirm(
        `Gửi phiếu chi ${getVoucherDisplay(voucher)} sang trạng thái Chờ duyệt?`
      )
    )
      return;
    withProcessing(voucher, "submit", () =>
      updateVoucherStatus(getVoucherId(voucher), { status: "PENDING" })
    );
  };

  const handleApprove = (voucher) => {
    if (
      !window.confirm(
        `Duyệt phiếu chi ${getVoucherDisplay(voucher)}? Hệ thống sẽ tự động tạo bút toán.`
      )
    )
      return;
    withProcessing(voucher, "approve", () =>
      updateVoucherStatus(getVoucherId(voucher), { status: "APPROVED" })
    );
  };

  const handleCancel = (voucher) => {
    const note = window.prompt(
        `Nhập lý do huỷ phiếu chi ${getVoucherDisplay(voucher)} (tuỳ chọn):`
    );
    withProcessing(voucher, "cancel", () =>
      updateVoucherStatus(getVoucherId(voucher), {
        status: "CANCELLED",
        note: note?.trim() || null,
      })
    );
  };

  const handleDelete = (voucher) => {
    if (
      !window.confirm(
        `Xoá phiếu chi ${getVoucherDisplay(voucher)}? Hành động không thể hoàn tác.`
      )
    )
      return;
    withProcessing(voucher, "delete", () => deleteVoucher(getVoucherId(voucher)));
  };

  const onViewVoucher = (voucher) => {
    const voucherId = getVoucherId(voucher);
    if (!voucherId) return;
    navigate(ROUTER_PAGE.ACCOUNTANT.VOUCHER_VIEW.replace(":id", voucherId));
  };

  const onEditVoucher = (voucher) => {
    const voucherId = getVoucherId(voucher);
    if (!voucherId) return;
    navigate(ROUTER_PAGE.ACCOUNTANT.VOUCHER_EDIT.replace(":id", voucherId));
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-slate-800">Phiếu chi</h1>
          <p className="text-sm text-slate-500">
            Quản lý phiếu chi thủ công, trạng thái duyệt và dòng tiền ra khỏi quỹ.
          </p>
        </div>
        <button
          onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.VOUCHER_CREATE)}
          className="inline-flex items-center justify-center gap-2 rounded-full bg-indigo-600 px-5 py-2 text-sm font-semibold text-white shadow hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
        >
          + Tạo phiếu chi
        </button>
      </div>

      <form
        onSubmit={onSearchSubmit}
        className="grid grid-cols-1 lg:grid-cols-6 gap-4 rounded-3xl border border-slate-200 bg-white/80 p-5 shadow-sm backdrop-blur"
      >
        <div className="lg:col-span-2">
          <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
            Tìm kiếm
          </label>
          <input
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
            placeholder="Số phiếu chi..."
          />
        </div>
        <div>
          <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
            Trạng thái
          </label>
          <select
            value={filters.status}
            onChange={(e) => onChangeFilter("status", e.target.value)}
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
          <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
            Từ ngày
          </label>
          <input
            type="date"
            value={filters.dateFrom}
            onChange={(e) => onChangeFilter("dateFrom", e.target.value)}
            className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
          />
        </div>
        <div>
          <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
            Đến ngày
          </label>
          <input
            type="date"
            value={filters.dateTo}
            onChange={(e) => onChangeFilter("dateTo", e.target.value)}
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
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">
              Tổng quan
            </p>
            <h2 className="text-lg font-semibold text-slate-700">
              {loading ? "Đang tải phiếu chi..." : `${data.totalItems} phiếu chi`}
            </h2>
          </div>
          <div className="flex items-center gap-2 text-xs text-slate-500">
            <span className="font-medium">Sắp xếp theo:</span>
            <button
              type="button"
              onClick={() => onToggleSort("Date")}
              className={`rounded-full px-3 py-1 transition ${
                filters.sortBy === "Date" ? "bg-indigo-100 text-indigo-600" : "hover:bg-slate-100"
              }`}
            >
              Ngày chứng từ {filters.sortBy === "Date" ? (filters.sortDir === "desc" ? "↓" : "↑") : ""}
            </button>
            <button
              type="button"
              onClick={() => onToggleSort("TotalAmount")}
              className={`rounded-full px-3 py-1 transition ${
                filters.sortBy === "TotalAmount"
                  ? "bg-indigo-100 text-indigo-600"
                  : "hover:bg-slate-100"
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
                <th className="px-6 py-3 text-left font-semibold text-slate-600">Số phiếu chi</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">Ngày</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">Đối tác</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">Số tiền</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">Trạng thái</th>
                <th className="px-6 py-3 text-right font-semibold text-slate-600">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 bg-white">
              {loading ? (
                <tr>
                  <td colSpan={6} className="px-6 py-12 text-center text-slate-500">
                    Đang tải phiếu chi...
                  </td>
                </tr>
              ) : data.items.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-12 text-center text-slate-400">
                    Không có phiếu chi nào phù hợp bộ lọc hiện tại.
                  </td>
                </tr>
              ) : (
                data.items.map((voucher) => {
                  const amount = voucher.totalAmount ?? voucher.amount ?? 0;
                  const voucherId = getVoucherId(voucher);
                  const actions = [];
                  const isProcessing = processing.id === voucherId;

                  if (voucher.status === "DRAFT") {
                    actions.push(
                      <button
                        key="edit"
                        onClick={() => onEditVoucher(voucher)}
                        className="text-xs font-semibold text-slate-600 hover:text-indigo-600"
                      >
                        Sửa
                      </button>
                    );
                    actions.push(
                      <button
                        key="submit"
                        onClick={() => handleSubmitApproval(voucher)}
                        disabled={isProcessing}
                        className={`text-xs font-semibold ${
                          isProcessing && processing.action === "submit"
                            ? "text-slate-400"
                            : "text-indigo-600 hover:text-indigo-800"
                        }`}
                      >
                        {isProcessing && processing.action === "submit" ? "Đang gửi..." : "Gửi duyệt"}
                      </button>
                    );
                    actions.push(
                      <button
                        key="cancel"
                        onClick={() => handleCancel(voucher)}
                        disabled={isProcessing}
                        className={`text-xs font-semibold ${
                          isProcessing && processing.action === "cancel"
                            ? "text-slate-400"
                            : "text-amber-600 hover:text-amber-800"
                        }`}
                      >
                        {isProcessing && processing.action === "cancel" ? "Đang huỷ..." : "Huỷ"}
                      </button>
                    );
                    actions.push(
                      <button
                        key="delete"
                        onClick={() => handleDelete(voucher)}
                        disabled={isProcessing}
                        className={`text-xs font-semibold ${
                          isProcessing && processing.action === "delete"
                            ? "text-slate-400"
                            : "text-rose-600 hover:text-rose-800"
                        }`}
                      >
                        {isProcessing && processing.action === "delete" ? "Đang xoá..." : "Xoá"}
                      </button>
                    );
                  } else if (voucher.status === "PENDING") {
                    actions.push(
                      <button
                        key="approve"
                        onClick={() => handleApprove(voucher)}
                        disabled={isProcessing}
                        className={`text-xs font-semibold ${
                          isProcessing && processing.action === "approve"
                            ? "text-slate-400"
                            : "text-emerald-600 hover:text-emerald-800"
                        }`}
                      >
                        {isProcessing && processing.action === "approve" ? "Đang duyệt..." : "Duyệt"}
                      </button>
                    );
                    actions.push(
                      <button
                        key="cancel"
                        onClick={() => handleCancel(voucher)}
                        disabled={isProcessing}
                        className={`text-xs font-semibold ${
                          isProcessing && processing.action === "cancel"
                            ? "text-slate-400"
                            : "text-rose-600 hover:text-rose-800"
                        }`}
                      >
                        {isProcessing && processing.action === "cancel" ? "Đang huỷ..." : "Huỷ"}
                      </button>
                    );
                  } else {
                    actions.push(
                      <button
                        key="view"
                        onClick={() => onViewVoucher(voucher)}
                        className="text-xs font-semibold text-slate-600 hover:text-indigo-600"
                      >
                        Xem
                      </button>
                    );
                  }

                  return (
                    <tr key={voucherId} className="hover:bg-slate-50/70 transition">
                      <td className="px-6 py-4 align-top">
                        <div className="font-semibold text-slate-700">
                          {getVoucherDisplay(voucher)}
                        </div>
                        {voucher.description && (
                          <div className="text-xs text-slate-400 mt-0.5 line-clamp-1">
                            {voucher.description}
                          </div>
                        )}
                      </td>
                      <td className="px-6 py-4 align-top text-sm text-slate-600">
                        {formatDate(voucher.voucherDate || voucher.date || voucher.createdAt)}
                      </td>
                      <td className="px-6 py-4 align-top text-sm text-slate-600">
                        {(voucher.companyInfo || "").trim() || "-"}
                      </td>
                      <td className="px-6 py-4 align-top">
                        <div className="text-sm font-semibold text-slate-700">
                          {money.format(amount)}
                        </div>
                      </td>
                      <td className="px-6 py-4 align-top">
                        <span
                          className={`inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold ${
                            STATUS_COLORS[voucher.status] || "bg-slate-100 text-slate-700"
                          }`}
                        >
                          {STATUS_LABELS[voucher.status] || voucher.status}
                        </span>
                      </td>
                      <td className="px-6 py-4 align-top">
                        <div className="flex flex-wrap justify-end gap-3">{actions}</div>
                      </td>
                    </tr>
                  );
                })
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
