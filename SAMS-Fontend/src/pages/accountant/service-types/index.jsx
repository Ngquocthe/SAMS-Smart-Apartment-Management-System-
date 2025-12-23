import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import ROUTER_PAGE from "../../../constants/Routes";
import ServiceTypeTable from "./ServiceTypeTable";
import {
  listServiceType,
  deleteServiceType,
  enableServiceType,
  disableServiceType,
} from "../../../features/accountant/servicetypesApi";
import CreateServiceType from "./CreateServiceTypePage";
import UpdateServiceType from "./UpdateServiceTypePage";
import Toast from "../../../components/Toast";

export default function ServiceTypesPage() {
  const [q, setQ] = useState("");
  const [page, setPage] = useState(1);
  const [data, setData] = useState({ items: [], totalPages: 0, page: 1 });
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState("");
  const [toast, setToast] = useState({ show: false, message: "", type: "success" });

  // 👇 modal state
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showUpdateModal, setShowUpdateModal] = useState(false);
  const [selectedServiceType, setSelectedServiceType] = useState(null);

  const navigate = useNavigate();

  const load = async (overrides = {}) => {
    try {
      setLoading(true);
      setErr("");
      const res = await listServiceType({ q, page, pageSize: 10, ...overrides });
      setData(res);
    } catch {
      setErr("Không thể tải loại dịch vụ.");
    } finally {
      setLoading(false);
    }
  };
  useEffect(() => { load(); }, [page]);

  const onSearch = (e) => { e.preventDefault(); setPage(1); load({ page: 1 }); };
  const showToast = (message, type = "success") => {
    setToast({ show: true, message, type });
  };
  const onCreate = () => setShowCreateModal(true);

  const onEdit = (row) => {
    setSelectedServiceType(row);
    setShowUpdateModal(true);
  };

  const onDelete = async (row) => {
    if (!window.confirm(`Xoá "${row.name}" (${row.code})?`)) return;
    try {
      await deleteServiceType(row.serviceTypeId);
      showToast("Xoá thành công", "success");
      load();
    } catch (e) {
      showToast(e?.response?.data?.error || e.message || "Xoá thất bại", "error");
    }
  };

  const onToggleActive = async (row) => {
    try {
      if (row.isActive) {
        await disableServiceType(row.serviceTypeId);
        showToast("Đã ngưng kích hoạt", "success");
      } else {
        await enableServiceType(row.serviceTypeId);
        showToast("Đã kích hoạt", "success");
      }
      load();
    } catch (e) {
      showToast(e?.response?.data?.error || e.message || "Không thể đổi trạng thái", "error");
    }
  };

  const handleCreateSuccess = (created) => {
    setShowCreateModal(false);
    load({ page: 1 });
    showToast(`Đã tạo ${created?.name || "loại dịch vụ"} (${created?.code || "-"})`, "success");
  };

  const handleUpdateSuccess = (info) => {
    setShowUpdateModal(false);
    setSelectedServiceType(null);
    load();
    showToast(info?.text || "Cập nhật thành công", info?.type || "success");
  };

  const onOpenPrices = (row) => {
    navigate(
      ROUTER_PAGE.ACCOUNTANT.SERVICE_TYPE_PRICES.replace(":id", row.serviceTypeId)
    );
  };

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Loại dịch vụ</h1>
        <button onClick={onCreate} className="px-4 py-2 rounded-xl bg-indigo-600 text-white hover:bg-indigo-700">
          + Tạo loại dịch vụ
        </button>
      </div>

      <form onSubmit={onSearch} className="flex gap-2">
        <input
          value={q}
          onChange={(e) => setQ(e.target.value)}
          placeholder="Tìm mã hoặc tên…"
          className="border rounded-xl px-3 py-2 w-full max-w-md"
        />
        <button className="px-4 py-2 rounded-xl bg-black text-white">Tìm kiếm</button>
      </form>

      <Toast
        show={toast.show}
        message={toast.message}
        type={toast.type}
        onClose={() => setToast((prev) => ({ ...prev, show: false }))}
      />

      {loading && <div>Đang tải…</div>}
      {err && <div className="text-red-600">{err}</div>}

      {!loading && !err && (
        <>
          <ServiceTypeTable
            data={data.items}
            onEdit={onEdit}
            onDelete={onDelete}
            onToggleActive={onToggleActive}
            onOpenPrices={onOpenPrices}
          />

          <div className="flex items-center gap-2">
            <button
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              className="px-3 py-1 rounded border disabled:opacity-50"
            >Trước</button>
            <span>Trang {data.page ?? page} / {data.totalPages || 1}</span>
            <button
              disabled={page >= (data.totalPages || 1)}
              onClick={() => setPage((p) => p + 1)}
              className="px-3 py-1 rounded border disabled:opacity-50"
            >Sau</button>
          </div>
        </>
      )}

      {/* ✅ Modal tạo */}
      <CreateServiceType
        show={showCreateModal}
        onHide={() => setShowCreateModal(false)}
        onSuccess={handleCreateSuccess}
      />

      {/* ✅ Modal cập nhật */}
      <UpdateServiceType
        show={showUpdateModal}
        serviceType={selectedServiceType}
        onHide={() => { setShowUpdateModal(false); setSelectedServiceType(null); }}
        onSuccess={handleUpdateSuccess}
      />
    </div>
  );
}
