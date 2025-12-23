import React, { useEffect, useMemo, useState } from "react";
import { listServiceTypeCategories } from "../../../features/accountant/servicetypesApi";
const normalizeCode = (s) => (s || "").trim().toUpperCase();

export default function ServiceTypeForm({
  mode = "create",               // "create" | "update"
  initialValues,                 // { code, name, category, unit, isMandatory, isRecurring }
  onSubmit,                      // async (payload) => {}
  submitting = false,
  serverMsg = null,              // { type: "success"|"error"|"info", text: string }
}) {
  const defaults = useMemo(() => ({
    code: "",
    name: "",
    categoryId: "",
    unit: "",
    isMandatory: false,
    isRecurring: false,
  }), []);

  const [form, setForm] = useState({ ...defaults, ...(initialValues || {}) });
  const [errors, setErrors] = useState({});
  const [categories, setCategories] = useState([]);

  useEffect(() => {
  let alive = true;
  (async () => {
    try {
      const data = await listServiceTypeCategories();
      if (!alive) return;
      setCategories((data || []).map(x => ({
        value: x.categoryId,
        label: x.name,
        desc: x.description
      })));
    } catch {
      setCategories([]);
    }
  })();
  return () => { alive = false; };
}, []);

  useEffect(() => {
    setForm({ ...defaults, ...(initialValues || {}) });
  }, [initialValues, defaults]);

  const updateField = (name, value) => {
    setForm((prev) => ({ ...prev, [name]: value }));
    if (name === "isMandatory" && value === true) {
      setForm((prev) => ({ ...prev, isRecurring: true }));
    }
  };

  const onChange = (e) => {
    const { name, value, type, checked } = e.target;
    updateField(name, type === "checkbox" ? checked : value);
  };

  const validate = () => {
    const e = {};
    if (mode === "create") {
      if (!form.code?.trim()) e.code = "Mã là bắt buộc";
      else if (form.code.length < 2 || form.code.length > 50) e.code = "Mã phải từ 2 đến 50 ký tự";
    }
    if (!form.name?.trim()) e.name = "Tên là bắt buộc";
    if (!form.categoryId?.trim()) e.categoryId = "Nhóm là bắt buộc";
    if (!form.unit?.trim()) e.unit = "Đơn vị là bắt buộc";
    if (form.isMandatory && !form.isRecurring) e.isRecurring = "Đánh dấu bắt buộc thì phải lặp định kỳ";
    setErrors(e);
    return Object.keys(e).length === 0;
  };

  const handleSubmit = async (ev) => {
    ev.preventDefault();
    if (!validate()) return;

    // chuẩn bị payload cho backend (không còn description)
    const payload = {
      ...(mode === "create" ? { code: normalizeCode(form.code) } : {}),
      name: form.name.trim(),
      categoryId: form.categoryId,
      unit: form.unit.trim(),
      isMandatory: !!form.isMandatory,
      isRecurring: !!form.isRecurring,
    };

    await onSubmit(payload, { raw: form });
  };

  const input = "w-full rounded-xl px-3 py-2 border bg-transparent focus:ring-2 focus:ring-indigo-500";
  const label = "text-sm font-medium mb-1 block";
  const err   = "text-xs text-red-500 mt-1";

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {serverMsg && (
        <div className={
          "rounded-xl px-3 py-2 " +
          (serverMsg.type === "success" ? "bg-green-100 text-green-800"
           : serverMsg.type === "error" ? "bg-red-100 text-red-800"
           : "bg-blue-100 text-blue-800")
        }>
          {serverMsg.text}
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Code */}
        <div>
          <label className={label}>Mã {mode === "create" && "*"} </label>
          <input
            name="code"
            value={form.code}
            onChange={onChange}
            onBlur={() => mode === "create" && updateField("code", normalizeCode(form.code))}
            className={input}
            placeholder="WATER"
            disabled={mode !== "create"}     // update: khoá code
            autoComplete="off"
          />
          {errors.code && <div className={err}>{errors.code}</div>}
        </div>

        {/* Name */}
        <div>
          <label className={label}>Tên *</label>
          <input
            name="name"
            value={form.name}
            onChange={onChange}
            className={input}
            placeholder="Phí nước"
          />
          {errors.name && <div className={err}>{errors.name}</div>}
        </div>

        {/* Category */}
        <div>
          <label className={label}>Nhóm dịch vụ *</label>
          <select
            name="categoryId"
            value={form.categoryId}
            onChange={onChange}
            className={input}
          >
            <option value="">-- Chọn --</option>
            {categories.map((c) => <option key={c.value} value={c.value}>{c.label}</option>)}
          </select>
          {errors.categoryId && <div className={err}>{errors.categoryId}</div>}
        </div>

        {/* Unit */}
        <div>
          <label className={label}>Đơn vị tính *</label>
          <input
            name="unit"
            value={form.unit}
            onChange={onChange}
            className={input}
            placeholder="M3 / KWH / THÁNG"
          />
          {errors.unit && <div className={err}>{errors.unit}</div>}
        </div>
      </div>

      {/* Flags */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <label className="flex items-center gap-2">
          <input type="checkbox" name="isMandatory" checked={form.isMandatory} onChange={onChange} />
          <span>Bắt buộc</span>
        </label>

        <label className="flex items-center gap-2">
          <input type="checkbox" name="isRecurring" checked={form.isRecurring} onChange={onChange} />
          <span>Lặp định kỳ</span>
        </label>
        {errors.isRecurring && <div className={err}>{errors.isRecurring}</div>}
      </div>

      <div className="flex gap-2">
        <button
          type="submit"
          disabled={submitting}
          className={
            "px-4 py-2 rounded-xl text-white " +
            (submitting ? "bg-indigo-300 cursor-not-allowed" : "bg-indigo-600 hover:bg-indigo-700")
          }
        >
          {submitting ? (mode === "create" ? "Đang tạo..." : "Đang lưu...") : (mode === "create" ? "Tạo mới" : "Lưu thay đổi")}
        </button>
      </div>
    </form>
  );
}
