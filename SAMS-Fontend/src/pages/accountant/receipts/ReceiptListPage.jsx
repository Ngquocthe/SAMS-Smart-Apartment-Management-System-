import React, { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import { listReceipts } from "../../../features/accountant/receiptApi";
import { listPaymentMethods } from "../../../features/accountant/paymentMethodApi";

// Payment method filters will be loaded from API

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

const initialFilters = {
  search: "",
  paymentMethod: "",
  dateFrom: "",
  dateTo: "",
  // Note: Backend doesn't support sortBy/sortDir yet
  // sortBy: "Date",
  // sortDir: "desc",
  page: 1,
  pageSize: 10,
};

export default function ReceiptListPage() {
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
  const [apartments, setApartments] = useState([]);
  const [invoices, setInvoices] = useState([]);
  const [paymentMethods, setPaymentMethods] = useState([]);

  const totalPages = useMemo(() => {
    if (data.totalPages) return data.totalPages;
    if (!data.totalItems || !data.pageSize) return 1;
    return Math.max(1, Math.ceil(data.totalItems / data.pageSize));
  }, [data.totalItems, data.pageSize, data.totalPages]);

  // Fetch payment methods on mount
  useEffect(() => {
    const fetchPaymentMethods = async () => {
      try {
        const methods = await listPaymentMethods();
        const methodsArray = Array.isArray(methods) ? methods : [];
        setPaymentMethods(methodsArray);
      } catch (error) {
        console.error("Error fetching payment methods:", error);
        setPaymentMethods([]);
      }
    };
    fetchPaymentMethods();
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const res = await listReceipts({
        search: filters.search || undefined,
        paymentMethod: filters.paymentMethod || undefined,
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
          "Không thể tải danh sách biên lai"
      );
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    load();
  }, [load]);

  // No longer need to fetch apartments separately since backend includes them in receipt response
  // useEffect(() => {
  //   const fetchApartments = async () => {
  //     try {
  //       const response = await api.get("/Apartment");
  //       const apartmentsData = Array.isArray(response.data) ? response.data : [];
  //       setApartments(apartmentsData);
  //     } catch (error) {
  //       console.error("Error fetching apartments:", error);
  //       setApartments([]);
  //     }
  //   };
  //   fetchApartments();
  // }, []);

  // No longer need to fetch invoices separately since backend includes them in receipt response
  // useEffect(() => {
  //   const fetchInvoices = async () => {
  //     if (data.items.length === 0) return;
  //     try {
  //       const response = await api.get("/Invoice", {
  //         params: { pageSize: 100 }
  //       });
  //       const invoicesData = Array.isArray(response.data?.items)
  //         ? response.data.items
  //         : Array.isArray(response.data)
  //         ? response.data
  //         : [];
  //       setInvoices(invoicesData);
  //     } catch (error) {
  //       console.error("Error fetching invoices:", error);
  //       setInvoices([]);
  //     }
  //   };
  //   fetchInvoices();
  // }, [data.items]);

  const getApartmentName = (receipt) => {
    // Priority 1: Check if receipt has nested apartment data
    if (receipt.invoice?.apartment?.number) {
      const apt = receipt.invoice.apartment;
      return `${apt.floorNumber ? `Tầng ${apt.floorNumber} - ` : ""}${
        apt.number
      }`;
    }

    // Priority 2: Check if receipt has apartmentName directly
    if (receipt.apartmentName) return receipt.apartmentName;
    if (receipt.apartment?.number) return receipt.apartment.number;

    // Priority 3: Lookup from apartments list
    const apartmentId = receipt.apartmentId || receipt.invoice?.apartmentId;
    if (apartmentId) {
      const apartment = apartments.find(
        (apt) => apt.apartmentId === apartmentId
      );
      if (apartment) {
        return `${
          apartment.floorNumber ? `Tầng ${apartment.floorNumber} - ` : ""
        }${apartment.number || apartmentId}`;
      }
      return apartmentId;
    }

    return "-";
  };

  const getInvoiceNo = (receipt) => {
    // Priority 1: Check if receipt already has invoice data
    if (receipt.invoiceNo) return receipt.invoiceNo;
    if (receipt.invoice?.invoiceNo) return receipt.invoice.invoiceNo;

    // Priority 2: Check if receipt has invoice_no (snake_case from backend)
    if (receipt.invoice_no) return receipt.invoice_no;

    // Priority 3: Look up from invoices list
    if (receipt.invoiceId) {
      const invoice = invoices.find(
        (inv) => inv.invoiceId === receipt.invoiceId
      );
      if (invoice?.invoiceNo) return invoice.invoiceNo;
    }

    // Priority 4: Show invoiceId as fallback
    return receipt.invoiceId || "-";
  };

  const getPaymentMethodLabel = (receipt) => {
    // Priority 1: Check if receipt has nested method data
    if (receipt.method?.name) return receipt.method.name;
    if (receipt.method?.code) return receipt.method.code;

    // Priority 2: Check if receipt has paymentMethod directly
    if (receipt.paymentMethod) {
      const methodMap = {
        CASH: "Tiền mặt",
        BANK_TRANSFER: "Chuyển khoản",
        CARD: "Thẻ",
        MOMO: "MoMo",
        VIETQR: "VietQR",
        OTHER: "Khác",
      };
      return methodMap[receipt.paymentMethod] || receipt.paymentMethod;
    }

    // Priority 3: For backwards compatibility
    if (receipt.paymentMethodName) return receipt.paymentMethodName;

    return "-";
  };

  const onSearchSubmit = (event) => {
    event.preventDefault();
    const trimmedSearch = searchInput.trim();
    setFilters((prev) => ({ ...prev, search: trimmedSearch, page: 1 }));
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

  // Commented out: Backend doesn't support sorting yet
  // const onToggleSort = (field) => {
  //   setFilters((prev) => ({
  //     ...prev,
  //     sortBy: field,
  //     sortDir: prev.sortBy === field && prev.sortDir === "desc" ? "asc" : "desc",
  //     page: 1,
  //   }));
  // };

  // REMOVED: Chức năng thu tiền tại quầy đã chuyển sang Receptionist
  // const onCreateReceipt = () => {
  //   navigate(ROUTER_PAGE.ACCOUNTANT.RECEIPT_CREATE);
  // };

  const onViewReceipt = (receiptId) => {
    navigate(ROUTER_PAGE.ACCOUNTANT.RECEIPT_VIEW.replace(":id", receiptId));
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-slate-800">Biên lai</h1>
          <p className="text-sm text-slate-500">
            Quản lý biên lai thu tiền, theo dõi phương thức thanh toán và lịch
            sử giao dịch.
          </p>
        </div>
        {/* REMOVED: Nút "Thu tiền tại quầy" - Chức năng đã chuyển sang Receptionist */}
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
            placeholder="Số biên lai..."
          />
        </div>
        <div>
          <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
            Phương thức
          </label>
          <select
            value={filters.paymentMethod}
            onChange={(e) =>
              setFilters((prev) => ({
                ...prev,
                paymentMethod: e.target.value,
                page: 1,
              }))
            }
            className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
          >
            <option value="">Tất cả phương thức</option>
            {paymentMethods.map((method) => (
              <option
                key={method.paymentMethodId}
                value={method.paymentMethodId}
              >
                {method.name || method.code}
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
              : actionMsg.type === "error"
              ? "bg-rose-50 border-rose-200 text-rose-700"
              : "bg-sky-50 border-sky-200 text-sky-700"
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
              {loading ? "Đang tải biên lai..." : `${data.totalItems} biên lai`}
            </h2>
          </div>
          {/* Sorting temporarily disabled - Backend doesn't support it yet */}
          {/* <div className="flex items-center gap-2 text-xs text-slate-500">
            <span className="font-medium">Sắp xếp theo:</span>
            <button
              type="button"
              onClick={() => onToggleSort("Date")}
              className={`rounded-full px-3 py-1 transition ${
                filters.sortBy === "Date" ? "bg-indigo-100 text-indigo-600" : "hover:bg-slate-100"
              }`}
            >
              Ngày {filters.sortBy === "Date" ? (filters.sortDir === "desc" ? "↓" : "↑") : ""}
            </button>
            <button
              type="button"
              onClick={() => onToggleSort("Amount")}
              className={`rounded-full px-3 py-1 transition ${
                filters.sortBy === "Amount" ? "bg-indigo-100 text-indigo-600" : "hover:bg-slate-100"
              }`}
            >
              Số tiền {filters.sortBy === "Amount" ? (filters.sortDir === "desc" ? "↓" : "↑") : ""}
            </button>
          </div> */}
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-slate-100 text-sm">
            <thead className="bg-slate-50/70">
              <tr>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">
                  Số biên lai
                </th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">
                  Số hóa đơn
                </th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">
                  Căn hộ
                </th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">
                  Số tiền
                </th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">
                  Phương thức
                </th>
                <th className="px-6 py-3 text-left font-semibold text-slate-600">
                  Ngày
                </th>
                <th className="px-6 py-3 text-right font-semibold text-slate-600">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 bg-white">
              {loading ? (
                <tr>
                  <td
                    colSpan={7}
                    className="px-6 py-12 text-center text-slate-500"
                  >
                    Đang tải biên lai...
                  </td>
                </tr>
              ) : data.items.length === 0 ? (
                <tr>
                  <td
                    colSpan={7}
                    className="px-6 py-12 text-center text-slate-400"
                  >
                    Không có biên lai nào phù hợp với bộ lọc hiện tại.
                  </td>
                </tr>
              ) : (
                data.items.map((receipt) => (
                  <tr
                    key={receipt.receiptId}
                    className="group hover:bg-slate-50/70 transition"
                  >
                    <td className="px-6 py-4 align-top">
                      <div className="font-semibold text-slate-700">
                        {receipt.receiptNo || receipt.receiptId}
                      </div>
                    </td>
                    <td className="px-6 py-4 align-top">
                      <div className="text-sm text-slate-600">
                        {getInvoiceNo(receipt)}
                      </div>
                    </td>
                    <td className="px-6 py-4 align-top">
                      <div className="text-sm font-medium text-slate-700">
                        {getApartmentName(receipt)}
                      </div>
                    </td>
                    <td className="px-6 py-4 align-top">
                      <div className="text-sm font-semibold text-slate-700">
                        {money.format(
                          receipt.amountTotal ?? receipt.amount ?? 0
                        )}
                      </div>
                    </td>
                    <td className="px-6 py-4 align-top">
                      <span className="inline-flex items-center rounded-full px-3 py-1 text-xs font-medium bg-slate-100 text-slate-700">
                        {getPaymentMethodLabel(receipt)}
                      </span>
                    </td>
                    <td className="px-6 py-4 align-top text-sm text-slate-600">
                      {formatDate(
                        receipt.receivedDate ||
                          receipt.receiptDate ||
                          receipt.date ||
                          receipt.createdAt
                      )}
                    </td>
                    <td className="px-6 py-4 align-top text-right">
                      <button
                        onClick={() => onViewReceipt(receipt.receiptId)}
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
