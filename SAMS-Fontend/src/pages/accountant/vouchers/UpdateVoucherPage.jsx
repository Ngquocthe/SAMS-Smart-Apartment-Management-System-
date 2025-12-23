import React, { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import VoucherForm from "./VoucherForm";
import {
  createVoucherItem,
  deleteVoucherItem,
  getVoucherById,
  getVoucherItems,
  updateVoucher,
  updateVoucherItem,
} from "../../../features/accountant/voucherApi";
import Toast from "../../../components/Toast";

const normalizeItem = (item) => {
  if (!item) return null;
  return {
    voucherItemId: item.voucherItemId || item.voucherItemsId || item.id,
    description: item.description || item.serviceTypeName || "",
    quantity: item.quantity ?? item.qty ?? 1,
    unitPrice: item.unitPrice ?? item.amount ?? 0,
    amount:
      item.amount ??
      item.totalAmount ??
      item.debitAmount ??
      item.creditAmount ??
      (item.quantity && item.unitPrice ? item.quantity * item.unitPrice : 0),
  };
};

export default function UpdateVoucherPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [voucher, setVoucher] = useState(null);
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [serverMsg, setServerMsg] = useState(null);
  const [toast, setToast] = useState({ show: false, message: "", type: "success" });

  const showToast = (message, type = "success") => {
    setToast({ show: true, message, type });
  };

  const load = async () => {
    if (!id) return;
    setLoading(true);
    setError("");
    try {
      const data = await getVoucherById(id);
      setVoucher(data);
      const itemRes = await getVoucherItems(id);
      setItems(Array.isArray(itemRes) ? itemRes.map(normalizeItem) : []);
    } catch (err) {
      setError(
        err?.response?.data?.error ||
          err?.response?.data?.message ||
          err?.message ||
          "Không thể tải phiếu chi"
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const initialValues = useMemo(() => {
    if (!voucher) return null;
    return {
      voucherNo: voucher.number || voucher.voucherNo || voucher.voucherNumber || "",
      voucherDate: voucher.date || voucher.voucherDate,
      companyInfo: voucher.companyInfo ?? "",
      description: voucher.description || "",
      totalAmount: voucher.totalAmount ?? voucher.amount ?? "",
      items,
    };
  }, [voucher, items]);

  const canEdit = voucher?.status === "DRAFT";

  const syncVoucherItems = async (voucherId, lineItems = [], deletedIds = []) => {
    if (!voucherId) return;
    const tasks = [];
    deletedIds.forEach((itemId) => {
      if (itemId) {
        tasks.push(deleteVoucherItem(itemId));
      }
    });
    lineItems.forEach((item) => {
      const dto = {
        description: item.description?.trim(),
        quantity: Number(item.quantity),
        unitPrice: Number(item.unitPrice),
        amount: Number(item.amount),
      };
      if (item.voucherItemId) {
        tasks.push(updateVoucherItem(item.voucherItemId, dto));
      } else {
        tasks.push(createVoucherItem(voucherId, dto));
      }
    });
    if (tasks.length) {
      await Promise.all(tasks);
    }
  };

  const handleSubmit = async (payload, meta = {}) => {
    if (!id) return;
    setSubmitting(true);
    setServerMsg(null);
    try {
      const updated = await updateVoucher(id, payload);
      setVoucher(updated);
      await syncVoucherItems(id, meta.raw?.items ?? [], meta.raw?.deletedItemIds ?? []);
      try {
        const itemRes = await getVoucherItems(id);
        setItems(Array.isArray(itemRes) ? itemRes.map(normalizeItem) : []);
      } catch (detailErr) {
        console.error("Không thể tải lại chi tiết", detailErr);
      }
      const successText = "Đã lưu thay đổi phiếu chi.";
      setServerMsg({ type: "success", text: successText });
      showToast(successText, "success");
    } catch (err) {
      const message =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        "Không thể cập nhật phiếu chi";
      setServerMsg({
        type: "error",
        text: message,
      });
      showToast(message, "error");
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="max-w-4xl mx-auto space-y-4">
        <div className="rounded-2xl border border-slate-200 bg-white px-6 py-10 text-center text-slate-500 shadow">
          Đang tải phiếu chi...
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="max-w-4xl mx-auto space-y-4">
        <button
          onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.VOUCHERS)}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
        >
          ← Quay lại danh sách
        </button>
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-6 py-10 text-center text-rose-700 shadow">
          {error}
        </div>
      </div>
    );
  }

  if (!initialValues) return null;

  if (!canEdit) {
    return (
      <div className="max-w-4xl mx-auto space-y-4">
        <div className="rounded-2xl border border-amber-200 bg-amber-50 px-6 py-6 text-sm text-amber-800">
          Phiếu chi đã ở trạng thái {voucher?.status}. Chỉ có thể chỉnh sửa khi còn trạng thái Nháp.
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.VOUCHERS)}
            className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
          >
            ← Quay lại danh sách
          </button>
          <button
            onClick={() =>
              navigate(ROUTER_PAGE.ACCOUNTANT.VOUCHER_VIEW.replace(":id", id))
            }
            className="inline-flex items-center gap-2 rounded-full bg-slate-900 px-4 py-2 text-sm font-semibold text-white shadow hover:bg-black"
          >
            Xem phiếu chi
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <button
          onClick={() => navigate(ROUTER_PAGE.ACCOUNTANT.VOUCHERS)}
          className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
        >
          ← Quay lại danh sách
        </button>
        <div className="text-right">
          <p className="text-xs uppercase tracking-wide text-slate-400">Phiếu chi</p>
          <h1 className="text-2xl font-bold text-slate-800">Cập nhật phiếu chi</h1>
          <p className="text-xs text-slate-400">
            Mã phiếu: {voucher?.number || voucher?.voucherNo || voucher?.voucherId}
          </p>
        </div>

      <Toast
        show={toast.show}
        message={toast.message}
        type={toast.type}
        onClose={() => setToast((prev) => ({ ...prev, show: false }))}
      />
      </div>

      <VoucherForm
        mode="update"
        initialValues={initialValues}
        onSubmit={handleSubmit}
        submitting={submitting}
        serverMsg={serverMsg}
        canEdit={canEdit}
      />
    </div>
  );
}

