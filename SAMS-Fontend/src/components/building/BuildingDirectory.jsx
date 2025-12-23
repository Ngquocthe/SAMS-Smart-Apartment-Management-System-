import { useEffect, useState, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import {
  Card,
  Row,
  Col,
  Space,
  Button,
  Input,
  Tag,
  message,
  Tooltip,
  Modal,
  Empty,
  Pagination,
} from "antd";
import {
  SearchOutlined,
  PlusOutlined,
  EyeOutlined,
  EditOutlined,
  ExclamationCircleOutlined,
  SyncOutlined,
} from "@ant-design/icons";
import { coreApi } from "../../features/building/coreApi";
import ROUTER_PAGE from "../../constants/Routes";

const { confirm } = Modal;

const PLACEHOLDER =
  "data:image/svg+xml;utf8," +
  encodeURIComponent(
    `<svg xmlns='http://www.w3.org/2000/svg' width='800' height='450'><rect fill='%23f0f2f5' width='100%' height='100%'/><text x='50%' y='50%' dominant-baseline='middle' text-anchor='middle' fill='%23999' font-family='Arial' font-size='24'>No image</text></svg>`
  );

function formatNumber(n) {
  if (n == null) return "—";
  return Number(n).toLocaleString(undefined, {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  });
}

function statusLabelVietnam(status) {
  return status === 1 ? "Hoạt động" : "Không hoạt động";
}

const truncateText = (text, maxLength) => {
  if (!text) return "";
  return text.length > maxLength ? text.substring(0, maxLength) + "..." : text;
};

export default function BuildingList({
  onView,
  onEdit,
  onDelete, // ignored (kept for API compatibility)
  onCreate,
  selectable = false,
  onSelect,
}) {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [searchText, setSearchText] = useState("");
  const [paging, setPaging] = useState({ current: 1, pageSize: 9 });
  const [total, setTotal] = useState(0);
  const [togglingIds, setTogglingIds] = useState(new Set()); // track toggling buttons

  const navigate = useNavigate();

  const handleAddBuilding = () => {
    navigate(ROUTER_PAGE.ADMIN.BUILDING.CREATE_BUILDING);
  };

  const loadBuildings = useCallback(async () => {
    setLoading(true);
    try {
      // Lấy tất cả buildings (kể cả INACTIVE) cho trang admin list
      const res = await coreApi.getAllBuildingsIncludingInactive();

      const payload = Array.isArray(res) ? res : res?.items ?? res?.data ?? [];

      setItems(payload);

      const totalCount = payload.length;

      setTotal(totalCount);
    } catch (e) {
      console.error(e);
      message.error("Không tải được danh sách toà nhà");
    } finally {
      setLoading(false);
    }
  }, [searchText, paging.current, paging.pageSize]);

  useEffect(() => {
    loadBuildings();
  }, [loadBuildings]);

  const handleImageError = (e) => {
    e.currentTarget.src = PLACEHOLDER;
  };

  const onPageChange = (page, pageSize) => {
    setPaging({ current: page, pageSize });
  };

  const handleToggleStatus = async (record) => {
    const id = record.id;
    const isActive = record.status === 1;
    const newStatus = isActive ? 0 : 1;

    const actionLabel = isActive ? "Vô hiệu hóa" : "Kích hoạt";
    const confirmMessage = isActive
      ? `Bạn có chắc muốn vô hiệu hóa tòa nhà "${record.buildingName}"?`
      : `Bạn có muốn kích hoạt lại tòa nhà "${record.buildingName}"?`;

    confirm({
      title: confirmMessage,
      icon: <ExclamationCircleOutlined />,
      okText: actionLabel,
      cancelText: "Hủy",

      onOk: async () => {
        setTogglingIds((s) => new Set(s).add(id)); // đánh dấu đang xử lý

        try {
          // gọi API cập nhật trạng thái
          if (typeof coreApi.updateBuildingStatus === "function") {
            await coreApi.updateBuildingStatus(id, { status: newStatus });
          } else if (typeof coreApi.patchBuilding === "function") {
            await coreApi.patchBuilding(id, { status: newStatus });
          } else {
            throw new Error(
              "API update status chưa được khai báo trong coreApi"
            );
          }

          // Reload danh sách để đảm bảo đồng bộ
          await loadBuildings();

          message.success(
            newStatus === 1 ? "Đã kích hoạt tòa nhà" : "Đã vô hiệu hóa tòa nhà"
          );
        } catch (err) {
          console.error(err);
          message.error("Cập nhật trạng thái thất bại");
        } finally {
          setTogglingIds((s) => {
            const next = new Set(s);
            next.delete(id);
            return next;
          });
        }
      },
    });
  };

  return (
    <div>
      <Space style={{ marginBottom: 16, width: "100%" }} align="center" wrap>
        <Input
          prefix={<SearchOutlined />}
          placeholder="Tìm theo tên / mã / schema"
          allowClear
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
          onPressEnter={() => {
            setPaging((p) => ({ ...p, current: 1 }));
            loadBuildings();
          }}
          style={{ width: 320 }}
        />
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => handleAddBuilding()}
        >
          Tạo toà nhà
        </Button>

        <Button
          onClick={() => {
            setPaging((p) => ({ ...p, current: 1 }));
            loadBuildings();
          }}
        >
          Tải lại
        </Button>
      </Space>

      {items?.length === 0 ? (
        <Card loading={loading}>
          <Empty description="Không có toà nhà" />
        </Card>
      ) : (
        <div>
          <Row gutter={[16, 16]}>
            {items.map((b) => {
              const img = b.avatarUrl || b.imageUrl || PLACEHOLDER;
              const address =
                b.address ||
                (b.latitude != null && b.longitude != null
                  ? `${Number(b.latitude).toFixed(6)}, ${Number(
                    b.longitude
                  ).toFixed(6)}`
                  : b.schemaName || "—");
              const totalAreaTag =
                b.totalAreaM2 != null ? (
                  <Tag>{formatNumber(b.totalAreaM2)} m²</Tag>
                ) : null;

              const statusTag = (
                <Tag color={b.status === 1 ? "green" : "default"}>
                  {statusLabelVietnam(b.status)}
                </Tag>
              );

              const isToggling = togglingIds.has(b.id);

              return (
                <Col
                  key={b.id || b.schemaName || b.code}
                  xs={24}
                  sm={12}
                  md={8}
                  style={{ display: "flex" }}
                >
                  <Card
                    hoverable
                    style={{
                      width: "100%",
                      display: "flex",
                      flexDirection: "column",
                      minHeight: 320,
                    }}
                    bodyStyle={{
                      flex: 1,
                      display: "flex",
                      flexDirection: "column",
                      justifyContent: "space-between",
                      padding: 16,
                    }}
                    cover={
                      <div
                        style={{
                          height: 160,
                          display: "flex",
                          alignItems: "center",
                          justifyContent: "center",
                          overflow: "hidden",
                          background: "#f5f5f5",
                        }}
                      >
                        <img
                          src={img}
                          alt={b.buildingName}
                          style={{
                            width: "100%",
                            height: "100%",
                            objectFit: "cover",
                          }}
                          onError={handleImageError}
                        />
                      </div>
                    }
                    actions={[
                      <Tooltip key="view" title="Xem">
                        <Button
                          type="text"
                          icon={<EyeOutlined />}
                          onClick={() => onView?.(b)}
                        />
                      </Tooltip>,
                      <Tooltip key="edit" title="Sửa">
                        <Button
                          type="text"
                          icon={<EditOutlined />}
                          onClick={() => onEdit?.(b)}
                        />
                      </Tooltip>,
                      <Tooltip
                        key="toggle"
                        title={b.status === 1 ? "Vô hiệu hoá" : "Kích hoạt"}
                      >
                        <Button
                          type="text"
                          icon={<SyncOutlined spin={isToggling} />}
                          onClick={() => handleToggleStatus(b)}
                          disabled={isToggling}
                        />
                      </Tooltip>,
                    ]}
                  >
                    <div
                      style={{
                        display: "flex",
                        flexDirection: "column",
                        height: "100%",
                      }}
                    >
                      <div style={{ marginBottom: 8 }}>
                        <div
                          style={{
                            display: "flex",
                            justifyContent: "space-between",
                            alignItems: "center",
                            gap: 8,
                          }}
                        >
                          <div style={{ fontWeight: 600, fontSize: 16 }}>
                            {b.buildingName}
                          </div>
                          <div
                            style={{
                              display: "flex",
                              gap: 6,
                              alignItems: "center",
                            }}
                          >
                            {totalAreaTag}
                            {statusTag}
                            {b.code ? <Tag>{b.code}</Tag> : null}
                          </div>
                        </div>
                      </div>

                      <div
                        style={{
                          flex: 1,
                          display: "flex",
                          flexDirection: "column",
                          justifyContent: "space-between",
                        }}
                      >
                        <div>
                          <div
                            style={{ color: "var(--ant-gray-6)", fontSize: 13 }}
                          >
                            {address}
                          </div>

                          {b.description ? (
                            <div
                              style={{
                                marginTop: 6,
                                color: "var(--ant-gray-6)",
                              }}
                            >
                              {truncateText(b.description, 50)}
                            </div>
                          ) : null}
                        </div>

                        <div style={{ marginTop: 8 }}>
                          {selectable && (
                            <Button
                              size="small"
                              type="default"
                              onClick={() => onSelect?.(b)}
                            >
                              Chọn
                            </Button>
                          )}
                        </div>
                      </div>
                    </div>
                  </Card>
                </Col>
              );
            })}
          </Row>

          {typeof total === "number" && total > paging.pageSize && (
            <div style={{ marginTop: 16, textAlign: "right" }}>
              <Pagination
                current={paging.current}
                pageSize={paging.pageSize}
                total={total}
                showSizeChanger
                onChange={onPageChange}
                onShowSizeChange={onPageChange}
                showTotal={(t) => `${t} toà nhà`}
              />
            </div>
          )}
        </div>
      )}
    </div>
  );
}
