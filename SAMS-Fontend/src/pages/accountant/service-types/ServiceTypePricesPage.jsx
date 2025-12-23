import { useEffect, useMemo, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Button,
  Card,
  DatePicker,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Table,
  Tag,
  Popconfirm,
  message,
} from "antd";
import dayjs from "dayjs";
import {
  listServicePrices,
  createServicePrice,
  updateServicePrice,
  cancelServicePrice,
} from "../../../features/accountant/servicepricesApi";

const STATUS_LABELS = {
  APPROVED: "Đã duyệt",
  CANCELED: "Đã huỷ",
};

export default function ServiceTypePricesPage() {
  const nav = useNavigate();
  const { id: serviceTypeId } = useParams();
  // filters / paging
  const [q, setQ] = useState("");
  const [status, setStatus] = useState();
  const [fromDate, setFromDate] = useState();
  const [toDate, setToDate] = useState();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const params = useMemo(
    () => ({
      page,
      pageSize,
      q: q || undefined,
      status: status || undefined,
      fromDate: fromDate ? dayjs(fromDate).format("YYYY-MM-DD") : undefined,
      toDate: toDate ? dayjs(toDate).format("YYYY-MM-DD") : undefined,
      sortBy: "EffectiveDate",
      sortDir: "desc",
    }),
    [page, pageSize, q, status, fromDate, toDate]
  );

  const [loading, setLoading] = useState(false);
  const [data, setData] = useState({
    items: [],
    totalItems: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
  });

  async function fetchData() {
    setLoading(true);
    try {
      const res = await listServicePrices(serviceTypeId, params);
      setData(res);
    } catch (e) {
      message.error(e.message || "Không thể tải dữ liệu");
    } finally {
      setLoading(false);
    }
  }
  useEffect(() => {
    if (serviceTypeId) fetchData();
  }, [serviceTypeId, params]);

  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState(null); // id
  const [form] = Form.useForm();

  const openCreate = () => {
    setEditing(null);
    form.resetFields();
    setOpen(true);
  };
  const openEdit = (row) => {
    setEditing(row.servicePrices);
    form.setFieldsValue({
      unitPrice: row.unitPrice,
      effectiveDate: dayjs(row.effectiveDate),
      endDate: row.endDate ? dayjs(row.endDate) : null,
      notes: row.notes,
    });
    setOpen(true);
  };

  async function onSubmit() {
    try {
      const v = await form.validateFields();
      const payload = {
        unitPrice: Number(v.unitPrice),
        effectiveDate: v.effectiveDate.format("YYYY-MM-DD"),
        endDate: v.endDate ? v.endDate.format("YYYY-MM-DD") : null,
        notes: v.notes || null,
      };
      if (!editing) {
        await createServicePrice(serviceTypeId, payload, {
          autoClosePrevious: true,
        });
        message.success("Đã tạo giá mới");
      } else {
        await updateServicePrice(serviceTypeId, editing, payload);
        message.success("Đã lưu");
      }
      setOpen(false);
      fetchData();
    } catch (e) {
      if (e?.message) message.error(e.message);
    }
  }

  async function onCancelPrice(id) {
    try {
      console.log("CALL DELETE", serviceTypeId, id);
      await cancelServicePrice(serviceTypeId, id); // DELETE 204
      message.success("Đã huỷ giá");
      fetchData(); // reload list
    } catch (e) {
      console.error("DELETE failed", e);
      message.error(e?.response?.data?.detail || e.message || "Huỷ không thành công");
    }
  }

  return (
    <Card
      title="Giá dịch vụ"
      extra={<Button onClick={() => nav(-1)}>Quay lại</Button>}
    >
      <Space style={{ marginBottom: 12 }} wrap>
        <Input
          placeholder="Tìm mã/tên/ghi chú"
          allowClear
          value={q}
          onChange={(e) => setQ(e.target.value)}
          style={{ width: 260 }}
        />
        <Select
          placeholder="Trạng thái"
          allowClear
          value={status}
          onChange={setStatus}
          style={{ width: 160 }}
          options={[
            { value: "APPROVED", label: "Đã duyệt" },
            { value: "CANCELED", label: "Đã huỷ" },
          ]}
        />
        <DatePicker
          placeholder="Từ ngày"
          value={fromDate}
          onChange={setFromDate}
        />
        <DatePicker placeholder="Đến ngày" value={toDate} onChange={setToDate} />
        <Button
          type="primary"
          onClick={() => {
            setPage(1);
            fetchData();
          }}
        >
          Áp dụng
        </Button>
        <Button
          onClick={() => {
            setQ("");
            setStatus();
            setFromDate();
            setToDate();
            setPage(1);
          }}
        >
          Xoá lọc
        </Button>
      </Space>

      <div style={{ marginBottom: 12 }}>
        <Button type="primary" onClick={openCreate}>
          Thêm giá
        </Button>
      </div>

      <Table
        rowKey="servicePrices"
        loading={loading}
        dataSource={data.items}
        pagination={{
          current: data.page ?? page,
          pageSize: data.pageSize ?? pageSize,
          total: data.totalItems ?? 0,
          showSizeChanger: true,
          onChange: (p, s) => {
            setPage(p);
            setPageSize(s);
          },
        }}
        columns={[
          { title: "Hiệu lực", dataIndex: "effectiveDate" },
          {
            title: "Kết thúc",
            dataIndex: "endDate",
            render: (v) => v ?? <Tag color="blue">Không giới hạn</Tag>,
          },
          {
            title: "Đơn giá",
            dataIndex: "unitPrice",
            render: (v) => Number(v).toLocaleString(),
          },
          {
            title: "Trạng thái",
            dataIndex: "status",
            render: (s) => (
              <Tag color={s === "APPROVED" ? "green" : "default"}>{STATUS_LABELS[s] || s}</Tag>
            ),
          },
          { title: "Ghi chú", dataIndex: "notes" },
          {
            title: "Thao tác",
            render: (_, r) => (
              <Space>
                <Button
                  size="small"
                  disabled={r.status === "CANCELED"}
                  onClick={() => openEdit(r)}
                >
                  Sửa
                </Button>
                <Popconfirm
                  title="Huỷ mức giá này?"
                  okText="Đồng ý"
                  cancelText="Không"
                  onConfirm={() => onCancelPrice(r.servicePrices)} // gọi thẳng API
                >
                  <Button
                    size="small"
                    danger
                    disabled={r.status === "CANCELED"}
                  >
                    Huỷ
                  </Button>
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        open={open}
        title={editing ? "Chỉnh sửa giá" : "Thêm giá"}
        onCancel={() => setOpen(false)}
        onOk={onSubmit}
        okText={editing ? "Lưu" : "Tạo"}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="unitPrice"
            label="Đơn giá"
            rules={[{ required: true, message: "Bắt buộc" }]}
          >
            <InputNumber min={0.01} step={1000} style={{ width: "100%" }} />
          </Form.Item>
          <Form.Item
            name="effectiveDate"
            label="Ngày hiệu lực"
            rules={[{ required: true, message: "Bắt buộc" }]}
          >
            <DatePicker format="YYYY-MM-DD" />
          </Form.Item>
          <Form.Item name="endDate" label="Ngày kết thúc">
            <DatePicker format="YYYY-MM-DD" />
          </Form.Item>
          <Form.Item name="notes" label="Ghi chú">
            <Input.TextArea rows={3} maxLength={1000} />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
}
