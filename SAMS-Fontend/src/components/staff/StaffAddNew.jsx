import { useEffect, useMemo, useState } from "react";
import {
  Form,
  Input,
  Select,
  DatePicker,
  InputNumber,
  Card,
  Row,
  Col,
  Upload,
  Button,
  Divider,
  Typography,
  Space,
  Tooltip,
  Avatar,
  message,
} from "antd";
import {
  UserOutlined,
  IdcardOutlined,
  HomeOutlined,
  SafetyCertificateOutlined,
  BankOutlined,
  DollarOutlined,
  PhoneOutlined,
  MailOutlined,
  CalendarOutlined,
  FileImageOutlined,
  ReloadOutlined,
  CheckCircleOutlined,
} from "@ant-design/icons";
import dayjs from "dayjs";

import { coreApi } from "../../features/building/coreApi";
import { staffApi } from "../../features/staff/staffApi";
import { keycloakApi } from "../../features/keycloak/keycloakApi";

const { Title, Text } = Typography;
const { Option } = Select;

function debounce(fn, delay) {
  let timer;
  return function (...args) {
    clearTimeout(timer);
    timer = setTimeout(() => fn.apply(this, args), delay);
  };
}

export default function StaffCreateForm({ onSubmit }) {
  const [form] = Form.useForm();

  const [buildings, setBuildings] = useState([]);
  const [schemaName, setSchemaName] = useState(null);

  const [roles, setRoles] = useState([]);
  const [accessRoles, setAccessRoles] = useState([]);

  const [avatarPreview, setAvatarPreview] = useState("");
  const [cardPhotoPreview, setCardPhotoPreview] = useState("");

  const [avatarFile, setAvatarFile] = useState(null);
  const [cardPhotoFile, setCardPhotoFile] = useState(null);

  const [banks, setBanks] = useState([]);
  const [loadingBanks, setLoadingBanks] = useState(false);

  const [loadingSubmit, setLoadingSubmit] = useState(false);

  const [search, setSearch] = useState("");

  const setPreviewFromFile = (file, setter) => {
    if (!file) return;
    const url = URL.createObjectURL(file);
    setter((prev) => {
      if (prev && prev.startsWith("blob:")) URL.revokeObjectURL(prev);
      return url;
    });
  };

  const normalizeCurrency = (v) => (typeof v === "number" ? v : undefined);

  const required = (msg) => [{ required: true, message: msg }];

  useEffect(() => {
    (async () => {
      try {
        const items = await coreApi.getBuildings();
        setBuildings(items);

        if (items.length > 0) {
          const first = items[0];
          form.setFieldsValue({
            assignment: {
              buildingId: first.id,
            },
          });
          setSchemaName(first.schemaName || null);
        }

        const kcRes = await keycloakApi.getClientRoles();
        let kcItems = [];
        if (Array.isArray(kcRes)) {
          kcItems = kcRes;
        } else if (kcRes && Array.isArray(kcRes.data)) {
          kcItems = kcRes.data;
        }
        setAccessRoles(kcItems);
      } catch (e) {
        console.error(e);
        message.error("Không tải được danh sách tòa nhà / quyền truy cập");
      }
    })();
  }, [form]);

  useEffect(() => {
    if (!schemaName) {
      setRoles([]);
      return;
    }

    (async () => {
      try {
        const list = await staffApi.getWorkRoles(schemaName);
        setRoles(list);
      } catch (e) {
        console.error(e);
        message.error("Không tải được danh sách vai trò");
        setRoles([]);
      }
    })();
  }, [schemaName]);

  const fetchBanks = async () => {
    setLoadingBanks(true);
    try {
      const res = await fetch("https://api.vietqr.io/v2/banks");
      const json = await res.json();
      if (json && json.code === "00" && Array.isArray(json.data)) {
        setBanks(json.data);
      }
    } catch (e) {
      console.error("Failed to fetch banks", e);
    } finally {
      setLoadingBanks(false);
    }
  };

  useEffect(() => {
    fetchBanks();
  }, []);

  useEffect(() => {
    return () => {
      if (avatarPreview && avatarPreview.startsWith("blob:")) {
        URL.revokeObjectURL(avatarPreview);
      }
      if (cardPhotoPreview && cardPhotoPreview.startsWith("blob:")) {
        URL.revokeObjectURL(cardPhotoPreview);
      }
    };
  }, [avatarPreview, cardPhotoPreview]);

  const handleSearchDebounced = useMemo(
    () =>
      debounce((value) => {
        setSearch(value);
      }, 300),
    []
  );

  const handleSearch = (value) => {
    handleSearchDebounced(value);
  };

  const filteredBanks = useMemo(() => {
    if (!search) return banks;
    const s = search.toLowerCase();
    return banks.filter(
      (b) =>
        (b.name && b.name.toLowerCase().includes(s)) ||
        (b.shortName && b.shortName.toLowerCase().includes(s)) ||
        (b.code && String(b.code).toLowerCase().includes(s))
    );
  }, [banks, search]);

  const handleFinish = async (values) => {
    setLoadingSubmit(true);
    try {
      const formData = new FormData();

      // ===== USER =====
      formData.append("Username", values.user?.username?.trim() || "");
      formData.append("Email", values.user?.email?.trim() || "");
      formData.append("Phone", values.user?.phone?.trim() || "");
      formData.append("FirstName", values.user?.firstName?.trim() || "");
      formData.append("LastName", values.user?.lastName?.trim() || "");
      formData.append(
        "Dob",
        values.user?.dob ? dayjs(values.user.dob).format("YYYY-MM-DD") : ""
      );
      formData.append("Address", values.user?.address?.trim() || "");

      // ===== STAFF PROFILE =====
      formData.append(
        "CurrentAddress",
        values.staff?.currentAddress?.trim() || ""
      );
      formData.append(
        "HireDate",
        values.staff?.hireDate
          ? dayjs(values.staff.hireDate).format("YYYY-MM-DD")
          : ""
      );
      formData.append(
        "TerminationDate",
        values.staff?.terminationDate
          ? dayjs(values.staff.terminationDate).format("YYYY-MM-DD")
          : ""
      );
      formData.append("Notes", values.staff?.notes?.trim() || "");
      formData.append("IsActive", values.staff?.isActive ? "true" : "false");

      formData.append(
        "EmergencyContactName",
        values.staff?.emergencyContactName?.trim() || ""
      );
      formData.append(
        "EmergencyContactPhone",
        values.staff?.emergencyContactPhone?.trim() || ""
      );
      formData.append(
        "EmergencyContactRelation",
        values.staff?.emergencyContactRelation?.trim() || ""
      );

      formData.append(
        "BankAccountNo",
        values.staff?.bankAccountNo?.trim() || ""
      );
      formData.append("BankName", values.staff?.bankName?.trim() || "");
      formData.append("TaxCode", values.staff?.taxCode?.trim() || "");
      formData.append(
        "SocialInsuranceNo",
        values.staff?.socialInsuranceNo?.trim() || ""
      );

      const baseSalary =
        normalizeCurrency(values.staff?.baseSalary) !== undefined
          ? String(normalizeCurrency(values.staff?.baseSalary))
          : "";
      formData.append("BaseSalary", baseSalary);

      formData.append("RoleId", values.staff?.roleId || "");

      formData.append("BuildingId", values.assignment?.buildingId || "");

      (values.assignment?.roles || []).forEach((r) => {
        formData.append("AccessRoles", r);
      });

      if (avatarFile) {
        formData.append("Avatar", avatarFile);
      }
      if (cardPhotoFile) {
        formData.append("CardPhoto", cardPhotoFile);
      }

      if (typeof onSubmit === "function") {
        await onSubmit(formData);
      } else {
        try {
          await staffApi.createStaff(schemaName, formData);
          message.success("Thêm nhân sự thành công");
        } catch (e) {
          console.error(e);
          message.error(e.errorMessage || "Thêm nhân sự thất bại");
          return;
        }
      }

      form.resetFields();
      if (avatarPreview && avatarPreview.startsWith("blob:")) {
        URL.revokeObjectURL(avatarPreview);
      }
      if (cardPhotoPreview && cardPhotoPreview.startsWith("blob:")) {
        URL.revokeObjectURL(cardPhotoPreview);
      }
      setAvatarPreview("");
      setCardPhotoPreview("");
      setAvatarFile(null);
      setCardPhotoFile(null);
    } finally {
      setLoadingSubmit(false);
    }
  };

  const cleanPhone = (p) => (p || "").toString().replace(/[\s\-\.\(\)]/g, "");

  const isValidVietnamPhone = (phone) => {
    if (!phone) return false;
    const cleaned = cleanPhone(phone);
    const vietPhoneRegex = /^(?:0|\+84)(?:3|5|7|8|9)[0-9]{8}$/;
    return vietPhoneRegex.test(cleaned);
  };

  return (
    <div className="mx-auto max-w-6xl p-4 sm:p-6">
      <div className="mb-4">
        <Title level={3} className="!mb-1">
          <UserOutlined className="mr-2 text-blue-600" />
          Thêm mới nhân viên
        </Title>
      </div>

      <Form
        form={form}
        layout="vertical"
        onFinish={handleFinish}
        disabled={loadingSubmit}
      >
        <Row gutter={[16, 16]} align="stretch">
          <Col xs={24} lg={12} className="flex">
            <Card
              title={
                <span className="font-semibold">
                  <FileImageOutlined className="mr-2" />
                  Ảnh thẻ
                </span>
              }
              className="shadow-sm w-full h-full min-h-[420px]"
              styles={{ body: { height: "88%" } }}
            >
              {cardPhotoPreview ? (
                <div className="flex flex-col items-center">
                  <img
                    src={cardPhotoPreview}
                    alt="card-photo-preview"
                    className="w-auto rounded-md object-cover ring-2 ring-blue-100"
                  />

                  <Upload
                    accept="image/*"
                    showUploadList={false}
                    beforeUpload={(file) => {
                      setCardPhotoFile(file);
                      setPreviewFromFile(file, setCardPhotoPreview);
                      return false;
                    }}
                    onChange={(info) => {
                      const f = info?.file?.originFileObj;
                      if (f) {
                        setCardPhotoFile(f);
                        setPreviewFromFile(f, setCardPhotoPreview);
                      }
                    }}
                  >
                    <button
                      type="button"
                      className="mt-3 px-3 py-1 rounded-md border text-sm"
                    >
                      Đổi ảnh thẻ
                    </button>
                  </Upload>
                </div>
              ) : (
                <Upload.Dragger
                  accept="image/*"
                  multiple={false}
                  showUploadList={false}
                  beforeUpload={(file) => {
                    setCardPhotoFile(file);
                    setPreviewFromFile(file, setCardPhotoPreview);
                    return false;
                  }}
                  onChange={(info) => {
                    const f = info?.file?.originFileObj;
                    if (f) {
                      setCardPhotoFile(f);
                      setPreviewFromFile(f, setCardPhotoPreview);
                    }
                  }}
                >
                  <p className="ant-upload-drag-icon">
                    <FileImageOutlined />
                  </p>
                  <p className="ant-upload-text">
                    Kéo & thả ảnh vào đây hoặc bấm để chọn
                  </p>
                  <p className="ant-upload-hint">Hỗ trợ JPG/PNG</p>

                  <div className="mt-4 h-40 w-full rounded-md bg-gray-50 flex items-center justify-center">
                    <IdcardOutlined className="text-2xl text-gray-400" />
                    <Text type="secondary" className="ml-2">
                      Chưa có ảnh thẻ
                    </Text>
                  </div>
                </Upload.Dragger>
              )}
            </Card>
          </Col>

          {/* Thông tin tài khoản */}
          <Col xs={24} lg={12} className="flex">
            <Card
              title={
                <span className="font-semibold">
                  <UserOutlined className="mr-2" />
                  Thông tin tài khoản
                </span>
              }
              className="shadow-sm w-full h-full min-h-[420px]"
              styles={{ body: { height: "100%" } }}
            >
              <Row gutter={[12, 0]}>
                <Col span={12}>
                  <Form.Item
                    label="Tên đăng nhập"
                    name={["user", "username"]}
                    rules={required("Vui lòng nhập tên đăng nhập")}
                  >
                    <Input
                      prefix={<UserOutlined />}
                      placeholder="vd: hoang.pham"
                    />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item
                    label="Email"
                    name={["user", "email"]}
                    rules={[
                      { required: true, message: "Vui lòng nhập email" },
                      { type: "email", message: "Email không hợp lệ" },
                    ]}
                  >
                    <Input
                      prefix={<MailOutlined />}
                      placeholder="vd: name@domain.com"
                    />
                  </Form.Item>
                </Col>
              </Row>

              <Form.Item label="Ảnh đại diện">
                {avatarPreview ? (
                  <div className="flex flex-col items-center">
                    <img
                      src={avatarPreview}
                      alt="avatar-preview"
                      className="h-24 w-24 rounded-full object-cover ring-2 ring-blue-100"
                    />

                    <Upload
                      accept="image/*"
                      showUploadList={false}
                      beforeUpload={(file) => {
                        setAvatarFile(file);
                        setPreviewFromFile(file, setAvatarPreview);
                        return false;
                      }}
                      onChange={(info) => {
                        const f = info?.file?.originFileObj;
                        if (f) {
                          setAvatarFile(f);
                          setPreviewFromFile(f, setAvatarPreview);
                        }
                      }}
                    >
                      <button
                        type="button"
                        className="mt-3 px-3 py-1 rounded-md border text-xs"
                      >
                        Đổi ảnh đại diện
                      </button>
                    </Upload>
                  </div>
                ) : (
                  <Upload.Dragger
                    accept="image/*"
                    multiple={false}
                    showUploadList={false}
                    beforeUpload={(file) => {
                      setAvatarFile(file);
                      setPreviewFromFile(file, setAvatarPreview);
                      return false;
                    }}
                    onChange={(info) => {
                      const f = info?.file?.originFileObj;
                      if (f) {
                        setAvatarFile(f);
                        setPreviewFromFile(f, setAvatarPreview);
                      }
                    }}
                  >
                    <p className="ant-upload-drag-icon">
                      <UserOutlined />
                    </p>
                    <p className="ant-upload-text">
                      Kéo & thả ảnh vào đây hoặc bấm để chọn
                    </p>
                    <p className="ant-upload-hint">Hỗ trợ JPG/PNG</p>
                  </Upload.Dragger>
                )}
              </Form.Item>
            </Card>
          </Col>
        </Row>

        {/* PHÂN CÔNG & QUYỀN */}
        <Row gutter={[16, 16]} className="mt-2">
          <Col xs={24}>
            <Card
              title={
                <span className="font-semibold">
                  <HomeOutlined className="mr-2" />
                  Phân công nhân sự
                </span>
              }
              className="shadow-sm"
            >
              <Form.Item
                label="Thuộc tòa nhà"
                name={["assignment", "buildingId"]}
                rules={required("Vui lòng chọn tòa nhà")}
              >
                <Select
                  placeholder="Chọn tòa nhà"
                  onChange={(value) => {
                    const selected = buildings.find((b) => b.id === value);
                    setSchemaName(
                      selected ? selected.schemaName || null : null
                    );

                    const staff = form.getFieldValue("staff") || {};
                    form.setFieldsValue({
                      assignment: { buildingId: value },
                      staff: { ...staff, roleId: undefined },
                    });
                  }}
                  options={buildings.map((b) => ({
                    value: b.id,
                    label: b.buildingName,
                  }))}
                />
              </Form.Item>

              <Form.Item
                label="Vai trò"
                name={["staff", "roleId"]}
                rules={required("Vui lòng chọn vai trò")}
              >
                <Select
                  placeholder="Chọn công việc chính"
                  options={roles.map((r) => ({
                    value: r.roleId,
                    label: r.roleName,
                  }))}
                />
              </Form.Item>

              <Form.Item
                label={
                  <span>
                    Gán quyền
                    <Tooltip title="Chọn một hoặc nhiều quyền truy cập cho nhân sự. Các quyền này quyết định màn hình/chức năng mà nhân sự được phép sử dụng trong hệ thống.">
                      <SafetyCertificateOutlined className="ml-1 text-blue-500" />
                    </Tooltip>
                  </span>
                }
                name={["assignment", "roles"]}
                rules={required("Vui lòng chọn quyền truy cập")}
              >
                <Select
                  mode="multiple"
                  placeholder="vd: accountant, building_admin…"
                  options={accessRoles.map((r) => ({
                    value: r.name,
                    label: r.description || r.name,
                  }))}
                />
              </Form.Item>
            </Card>
          </Col>
        </Row>

        {/* THÔNG TIN NHÂN SỰ */}
        <Row gutter={[16, 16]} className="mt-2">
          <Col xs={24}>
            <Card
              title={
                <span className="font-semibold">
                  <IdcardOutlined className="mr-2" />
                  Thông tin nhân sự
                </span>
              }
              className="shadow-sm"
            >
              {/* Họ tên */}
              <Row gutter={[12, 0]}>
                <Col span={12}>
                  <Form.Item
                    label="Họ"
                    name={["user", "firstName"]}
                    rules={required("Vui lòng nhập Họ")}
                  >
                    <Input placeholder="vd: Nguyễn" />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item
                    label="Tên"
                    name={["user", "lastName"]}
                    rules={required("Vui lòng nhập Tên")}
                  >
                    <Input placeholder="vd: An" />
                  </Form.Item>
                </Col>
              </Row>

              {/* SĐT + Ngày sinh */}
              <Row gutter={[12, 0]}>
                <Col span={12}>
                  <Form.Item
                    label="Số điện thoại"
                    name={["user", "phone"]}
                    rules={[
                      {
                        validator: (_, value) => {
                          if (!value) {
                            return Promise.reject(
                              new Error("Vui lòng nhập số điện thoại")
                            );
                          }
                          if (!isValidVietnamPhone(value)) {
                            return Promise.reject(
                              new Error("Số điện thoại không hợp lệ.")
                            );
                          }
                          return Promise.resolve();
                        },
                      },
                    ]}
                  >
                    <Input
                      prefix={<PhoneOutlined />}
                      placeholder="vd: 0901234567"
                    />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item label="Ngày sinh" name={["user", "dob"]}>
                    <DatePicker
                      className="w-full"
                      placeholder="Chọn ngày sinh"
                      format="DD/MM/YYYY"
                      suffixIcon={<CalendarOutlined />}
                      disabledDate={(current) =>
                        current && current > dayjs().endOf("day")
                      }
                    />
                  </Form.Item>
                </Col>
              </Row>

              {/* Quê quán + Địa chỉ hiện tại */}
              <Form.Item
                label="Quê quán"
                name={["user", "address"]}
                rules={required("Vui lòng nhập quê quán")}
              >
                <Input placeholder="Số nhà, đường, quận/huyện, ..." />
              </Form.Item>

              <Form.Item
                label="Địa chỉ hiện tại"
                name={["staff", "currentAddress"]}
                rules={required("Vui lòng nhập địa chỉ hiện tại")}
              >
                <Input placeholder="Số nhà, đường, quận/huyện, ..." />
              </Form.Item>

              <Divider className="my-4" />

              {/* Staff profile */}
              <Row gutter={[12, 0]}>
                <Col span={12}>
                  <Form.Item
                    label="Ngày vào làm"
                    name={["staff", "hireDate"]}
                    rules={required("Vui lòng chọn ngày vào làm")}
                  >
                    <DatePicker
                      className="w-full"
                      placeholder="Chọn ngày"
                      format="DD/MM/YYYY"
                      suffixIcon={<CalendarOutlined />}
                      disabledDate={(current) =>
                        current && current > dayjs().endOf("day")
                      }
                    />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item
                    label="Lương cơ bản"
                    name={["staff", "baseSalary"]}
                    rules={required("Vui lòng nhập lương cơ bản")}
                    tooltip="Mức lương cố định hàng tháng, chưa bao gồm phụ cấp, thưởng và các khoản hỗ trợ khác."
                  >
                    <InputNumber
                      className="w-full"
                      min={0}
                      step={100000}
                      formatter={(v) =>
                        (v || "")
                          .toString()
                          .replace(/\B(?=(\d{3})+(?!\d))/g, ".")
                      }
                      parser={(v) => v.replace(/\./g, "")}
                      prefix={<DollarOutlined />}
                    />
                  </Form.Item>
                </Col>
              </Row>

              <Row gutter={[12, 0]}>
                <Col span={12}>
                  <Form.Item
                    label="Người liên hệ khẩn cấp"
                    name={["staff", "emergencyContactName"]}
                  >
                    <Input placeholder="Họ và tên" />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item
                    label="SĐT liên hệ khẩn cấp"
                    name={["staff", "emergencyContactPhone"]}
                    rules={[
                      {
                        validator: (_, value) => {
                          if (!value || value.trim() === "") {
                            return Promise.resolve();
                          }

                          if (!isValidVietnamPhone(value)) {
                            return Promise.reject(
                              new Error("Số điện thoại không hợp lệ.")
                            );
                          }
                          return Promise.resolve();
                        },
                      },
                    ]}
                  >
                    <Input placeholder="vd: 0901234567" />
                  </Form.Item>
                </Col>
              </Row>

              <Form.Item
                label="Quan hệ"
                name={["staff", "emergencyContactRelation"]}
              >
                <Input placeholder="vd: Vợ/Chồng, Anh/Chị, ..." />
              </Form.Item>

              <Divider className="my-4" />

              <Row gutter={[12, 0]}>
                <Col span={12}>
                  <Form.Item
                    label="Số tài khoản"
                    name={["staff", "bankAccountNo"]}
                  >
                    <Input prefix={<BankOutlined />} placeholder="…" />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item label="Ngân hàng" name={["staff", "bankName"]}>
                    <Select
                      showSearch
                      placeholder="Chọn ngân hàng"
                      loading={loadingBanks}
                      filterOption={false}
                      onSearch={handleSearch}
                      allowClear
                    >
                      {filteredBanks.map((b) => (
                        <Option key={b.id} value={b.name}>
                          <Space>
                            <Avatar src={b.logo} size="small" />
                            <span>{b.shortName || b.name}</span>
                          </Space>
                        </Option>
                      ))}
                    </Select>
                  </Form.Item>
                </Col>
              </Row>

              <Row gutter={[12, 0]}>
                <Col span={12}>
                  <Form.Item label="Mã số thuế" name={["staff", "taxCode"]}>
                    <Input placeholder="…" />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item
                    label="Số BHXH"
                    name={["staff", "socialInsuranceNo"]}
                  >
                    <Input placeholder="…" />
                  </Form.Item>
                </Col>
              </Row>

              <Form.Item label="Ghi chú" name={["staff", "notes"]}>
                <Input.TextArea
                  rows={2}
                  placeholder="Ghi chú nội bộ, lưu ý, ..."
                />
              </Form.Item>
            </Card>
          </Col>
        </Row>

        {/* Actions */}
        <div className="mt-6 flex items-center justify-end gap-3">
          <Button icon={<ReloadOutlined />} onClick={() => form.resetFields()}>
            Nhập lại
          </Button>
          <Button
            type="primary"
            htmlType="submit"
            icon={<CheckCircleOutlined />}
            loading={loadingSubmit}
          >
            Lưu nhân viên
          </Button>
        </div>
      </Form>
    </div>
  );
}
