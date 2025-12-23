import React, { useEffect, useState } from "react";
import { Modal, Button } from "react-bootstrap";
import ServiceTypeForm from "./ServiceTypeForm";
import { updateServiceType } from "../../../features/accountant/servicetypesApi";

export default function UpdateServiceType({ show = false, onHide, onSuccess, serviceType }) {
  // ❌ không return sớm trước hooks
  const [form, setForm] = useState(serviceType ?? null);
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState(serviceType ? null : { type: "info", text: "Chưa có dữ liệu ban đầu. Vui lòng sửa thông tin và lưu." });

  useEffect(() => {
  setForm(serviceType ?? null);
  if (serviceType) {
    setMsg(null);
  } else {
    setMsg({ type: "info", text: "Chưa có dữ liệu ban đầu. Vui lòng sửa thông tin và lưu." });
  }
}, [serviceType]);

  const handleUpdate = async (payload) => {
    if (!serviceType?.serviceTypeId) {
      setMsg({ type: "error", text: "Thiếu mã loại dịch vụ" });
      return;
    }
    setMsg(null);
    setSaving(true);
    try {
      await updateServiceType(serviceType.serviceTypeId, payload);
      setMsg({ type: "success", text: "Cập nhật thành công" });
      onSuccess?.({ text: "Cập nhật thành công", type: "success" });
    } catch (e) {
      const text = e.message || "Cập nhật thất bại";
      setMsg({ type: "error", text });
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal show={show} onHide={onHide} centered backdrop="static">
      <Modal.Header closeButton><Modal.Title>Chỉnh sửa loại dịch vụ</Modal.Title></Modal.Header>
      <Modal.Body>
        <ServiceTypeForm mode="update" initialValues={form} onSubmit={handleUpdate} submitting={saving} serverMsg={msg} />
      </Modal.Body>
      <Modal.Footer>
        <Button variant="secondary" onClick={onHide}>Đóng</Button>
      </Modal.Footer>
    </Modal>
  );
}
