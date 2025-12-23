import React, { useMemo, useState } from "react";
import { Button, Form, Input, Select, message, Card } from "antd";
import { PlusOutlined } from "@ant-design/icons";
import ticketsApi from "../../features/tickets/ticketsApi";

const categoryOptions = [
  { label: "Bảo trì", value: "Bảo trì", scope: "Theo căn hộ" },
  { label: "Tiếp tân", value: "Tiếp tân", scope: "Tòa nhà" },
  { label: "An ninh", value: "An ninh", scope: "Tòa nhà" },
  { label: "Hóa đơn", value: "Hóa đơn", scope: "Tòa nhà" },
  { label: "Khiếu nại", value: "Khiếu nại", scope: "Tòa nhà" },
  { label: "Vệ sinh", value: "Vệ sinh", scope: "Tòa nhà" },
  { label: "Bãi đỗ xe", value: "Bãi đỗ xe", scope: "Tòa nhà" },
  { label: "CNTT", value: "CNTT", scope: "Tòa nhà" },
  { label: "Tiện ích", value: "Tiện ích", scope: "Tòa nhà" },
  { label: "Khác", value: "Khác", scope: "Tòa nhà" },
];

const priorityOptions = [
  { label: "Thấp", value: "Thấp" },
  { label: "Bình thường", value: "Bình thường" },
  { label: "Cao", value: "Cao" },
  { label: "Khẩn cấp", value: "Khẩn cấp" },
];

export default function CreateTicket() {
  const [loading, setLoading] = useState(false);
  const [form] = Form.useForm();

  const defaultCategory = useMemo(() => categoryOptions[0].value, []);
  const defaultPriority = useMemo(() => priorityOptions[1].value, []);

  const submitCreate = async () => {
    setLoading(true);
    try {
      const values = await form.validateFields();
      const selectedCategory = categoryOptions.find(
        (item) => item.value === values.category
      );
      const payload = {
        category: values.category,
        priority: values.priority,
        scope: selectedCategory?.scope ?? "Tòa nhà",
        subject: String(values.subject).trim(),
        description: values.description
          ? String(values.description).trim()
          : undefined,
        createdByUserId: values.createdByUserId || undefined,
      };
      await ticketsApi.create(payload);
      message.success(
        "Đã tạo ticket thành công! Chúng tôi sẽ xử lý sớm nhất có thể."
      );
      form.resetFields();
    } catch (e) {
      if (!e?.errorFields) message.error("Tạo ticket thất bại");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 flex">
      <main className="flex-1">
        <div className="px-8 py-6">
          <h1 className="text-3xl font-bold mb-6">Tạo yêu cầu hỗ trợ</h1>

          <Card className="max-w-2xl">
            <Form
              form={form}
              layout="vertical"
              onFinish={submitCreate}
              initialValues={{
                category: defaultCategory,
                priority: defaultPriority,
              }}
            >
              <Form.Item
                name="subject"
                label="Tiêu đề"
                rules={[{ required: true, message: "Nhập tiêu đề yêu cầu" }]}
              >
                <Input placeholder="Ví dụ: Hỏng điều hòa phòng 101" />
              </Form.Item>

              <Form.Item
                name="category"
                label="Loại yêu cầu"
                rules={[{ required: true, message: "Chọn loại yêu cầu" }]}
              >
                <Select
                  placeholder="Chọn loại yêu cầu"
                  showSearch
                  allowClear
                  options={categoryOptions}
                />
              </Form.Item>

              <Form.Item
                name="priority"
                label="Mức độ ưu tiên"
                rules={[{ required: true, message: "Chọn mức độ ưu tiên" }]}
              >
                <Select
                  placeholder="Chọn mức độ ưu tiên"
                  options={priorityOptions}
                />
              </Form.Item>

              <Form.Item name="description" label="Mô tả chi tiết">
                <Input.TextArea
                  rows={4}
                  placeholder="Mô tả chi tiết về vấn đề cần hỗ trợ..."
                />
              </Form.Item>

              <Form.Item name="createdByUserId" label="Mã cư dân (tùy chọn)">
                <Input placeholder="Nhập mã cư dân nếu có" />
              </Form.Item>

              <Form.Item>
                <Button
                  type="primary"
                  htmlType="submit"
                  icon={<PlusOutlined />}
                  loading={loading}
                  size="large"
                  className="w-full"
                >
                  Gửi yêu cầu
                </Button>
              </Form.Item>
            </Form>
          </Card>

          <div className="mt-6 p-4 bg-blue-50 rounded-lg">
            <h3 className="font-semibold text-blue-800 mb-2">Lưu ý:</h3>
            <ul className="text-blue-700 text-sm space-y-1">
              <li>• Yêu cầu của bạn sẽ được lễ tân tiếp nhận và xử lý</li>
              <li>• Với yêu cầu khẩn cấp, vui lòng liên hệ trực tiếp lễ tân</li>
              <li>
                • Bạn có thể theo dõi trạng thái yêu cầu trong mục "Yêu cầu của
                tôi"
              </li>
            </ul>
          </div>
        </div>
      </main>
    </div>
  );
}
