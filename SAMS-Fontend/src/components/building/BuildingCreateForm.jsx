import { useEffect, useState } from "react";
import {
  Form,
  Input,
  InputNumber,
  DatePicker,
  Card,
  Row,
  Col,
  Upload,
  Button,
  Typography,
  message,
} from "antd";
import {
  HomeOutlined,
  FileImageOutlined,
  ReloadOutlined,
  CheckCircleOutlined,
  CalendarOutlined,
  InfoCircleOutlined,
} from "@ant-design/icons";
import dayjs from "dayjs";
import { coreApi } from "../../features/building/coreApi";

import { MapContainer, TileLayer, Marker, useMapEvents } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

/* Fix default marker icon issue in many bundlers */
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: require("leaflet/dist/images/marker-icon-2x.png"),
  iconUrl: require("leaflet/dist/images/marker-icon.png"),
  shadowUrl: require("leaflet/dist/images/marker-shadow.png"),
});

const { Title, Text } = Typography;
const { TextArea } = Input;
const { Dragger } = Upload;

const DEFAULT_CENTER = { lat: 21.0278, lng: 105.8342 };
const DEFAULT_ZOOM = 13;

function LocationSelector({ setMarkerPos }) {
  useMapEvents({
    click(e) {
      const val = {
        lat: +e.latlng.lat.toFixed(7),
        lng: +e.latlng.lng.toFixed(7),
      };
      setMarkerPos(val);
    },
  });

  return null;
}

export default function BuildingCreateForm({ onSubmit }) {
  const [form] = Form.useForm();
  const [loadingSubmit, setLoadingSubmit] = useState(false);

  const [avatarPreview, setAvatarPreview] = useState("");

  const [avatarFile, setAvatarFile] = useState(null);

  const [markerPos, setMarkerPos] = useState(null);
  const [mapCenter, setMapCenter] = useState(DEFAULT_CENTER);
  const [mapZoom, setMapZoom] = useState(DEFAULT_ZOOM);

  useEffect(() => {
    return () => {
      if (avatarPreview && avatarPreview.startsWith("blob:")) {
        URL.revokeObjectURL(avatarPreview);
      }
    };
  }, [avatarPreview]);

  const setPreviewFromFile = (file, setter) => {
    if (!file) return;
    const url = URL.createObjectURL(file);
    setter((prev) => {
      if (prev && prev.startsWith("blob:")) URL.revokeObjectURL(prev);
      return url;
    });
  };

  const handleMarkerDragEnd = (e) => {
    const lat = e.target.getLatLng().lat;
    const lng = e.target.getLatLng().lng;
    const val = { lat: +lat.toFixed(7), lng: +lng.toFixed(7) };
    setMarkerPos(val);
    form.setFieldsValue({ latitude: val.lat, longitude: val.lng });
  };

  const handleLatLngInputChange = () => {
    const lat = form.getFieldValue("latitude");
    const lng = form.getFieldValue("longitude");
    if (lat != null && lng != null && !isNaN(lat) && !isNaN(lng)) {
      const val = {
        lat: +Number(lat).toFixed(7),
        lng: +Number(lng).toFixed(7),
      };
      setMarkerPos(val);
      setMapCenter(val);
      setMapZoom(15);
    }
  };

  const handleFinish = async (values) => {
    setLoadingSubmit(true);
    try {
      const formData = new FormData();

      formData.append("code", values.code?.trim() || "");
      formData.append("buildingName", values.buildingName?.trim() || "");

      formData.append("description", values.description?.trim() || "");
      formData.append(
        "totalAreaM2",
        values.totalAreaM2 != null ? String(values.totalAreaM2) : ""
      );

      formData.append(
        "openingDate",
        values.openingDate ? dayjs(values.openingDate).format("YYYY-MM-DD") : ""
      );

      formData.append(
        "latitude",
        values.latitude != null ? String(values.latitude) : ""
      );
      formData.append(
        "longitude",
        values.longitude != null ? String(values.longitude) : ""
      );

      if (avatarFile) formData.append("avatar", avatarFile);

      if (typeof onSubmit === "function") {
        await onSubmit(formData);
      } else {
        await coreApi.createBuilding(formData);
        message.success("Tạo tòa nhà thành công");
      }

      form.resetFields();
      if (avatarPreview && avatarPreview.startsWith("blob:"))
        URL.revokeObjectURL(avatarPreview);
      setAvatarPreview("");
      setAvatarFile(null);
      setMarkerPos(null);
      setMapCenter(DEFAULT_CENTER);
      setMapZoom(DEFAULT_ZOOM);
    } catch (e) {
      console.error(e);
      message.error(e?.errorMessage || "Tạo tòa nhà thất bại");
    } finally {
      setLoadingSubmit(false);
    }
  };

  return (
    <div className="mx-auto p-4 sm:p-6">
      <div className="mb-4">
        <Title level={3} className="!mb-1">
          <HomeOutlined className="mr-2 text-blue-600" />
          Thêm mới tòa nhà
        </Title>
        <Text type="secondary">
          Nhập thông tin tòa nhà và chọn vị trí trên bản đồ
        </Text>
      </div>

      <Form
        form={form}
        layout="vertical"
        onFinish={handleFinish}
        disabled={loadingSubmit}
      >
        <Row gutter={[16, 16]} className="items-stretch">
          <Col xs={24} lg={16}>
            <Card
              title={
                <span className="font-semibold">
                  <FileImageOutlined className="mr-2" />
                  Ảnh tòa nhà
                </span>
              }
              className="shadow-sm w-full h-full flex flex-col"
              styles={{
                body: {
                  padding: 16,
                  display: "flex",
                  flexDirection: "column",
                  height: "100%",
                },
              }}
            >
              {avatarPreview ? (
                <div className="flex flex-col items-center">
                  <img
                    src={avatarPreview}
                    alt="avatar-preview"
                    className="h-64 w-full rounded-md object-cover"
                  />
                  <div style={{ marginTop: 12 }}>
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
                        className="px-3 py-1 rounded-md border text-sm"
                      >
                        Đổi ảnh
                      </button>
                    </Upload>
                  </div>
                </div>
              ) : (
                <Dragger
                  accept="image/*"
                  className="h-full"
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
                    <FileImageOutlined />
                  </p>
                  <p className="ant-upload-text">
                    Kéo & thả ảnh đại diện hoặc bấm để chọn
                  </p>
                  <p className="ant-upload-hint">Hỗ trợ JPG/PNG</p>
                </Dragger>
              )}
            </Card>
          </Col>

          <Col xs={24} lg={8}>
            <Card
              title={
                <span className="font-semibold">
                  <InfoCircleOutlined className="mr-2" />
                  Thông tin cơ bản
                </span>
              }
              className="shadow-sm w-full h-full"
            >
              <Row gutter={[24, 0]}>
                <Col span={24}>
                  <Form.Item
                    label="Mã tòa nhà"
                    name="code"
                    rules={[
                      { required: true, message: "Vui lòng nhập mã tòa nhà" },
                      { max: 30, message: "Tối đa 30 ký tự" },
                      {
                        pattern: /^[A-Za-z0-9-]+$/,
                        message:
                          "Chỉ cho phép chữ không dấu, số và dấu gạch ngang, không chứa khoảng trắng",
                      },
                    ]}
                  >
                    <Input placeholder="vd: B-001" />
                  </Form.Item>
                </Col>
              </Row>

              <Form.Item
                label="Tên tòa nhà"
                name="buildingName"
                rules={[
                  { required: true, message: "Vui lòng nhập tên tòa nhà" },
                ]}
              >
                <Input placeholder="vd: Sunshine Tower" />
              </Form.Item>

              <Form.Item label="Tổng diện tích (m²)" name="totalAreaM2">
                <InputNumber min={0} step={0.01} className="w-full" />
              </Form.Item>
            </Card>
          </Col>
        </Row>

        {/* Map picker + description */}
        <Row gutter={[16, 16]} className="mt-2">
          <Col xs={24} lg={16}>
            <Card
              title={
                <span className="font-semibold">
                  <HomeOutlined className="mr-2" />
                  Chọn vị trí trên bản đồ
                </span>
              }
              className="shadow-sm"
            >
              <div style={{ height: 360 }}>
                <MapContainer
                  center={mapCenter}
                  zoom={mapZoom}
                  style={{ height: "100%", width: "100%" }}
                >
                  <TileLayer
                    attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
                    url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                  />
                  <LocationSelector
                    markerPos={markerPos}
                    setMarkerPos={(val) => {
                      setMarkerPos(val);
                      form.setFieldsValue({
                        latitude: val.lat,
                        longitude: val.lng,
                      });
                    }}
                  />
                  {markerPos && (
                    <Marker
                      position={markerPos}
                      draggable={true}
                      eventHandlers={{ dragend: handleMarkerDragEnd }}
                    />
                  )}
                </MapContainer>
              </div>
              <div style={{ marginTop: 12 }}>
                <Text type="secondary">
                  Click bản đồ để đặt vị trí, kéo marker để điều chỉnh.
                </Text>
              </div>
            </Card>
          </Col>

          <Col xs={24} lg={8}>
            <Card
              title={
                <span className="font-semibold">
                  <InfoCircleOutlined className="mr-2" />
                  Mô tả & Vị trí
                </span>
              }
              className="shadow-sm"
            >
              <Form.Item label="Mô tả ngắn" name="description">
                <TextArea rows={3} placeholder="Mô tả tòa nhà, tiện ích, ..." />
              </Form.Item>
              <Form.Item label="Kinh độ" name="longitude">
                <InputNumber
                  className="w-full"
                  step={0.0000001}
                  onBlur={handleLatLngInputChange}
                />
              </Form.Item>

              <Form.Item label="Vĩ độ" name="latitude">
                <InputNumber
                  className="w-full"
                  step={0.0000001}
                  onBlur={handleLatLngInputChange}
                />
              </Form.Item>

              <Form.Item label="Ngày khai trương" name="openingDate">
                <DatePicker
                  className="w-full"
                  format="DD/MM/YYYY"
                  suffixIcon={<CalendarOutlined />}
                />
              </Form.Item>
            </Card>
          </Col>
        </Row>

        {/* Actions */}
        <div className="mt-6 flex items-center justify-end gap-3">
          <Button
            icon={<ReloadOutlined />}
            onClick={() => {
              form.resetFields();
              setMarkerPos(null);
              setMapCenter(DEFAULT_CENTER);
              setMapZoom(DEFAULT_ZOOM);
            }}
          >
            Nhập lại
          </Button>
          <Button
            type="primary"
            htmlType="submit"
            icon={<CheckCircleOutlined />}
            loading={loadingSubmit}
          >
            Lưu tòa nhà
          </Button>
        </div>
      </Form>
    </div>
  );
}
