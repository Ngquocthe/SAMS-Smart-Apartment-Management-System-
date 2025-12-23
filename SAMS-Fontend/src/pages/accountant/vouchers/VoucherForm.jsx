import React, { useEffect, useMemo, useState } from "react";

const amountFormatter = new Intl.NumberFormat("vi-VN", {
  style: "currency",
  currency: "VND",
  maximumFractionDigits: 0,
});

const toIsoDate = (value) => {
  if (!value) return "";
  if (value instanceof Date && !Number.isNaN(value.getTime())) {
    return value.toISOString().split("T")[0];
  }
  if (typeof value === "string") {
    return value.split("T")[0];
  }
  try {
    const date = new Date(value);
    if (!Number.isNaN(date.getTime())) {
      return date.toISOString().split("T")[0];
    }
  } catch {
    return "";
  }
  return "";
};

const emptyLineItem = () => ({
  voucherItemId: undefined,
  description: "",
  quantity: 1,
  unitPrice: "",
  amount: 0,
});

export default function VoucherForm({
  mode = "create",
  initialValues = null,
  onSubmit,
  submitting = false,
  serverMsg = null,
  canEdit = true,
}) {
  const defaults = useMemo(
    () => ({
      voucherNo: "",
      voucherDate: toIsoDate(new Date()),
      companyInfo: "",
      description: "",
      items: [emptyLineItem()],
    }),
    []
  );

  const [form, setForm] = useState(defaults);
  const [items, setItems] = useState(defaults.items);
  const [errors, setErrors] = useState({});
  const [deletedItemIds, setDeletedItemIds] = useState([]);

  const showItemsSection = mode === "update";
  const editable = canEdit && !submitting;

  useEffect(() => {
    setForm((prev) => ({
      ...prev,
      ...(initialValues
        ? {
            voucherNo:
              initialValues.voucherNumber ??
              initialValues.voucherNo ??
              initialValues.number ??
              "",
            voucherDate: toIsoDate(initialValues.voucherDate || initialValues.date),
            companyInfo: initialValues.companyInfo ?? "",
            description: initialValues.description ?? "",
          }
        : {}),
    }));
    if (showItemsSection && initialValues?.items?.length) {
      setItems(
        initialValues.items.map((item) => ({
          voucherItemId: item.voucherItemId || item.voucherItemsId,
          description: item.serviceTypeName || item.description || "",
          quantity: item.quantity ?? item.qty ?? 1,
          unitPrice: item.unitPrice ?? item.amount ?? 0,
          amount:
            item.amount ??
            item.totalAmount ??
            item.debitAmount ??
            item.creditAmount ??
            (item.quantity && item.unitPrice ? item.quantity * item.unitPrice : 0),
        }))
      );
    } else {
      setItems([emptyLineItem()]);
    }
    setDeletedItemIds([]);
  }, [initialValues, defaults, showItemsSection]);

  const cleanItems = useMemo(() => {
    if (!showItemsSection) return [];
    return items.filter((item) => {
      const description = item.description?.trim();
      const qty = Number(item.quantity);
      const price = Number(item.unitPrice);
      const amount = Number(item.amount);
      return description && qty > 0 && price > 0 && amount > 0;
    });
  }, [items, showItemsSection]);

  const totalLineAmount = useMemo(() => {
    if (!showItemsSection) return 0;
    return cleanItems.reduce((sum, item) => sum + Number(item.amount), 0);
  }, [cleanItems, showItemsSection]);

  const totalDisplayValue = showItemsSection ? totalLineAmount : 0;
  const handleChange = (field, value) => {
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  const handleItemChange = (index, field, value) => {
    setItems((prev) =>
      prev.map((item, idx) => {
        if (idx !== index) return item;
        const updated = { ...item, [field]: value };
        if (field === "quantity" || field === "unitPrice") {
          const qty = Number(updated.quantity);
          const price = Number(updated.unitPrice);
          updated.amount = qty > 0 && price > 0 ? qty * price : 0;
        }
        return updated;
      })
    );
  };

  const addItem = () => setItems((prev) => [...prev, emptyLineItem()]);

  const removeItem = (index) => {
    setItems((prev) => {
      if (prev.length === 1) return prev;
      const item = prev[index];
      if (item?.voucherItemId) {
        setDeletedItemIds((ids) => [...ids, item.voucherItemId]);
      }
      return prev.filter((_, idx) => idx !== index);
    });
  };

  const validate = () => {
    const nextErrors = {};
    if (!form.voucherDate) {
      nextErrors.voucherDate = "Ngày phiếu chi là bắt buộc";
    }
    if (!form.companyInfo?.trim()) {
      nextErrors.companyInfo = "Thông tin đơn vị nhận là bắt buộc";
    } else if (form.companyInfo.length > 1000) {
      nextErrors.companyInfo = "Thông tin đơn vị nhận không vượt quá 1000 ký tự";
    }
    if (form.description && form.description.length > 1000) {
      nextErrors.description = "Diễn giải không vượt quá 1000 ký tự";
    }
    if (showItemsSection) {
      const invalidItem = items.find((item) => {
        const description = item.description?.trim();
        const qty = Number(item.quantity);
        const price = Number(item.unitPrice);
        const amount = Number(item.amount);
        return !description || qty <= 0 || price <= 0 || amount <= 0;
      });
      if (invalidItem) {
        nextErrors.items =
          "Mỗi dòng chi tiết cần mô tả, số lượng, đơn giá và thành tiền lớn hơn 0";
      }
      if (cleanItems.length === 0 || totalLineAmount <= 0) {
        nextErrors.items = "Cần ít nhất một dòng chi tiết hợp lệ";
      }
    }
    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!editable) return;
    if (!validate()) return;

    const payload = {
      voucherNumber: form.voucherNo?.trim() || null,
      voucherDate: form.voucherDate,
      companyInfo: form.companyInfo?.trim() || null,
      totalAmount: showItemsSection ? totalLineAmount : 0,
      description: form.description?.trim() || null,
      status: "DRAFT",
    };

    const meta = {};
    if (showItemsSection) {
      meta.raw = {
        items,
        deletedItemIds,
      };
    }

    await onSubmit(payload, Object.keys(meta).length ? meta : undefined);
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {serverMsg && (
        <div
          className={`rounded-2xl border px-4 py-3 text-sm font-medium ${
            serverMsg.type === "success"
              ? "bg-emerald-50 border-emerald-200 text-emerald-700"
              : serverMsg.type === "error"
              ? "bg-rose-50 border-rose-200 text-rose-700"
              : "bg-sky-50 border-sky-200 text-sky-700"
          }`}
        >
          {serverMsg.text}
        </div>
      )}

      <div className="rounded-3xl border border-slate-200 bg-white shadow-lg overflow-hidden">
        <div className="bg-gradient-to-r from-indigo-600 to-sky-500 px-6 py-5 text-white flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div>
            <p className="text-xs uppercase tracking-wide text-indigo-100">
              Thông tin chung
            </p>
            <h3 className="text-2xl font-semibold">
              {mode === "create" ? "Phiếu chi thủ công" : "Cập nhật phiếu chi"}
            </h3>
          </div>
          <div className="text-right">
            <p className="text-xs uppercase tracking-wide text-indigo-100">Tổng số tiền</p>
            <p className="text-3xl font-bold">{amountFormatter.format(totalDisplayValue)}</p>
          </div>
        </div>
        <div className="p-6 space-y-5">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Số phiếu chi
              </label>
              <input
                value={form.voucherNo}
                onChange={(e) => handleChange("voucherNo", e.target.value)}
                className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200 disabled:bg-slate-50"
                placeholder="PV-2025-001"
                disabled={!editable}
                autoComplete="off"
              />
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Ngày phiếu chi *
              </label>
              <input
                type="date"
                value={form.voucherDate}
                onChange={(e) => handleChange("voucherDate", e.target.value)}
                className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200 disabled:bg-slate-50"
                disabled={!editable}
              />
              {errors.voucherDate && (
                <p className="mt-1 text-xs text-rose-500">{errors.voucherDate}</p>
              )}
            </div>
            <div className="md:col-span-2">
              <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Thông tin đơn vị nhận *
              </label>
              <textarea
                value={form.companyInfo}
                onChange={(e) => handleChange("companyInfo", e.target.value)}
                rows={3}
                className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200 disabled:bg-slate-50"
                placeholder="Tên đơn vị/đối tác, tài khoản hoặc ghi chú"
                disabled={!editable}
              />
              {errors.companyInfo && (
                <p className="mt-1 text-xs text-rose-500">{errors.companyInfo}</p>
              )}
            </div>
          </div>
          <div>
            <label className="text-xs font-semibold uppercase tracking-wide text-slate-500">
              Mô tả
            </label>
              <textarea
                value={form.description}
                onChange={(e) => handleChange("description", e.target.value)}
                rows={3}
                className="mt-2 w-full rounded-xl border border-slate-200 bg-white px-4 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200 disabled:bg-slate-50"
                placeholder="Thông tin chi tiết về chứng từ"
                disabled={!editable}
              />
            {errors.description && (
              <p className="mt-1 text-xs text-rose-500">{errors.description}</p>
            )}
          </div>
        </div>
      </div>

      {showItemsSection && (
        <div className="rounded-3xl border border-slate-200 bg-white shadow-sm overflow-hidden">
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3 border-b border-slate-100 px-6 py-4 bg-slate-50">
            <div>
              <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">
                Dòng chi tiết
              </p>
              <h3 className="text-lg font-semibold text-slate-800">Chi tiết chi</h3>
            </div>
            {editable && (
              <button
                type="button"
                onClick={addItem}
                className="inline-flex items-center gap-2 rounded-full bg-indigo-600 px-4 py-2 text-xs font-semibold text-white shadow hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
              >
                + Thêm dòng
              </button>
            )}
          </div>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-100 text-sm">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-4 py-3 text-left font-semibold text-slate-600">Mô tả</th>
                  <th className="px-4 py-3 text-left font-semibold text-slate-600">Số lượng</th>
                  <th className="px-4 py-3 text-left font-semibold text-slate-600">Đơn giá</th>
                  <th className="px-4 py-3 text-left font-semibold text-slate-600">Thành tiền</th>
                  <th className="px-4 py-3 text-right font-semibold text-slate-600" />
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white">
                {items.map((item, index) => (
                  <tr key={index} className="hover:bg-slate-50">
                    <td className="px-4 py-3">
                      <input
                        value={item.description}
                        onChange={(e) => handleItemChange(index, "description", e.target.value)}
                        className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200 disabled:bg-slate-50"
                        placeholder="Mô tả khoản chi"
                        disabled={!editable}
                      />
                    </td>
                    <td className="px-4 py-3">
                      <input
                        type="number"
                        min="0"
                        step="1"
                        value={item.quantity}
                        onChange={(e) => handleItemChange(index, "quantity", e.target.value)}
                        className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200 disabled:bg-slate-50"
                        placeholder="1"
                        disabled={!editable}
                      />
                    </td>
                    <td className="px-4 py-3">
                      <input
                        type="number"
                        min="0"
                        step="1000"
                        value={item.unitPrice}
                        onChange={(e) => handleItemChange(index, "unitPrice", e.target.value)}
                        className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200 disabled:bg-slate-50"
                        placeholder="0"
                        disabled={!editable}
                      />
                    </td>
                    <td className="px-4 py-3">
                      <div className="font-semibold text-slate-800">
                        {amountFormatter.format(Number(item.amount) || 0)}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-right">
                      {editable && items.length > 1 && (
                        <button
                          type="button"
                          onClick={() => removeItem(index)}
                          className="text-xs font-semibold text-rose-600 hover:text-rose-800"
                        >
                          Xoá
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="px-6 py-4 border-t border-slate-100 bg-slate-50">
            <div className="flex flex-col gap-2 text-sm text-slate-600">
              <div className="flex items-center justify-between">
                <span>Tổng phiếu chi:</span>
                <span className="font-semibold text-slate-800">
                  {amountFormatter.format(totalLineAmount || 0)}
                </span>
              </div>
              {errors.items && <p className="text-xs text-rose-500">{errors.items}</p>}
            </div>
          </div>
        </div>
      )}

      <div className="flex justify-end">
        <button
          type="submit"
          disabled={!editable}
          className={`inline-flex items-center gap-2 rounded-full px-6 py-2.5 text-sm font-semibold text-white shadow-lg shadow-indigo-300/40 transition focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-600 ${
            editable ? "bg-indigo-600 hover:bg-indigo-700" : "bg-slate-300 cursor-not-allowed"
          }`}
        >
          {submitting ? "Đang xử lý..." : mode === "create" ? "Tạo phiếu chi" : "Lưu thay đổi"}
        </button>
      </div>
    </form>
  );
}

