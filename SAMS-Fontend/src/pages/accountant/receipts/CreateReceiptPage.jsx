import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import api from "../../../lib/apiClient";
import { createReceipt } from "../../../features/accountant/receiptApi";
import { listPaymentMethods } from "../../../features/accountant/paymentMethodApi";
import { updateInvoiceStatus } from "../../../features/accountant/invoiceApi";
import Toast from "../../../components/Toast";

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

export default function CreateReceiptPage() {
  const navigate = useNavigate();
  const [step, setStep] = useState(1);
  const [searchKeyword, setSearchKeyword] = useState("");
  const [invoices, setInvoices] = useState([]);
  const [loading, setLoading] = useState(false);
  const [selectedInvoice, setSelectedInvoice] = useState(null);
  const [apartments, setApartments] = useState([]);
  const [submitting, setSubmitting] = useState(false);
  const [createdReceipt, setCreatedReceipt] = useState(null);
  const [error, setError] = useState("");
  const [paymentMethods, setPaymentMethods] = useState([]);
  const [toast, setToast] = useState({ show: false, message: "", type: "success" });

  const [paymentForm, setPaymentForm] = useState({
    paymentMethodId: "",
    paymentMethodName: "",
    amount: 0,
    note: "",
  });

  // Load unpaid invoices
  const loadUnpaidInvoices = async (searchTerm = "") => {
    setLoading(true);
    setError("");
    try {
      const params = {
        pageSize: 100,
      };
      
      // Try without status filter first to see if we get data
      // Backend might not support comma-separated statuses
      // params.status = "ISSUED,OVERDUE";
      
      if (searchTerm) {
        params.search = searchTerm;
      }
      
      console.log("🔍 Loading invoices with params:", params);
      
      const res = await api.get("/Invoice", { params });
      
      console.log("📥 Invoice API response:", {
        status: res.status,
        dataType: typeof res.data,
        isArray: Array.isArray(res.data),
        hasItems: res.data?.items ? true : false,
        itemsCount: res.data?.items?.length || (Array.isArray(res.data) ? res.data.length : 0),
        firstItem: res.data?.items?.[0] || res.data?.[0],
        statuses: res.data?.items?.map(i => i.status).filter((v, i, a) => a.indexOf(v) === i)
      });
      
      const allItems = res?.data?.items ?? (Array.isArray(res?.data) ? res.data : []);
      
      // Filter on frontend for ISSUED or OVERDUE status
      const items = allItems.filter(invoice => 
        invoice.status === "ISSUED" || invoice.status === "OVERDUE"
      );
      
      setInvoices(items);
      
      console.log(`✅ Filtered ${items.length} unpaid invoices (ISSUED/OVERDUE) from ${allItems.length} total`);
      
      if (items.length === 0 && allItems.length > 0) {
        console.warn("⚠️ Backend returned invoices but none with ISSUED/OVERDUE status");
      }
    } catch (err) {
      console.error("❌ Error loading invoices:", err);
      setError(
        err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          "Không thể tải danh sách hóa đơn"
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const fetchData = async () => {
      try {
        // Fetch apartments
        const apartmentResponse = await api.get("/Apartment");
        const apartmentsData = Array.isArray(apartmentResponse.data) ? apartmentResponse.data : [];
        setApartments(apartmentsData);

        // Fetch payment methods from /api/PaymentMethod/active
        const methods = await listPaymentMethods();
        const methodsArray = Array.isArray(methods) ? methods : [];
        setPaymentMethods(methodsArray);
        
        // Set default payment method to first one (usually CASH)
        if (methodsArray.length > 0) {
          setPaymentForm(prev => ({ 
            ...prev, 
            paymentMethodId: methodsArray[0].paymentMethodId,
            paymentMethodName: methodsArray[0].name || methodsArray[0].code || ""
          }));
        }

        // Auto-load unpaid invoices
        await loadUnpaidInvoices();
      } catch (error) {
        console.error("Error fetching data:", error);
      }
    };
    fetchData();
  }, []);

  const getApartmentName = (apartmentId) => {
    if (!apartmentId) return "-";
    const apartment = apartments.find((apt) => apt.apartmentId === apartmentId);
    if (apartment) {
      return `${apartment.floorNumber ? `Tầng ${apartment.floorNumber} - ` : ""}${apartment.number || apartmentId}`;
    }
    return apartmentId;
  };

  const handleSearchInvoices = async (e) => {
    e.preventDefault();
    await loadUnpaidInvoices(searchKeyword.trim());
  };

  const handleSelectInvoice = (invoice) => {
    setSelectedInvoice(invoice);
    const defaultMethod = paymentMethods.find(m => m.paymentMethodId === paymentForm.paymentMethodId) || paymentMethods[0];
    setPaymentForm({
      paymentMethodId: defaultMethod?.paymentMethodId || "",
      paymentMethodName: defaultMethod?.name || defaultMethod?.code || "",
      amount: invoice.totalAmount || 0,
      note: "",
    });
    setStep(2);
  };

  const showToast = (message, type = "success") => {
    setToast({ show: true, message, type });
  };

  const handleConfirmPayment = async (e) => {
    e.preventDefault();
    if (!selectedInvoice) return;

    setSubmitting(true);
    setError("");
    try {
      const payload = {
        invoiceId: selectedInvoice.invoiceId,
        methodId: paymentForm.paymentMethodId,
        amountTotal: parseFloat(paymentForm.amount) || 0,
        note: paymentForm.note?.trim() || null,
      };

      console.log("📤 Sending receipt payload:", payload);

      // Bước 1: Tạo receipt
      const receipt = await createReceipt(payload);
      console.log("✅ Receipt created:", receipt);
      
      // Bước 2: Cập nhật trạng thái invoice sang PAID
      try {
        const now = new Date();
        const timeString = now.toLocaleString("vi-VN", {
          year: "numeric",
          month: "2-digit",
          day: "2-digit",
          hour: "2-digit",
          minute: "2-digit",
        });
        
        const statusPayload = {
          status: "PAID",
          note: `Thanh toán qua Receipt ${receipt.receiptNo || receipt.receiptId} - ${timeString}`
        };
        
        console.log("📤 Updating invoice status to PAID:", statusPayload);
        await updateInvoiceStatus(selectedInvoice.invoiceId, statusPayload);
        console.log("✅ Invoice status updated to PAID");
      } catch (statusErr) {
        console.error("⚠️ Failed to update invoice status (receipt was created successfully):", statusErr);
        // Không throw error vì receipt đã tạo thành công
        // Chỉ log warning
      }
      
      setCreatedReceipt(receipt);
      showToast("Đã tạo biên lai thành công", "success");
      setStep(3);
    } catch (err) {
      console.error("❌ Create receipt error:", err);
      console.error("❌ Error response:", err?.response?.data);
      
      const message =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        "Không thể tạo biên lai. Vui lòng kiểm tra backend logs.";
      setError(message);
      showToast(message, "error");
    } finally {
      setSubmitting(false);
    }
  };

  const handlePrintReceipt = () => {
    // TODO: Implement print functionality
    alert("Tính năng in biên lai sẽ được thêm sau");
  };

  const handleSendEmail = () => {
    // TODO: Implement send email functionality
    alert("Tính năng gửi email sẽ được thêm sau");
  };

  const handleViewInvoice = () => {
    if (selectedInvoice) {
      navigate(ROUTER_PAGE.ACCOUNTANT.INVOICE_VIEW.replace(":id", selectedInvoice.invoiceId));
    }
  };

  const handleBackToList = () => {
    navigate(ROUTER_PAGE.ACCOUNTANT.RECEIPTS);
  };

  const handleStartNew = () => {
    setStep(1);
    setSearchKeyword("");
    setInvoices([]);
    setSelectedInvoice(null);
    setCreatedReceipt(null);
    setError("");
    const defaultMethod = paymentMethods[0];
    setPaymentForm({
      paymentMethodId: defaultMethod?.paymentMethodId || "",
      paymentMethodName: defaultMethod?.name || defaultMethod?.code || "",
      amount: 0,
      note: "",
    });
    // Reload unpaid invoices
    loadUnpaidInvoices();
  };

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      <Toast
        show={toast.show}
        message={toast.message}
        type={toast.type}
        onClose={() => setToast((prev) => ({ ...prev, show: false }))}
      />
      {/* Header */}
      <div className="flex items-center justify-between">
        <button
          onClick={handleBackToList}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
        >
          ← Quay lại danh sách
        </button>
        <div className="flex items-center gap-2">
          {[1, 2, 3].map((s) => (
            <div
              key={s}
              className={`flex items-center justify-center w-8 h-8 rounded-full text-sm font-semibold ${
                s === step
                  ? "bg-indigo-600 text-white"
                  : s < step
                  ? "bg-emerald-500 text-white"
                  : "bg-slate-200 text-slate-500"
              }`}
            >
              {s}
            </div>
          ))}
        </div>
      </div>

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-medium text-rose-700">
          {error}
        </div>
      )}

      {/* Step 1: Search Invoice */}
      {step === 1 && (
        <div className="rounded-3xl border border-slate-200 bg-white shadow-xl p-8">
          <h2 className="text-2xl font-bold text-slate-800 mb-6">Bước 1: Tìm hóa đơn chưa thanh toán</h2>

          <form onSubmit={handleSearchInvoices} className="space-y-4">
            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-2">
                Tìm kiếm hóa đơn
              </label>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={searchKeyword}
                  onChange={(e) => setSearchKeyword(e.target.value)}
                  className="flex-1 rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
                  placeholder="Nhập số hóa đơn hoặc tên căn hộ..."
                />
                <button
                  type="submit"
                  disabled={loading}
                  className="inline-flex items-center justify-center rounded-xl bg-slate-900 px-6 py-2.5 text-sm font-semibold text-white shadow hover:bg-black focus:outline-none focus:ring-2 focus:ring-slate-500 focus:ring-offset-2 disabled:bg-slate-400"
                >
                  {loading ? "Đang tìm..." : "Tìm kiếm"}
                </button>
              </div>
              <p className="mt-2 text-xs text-slate-500">
                Chỉ hiển thị các hóa đơn có trạng thái ISSUED hoặc OVERDUE
              </p>
            </div>
          </form>

          {invoices.length > 0 && (
            <div className="mt-6">
              <h3 className="text-sm font-semibold text-slate-700 mb-3">
                Kết quả tìm kiếm ({invoices.length} hóa đơn)
              </h3>
              <div className="space-y-2 max-h-96 overflow-y-auto">
                {invoices.map((invoice) => (
                  <div
                    key={invoice.invoiceId}
                    className="flex items-center justify-between p-4 border border-slate-200 rounded-xl hover:bg-slate-50 transition"
                  >
                    <div className="flex-1">
                      <div className="font-semibold text-slate-800">{invoice.invoiceNo}</div>
                      <div className="text-sm text-slate-600">
                        Căn hộ: {getApartmentName(invoice.apartmentId)}
                      </div>
                      <div className="text-xs text-slate-500 mt-1">
                        Ngày đến hạn: {formatDate(invoice.dueDate)}
                      </div>
                    </div>
                    <div className="text-right mr-4">
                      <div className="text-lg font-bold text-slate-800">
                        {money.format(invoice.totalAmount ?? 0)}
                      </div>
                      <span
                        className={`inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold ${
                          invoice.status === "OVERDUE"
                            ? "bg-amber-100 text-amber-800"
                            : "bg-blue-100 text-blue-700"
                        }`}
                      >
                        {invoice.status}
                      </span>
                    </div>
                    <button
                      onClick={() => handleSelectInvoice(invoice)}
                      className="inline-flex items-center gap-2 rounded-full bg-indigo-600 px-5 py-2 text-sm font-semibold text-white shadow hover:bg-indigo-700"
                    >
                      Chọn
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )}

          {!loading && invoices.length === 0 && searchKeyword && (
            <div className="mt-6 text-center text-slate-500 py-8">
              Không tìm thấy hóa đơn nào phù hợp
            </div>
          )}
        </div>
      )}

      {/* Step 2: Confirm Payment */}
      {step === 2 && selectedInvoice && (
        <div className="rounded-3xl border border-slate-200 bg-white shadow-xl p-8">
          <h2 className="text-2xl font-bold text-slate-800 mb-6">Bước 2: Xác nhận thông tin thanh toán</h2>

          <div className="mb-6 p-4 bg-slate-50 rounded-xl">
            <h3 className="text-sm font-semibold text-slate-700 mb-3">Thông tin hóa đơn</h3>
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <span className="text-slate-500">Số hóa đơn:</span>
                <span className="ml-2 font-semibold text-slate-800">{selectedInvoice.invoiceNo}</span>
              </div>
              <div>
                <span className="text-slate-500">Căn hộ:</span>
                <span className="ml-2 font-semibold text-slate-800">
                  {getApartmentName(selectedInvoice.apartmentId)}
                </span>
              </div>
              <div>
                <span className="text-slate-500">Ngày phát hành:</span>
                <span className="ml-2 text-slate-700">{formatDate(selectedInvoice.issueDate)}</span>
              </div>
              <div>
                <span className="text-slate-500">Ngày đến hạn:</span>
                <span className="ml-2 text-slate-700">{formatDate(selectedInvoice.dueDate)}</span>
              </div>
              <div className="col-span-2">
                <span className="text-slate-500">Tổng tiền:</span>
                <span className="ml-2 text-xl font-bold text-indigo-600">
                  {money.format(selectedInvoice.totalAmount ?? 0)}
                </span>
              </div>
            </div>
          </div>

          <form onSubmit={handleConfirmPayment} className="space-y-6">
            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-2">
                Phương thức thanh toán *
              </label>
              <select
                value={paymentForm.paymentMethodId}
                onChange={(e) => {
                  const selectedMethod = paymentMethods.find(m => m.paymentMethodId === e.target.value);
                  setPaymentForm({ 
                    ...paymentForm, 
                    paymentMethodId: e.target.value,
                    paymentMethodName: selectedMethod?.name || selectedMethod?.code || ""
                  });
                }}
                className="w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
                required
              >
                {paymentMethods.length === 0 ? (
                  <option value="">Đang tải...</option>
                ) : (
                  paymentMethods.map((method) => (
                    <option key={method.paymentMethodId} value={method.paymentMethodId}>
                      {method.name || method.code}
                    </option>
                  ))
                )}
              </select>
            </div>

            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-2">
                Số tiền thanh toán *
              </label>
              <input
                type="number"
                value={paymentForm.amount}
                onChange={(e) => setPaymentForm({ ...paymentForm, amount: e.target.value })}
                className="w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200"
                min="0"
                step="1000"
                required
              />
              <p className="mt-1 text-xs text-slate-500">
                Mặc định bằng tổng tiền hóa đơn
              </p>
            </div>

            <div>
              <label className="block text-sm font-semibold text-slate-700 mb-2">
                Ghi chú
              </label>
              <textarea
                value={paymentForm.note}
                onChange={(e) => setPaymentForm({ ...paymentForm, note: e.target.value })}
                rows={3}
                className="w-full rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200 resize-none"
                placeholder="Nhập ghi chú (không bắt buộc)..."
              />
            </div>

            <div className="flex items-center gap-3 pt-4">
              <button
                type="button"
                onClick={() => setStep(1)}
                className="flex-1 inline-flex items-center justify-center rounded-xl border border-slate-300 px-6 py-3 text-sm font-semibold text-slate-600 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-slate-400 focus:ring-offset-2"
              >
                ← Quay lại
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="flex-1 inline-flex items-center justify-center rounded-xl bg-indigo-600 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-indigo-400/30 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:bg-indigo-400"
              >
                {submitting ? "Đang xử lý..." : "Xác nhận thu tiền"}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Step 3: Success */}
      {step === 3 && createdReceipt && (
        <div className="rounded-3xl border border-slate-200 bg-white shadow-xl p-8">
          <div className="text-center mb-6">
            <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-emerald-100 mb-4">
              <svg
                className="w-8 h-8 text-emerald-600"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M5 13l4 4L19 7"
                />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-slate-800 mb-2">Thu tiền thành công!</h2>
            <p className="text-slate-600">Biên lai đã được tạo và lưu vào hệ thống</p>
          </div>

          <div className="mb-6 p-6 bg-gradient-to-br from-indigo-50 to-slate-50 rounded-xl">
            <h3 className="text-sm font-semibold text-slate-700 mb-4">Thông tin biên lai</h3>
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <span className="text-slate-500">Số biên lai:</span>
                <span className="ml-2 font-semibold text-slate-800">
                  {createdReceipt.receiptNo || createdReceipt.receiptId}
                </span>
              </div>
              <div>
                <span className="text-slate-500">Số hóa đơn:</span>
                <span className="ml-2 font-semibold text-slate-800">{selectedInvoice?.invoiceNo}</span>
              </div>
              <div>
                <span className="text-slate-500">Phương thức:</span>
                <span className="ml-2 text-slate-700">
                  {createdReceipt.method?.name || 
                   createdReceipt.paymentMethod || 
                   paymentForm.paymentMethodName || 
                   "-"}
                </span>
              </div>
              <div>
                <span className="text-slate-500">Ngày thu:</span>
                <span className="ml-2 text-slate-700">
                  {formatDate(createdReceipt.receiptDate || createdReceipt.createdAt || new Date())}
                </span>
              </div>
              <div className="col-span-2">
                <span className="text-slate-500">Số tiền đã thu:</span>
                <span className="ml-2 text-2xl font-bold text-emerald-600">
                  {money.format(
                    createdReceipt.amount ?? 
                    createdReceipt.amountTotal ?? 
                    paymentForm.amount ?? 
                    0
                  )}
                </span>
              </div>
              {(createdReceipt.note || paymentForm.note) && (
                <div className="col-span-2">
                  <span className="text-slate-500">Ghi chú:</span>
                  <span className="ml-2 text-slate-700">{createdReceipt.note || paymentForm.note}</span>
                </div>
              )}
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <button
              onClick={handlePrintReceipt}
              className="inline-flex items-center justify-center gap-2 rounded-xl border border-slate-300 px-4 py-2.5 text-sm font-semibold text-slate-600 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-slate-400 focus:ring-offset-2"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"
                />
              </svg>
              In biên lai
            </button>
            <button
              onClick={handleSendEmail}
              className="inline-flex items-center justify-center gap-2 rounded-xl border border-slate-300 px-4 py-2.5 text-sm font-semibold text-slate-600 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-slate-400 focus:ring-offset-2"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                />
              </svg>
              Gửi Email
            </button>
            <button
              onClick={handleViewInvoice}
              className="inline-flex items-center justify-center gap-2 rounded-xl border border-slate-300 px-4 py-2.5 text-sm font-semibold text-slate-600 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-slate-400 focus:ring-offset-2"
            >
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
              Xem Invoice
            </button>
          </div>

          <div className="mt-6 pt-6 border-t border-slate-200 flex gap-3">
            <button
              onClick={handleStartNew}
              className="flex-1 inline-flex items-center justify-center rounded-xl bg-indigo-600 px-6 py-2.5 text-sm font-semibold text-white shadow-lg shadow-indigo-400/30 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
            >
              Thu tiền tiếp
            </button>
            <button
              onClick={handleBackToList}
              className="flex-1 inline-flex items-center justify-center rounded-xl border border-slate-300 px-6 py-2.5 text-sm font-semibold text-slate-600 hover:bg-slate-100 focus:outline-none focus:ring-2 focus:ring-slate-400 focus:ring-offset-2"
            >
              Về danh sách biên lai
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
