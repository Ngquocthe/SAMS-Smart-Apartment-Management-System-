import React, { useState } from "react";
import { Modal, Button } from "react-bootstrap";
import ServiceTypeForm from "./ServiceTypeForm";
import { createServiceType } from "../../../features/accountant/servicetypesApi";

export default function CreateServiceType({ show = false, onHide, onSuccess }) {
  const [submitting, setSubmitting] = useState(false);
  const [msg, setMsg] = useState(null);

  const handleCreate = async (payload) => {
    setMsg(null);
    setSubmitting(true);
    try {
      const created = await createServiceType(payload);
      setMsg({ type: "success", text: `Đã tạo: ${created.name} (${created.code})` });
      onSuccess?.(created);
    } catch (e) {
      setMsg({ type: "error", text: e.message || "Tạo thất bại" });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal show={show} onHide={onHide} centered backdrop="static">
      <Modal.Header closeButton><Modal.Title>Tạo loại dịch vụ</Modal.Title></Modal.Header>
      <Modal.Body>
        <ServiceTypeForm mode="create" onSubmit={handleCreate} submitting={submitting} serverMsg={msg} />
      </Modal.Body>
      <Modal.Footer>
        <Button variant="secondary" onClick={onHide}>Đóng</Button>
      </Modal.Footer>
    </Modal>
  );
}
