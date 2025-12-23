import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import VoucherForm from "./VoucherForm";
import { createVoucher } from "../../../features/accountant/voucherApi";
import Toast from "../../../components/Toast";

export default function CreateVoucherPage() {
  const navigate = useNavigate();
  const [submitting, setSubmitting] = useState(false);
  const [serverMsg, setServerMsg] = useState(null);
  const [createdVoucher, setCreatedVoucher] = useState(null);
  const [toast, setToast] = useState({ show: false, message: "", type: "success" });

  const showToast = (message, type = "success") => {
    setToast({ show: true, message, type });
  };

  const handleSubmit = async (payload) => {
    setSubmitting(true);
    setServerMsg(null);
    try {
      const created = await createVoucher(payload);
      setCreatedVoucher(created);
      const display =
        created?.number ||
        created?.voucherNo ||
        created?.voucherNumber ||
        created?.voucherId ||
        created?.id ||
        "";
      const text = `Đã tạo phiếu chi ${display}.`;
      setServerMsg({
        type: "success",
        text,
      });
      showToast(text, "success");
    } catch (err) {
      const message =
        err?.response?.data?.error ||
        err?.response?.data?.message ||
        err?.message ||
        "Không thể tạo phiếu chi";
      setServerMsg({
        type: "error",
        text: message,
      });
      showToast(message, "error");
    } finally {
      setSubmitting(false);
    }
  };

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
          <h1 className="text-2xl font-bold text-slate-800">Tạo phiếu chi thủ công</h1>
        </div>
      </div>
      <Toast
        show={toast.show}
        message={toast.message}
        type={toast.type}
        onClose={() => setToast((prev) => ({ ...prev, show: false }))}
      />
      {createdVoucher && (
        <div className="flex items-center gap-2">
          <button
            onClick={() =>
              navigate(
                ROUTER_PAGE.ACCOUNTANT.VOUCHER_EDIT.replace(
                  ":id",
                  createdVoucher?.voucherId || createdVoucher?.id
                )
              )
            }
            className="inline-flex items-center gap-2 rounded-full bg-slate-900 px-4 py-2 text-sm font-semibold text-white shadow hover:bg-black"
          >
            Mở phiếu chi
          </button>
          <button
            onClick={() => {
              setCreatedVoucher(null);
              setServerMsg(null);
            }}
            className="inline-flex items-center gap-2 rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-600 transition hover:border-indigo-300 hover:text-indigo-600"
          >
            Tạo phiếu chi khác
          </button>
        </div>
      )}

      <VoucherForm
        mode="create"
        onSubmit={handleSubmit}
        submitting={submitting}
        serverMsg={serverMsg}
      />
    </div>
  );
}

