import { useState, useEffect } from "react";
import {
  Card,
  Row,
  Col,
  Input,
  Tag,
  Typography,
  Space,
  Empty,
  Spin,
  Button,
  Modal,
  Divider,
  Badge,
  Tabs,
  Image,
  Timeline,
} from "antd";
import {
  BellOutlined,
  SearchOutlined,
  ClockCircleOutlined,
  CalendarOutlined,
  PushpinOutlined,
  NotificationOutlined,
  FireOutlined,
  EyeOutlined,
  FieldTimeOutlined,
} from "@ant-design/icons";
import { announcementApi } from "../../features/building-management/announcementApi";
import dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";
import "dayjs/locale/vi";

dayjs.extend(relativeTime);
dayjs.locale("vi");

const { Title, Text, Paragraph } = Typography;
const { Search } = Input;

export default function News() {
  const [announcements, setAnnouncements] = useState([]);
  const [events, setEvents] = useState([]);
  const [filteredAnnouncements, setFilteredAnnouncements] = useState([]);
  const [filteredEvents, setFilteredEvents] = useState([]);
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedItem, setSelectedItem] = useState(null);
  const [detailModalVisible, setDetailModalVisible] = useState(false);
  const [activeTab, setActiveTab] = useState("announcements");
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(10);

  useEffect(() => {
    fetchAnnouncements();
  }, []);

  useEffect(() => {
    const filterItems = () => {
      const filterFunction = (items) => {
        if (!searchTerm.trim()) return items;
        return items.filter(
          (item) =>
            item.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
            item.content.toLowerCase().includes(searchTerm.toLowerCase())
        );
      };

      setFilteredAnnouncements(filterFunction(announcements));
      setFilteredEvents(filterFunction(events));
    };

    filterItems();
    setCurrentPage(1); // Reset về trang đầu khi thay đổi tìm kiếm
  }, [searchTerm, announcements, events]);

  // Reset trang khi thay đổi tab
  useEffect(() => {
    setCurrentPage(1);
  }, [activeTab]);

  const fetchAnnouncements = async () => {
    setLoading(true);
    try {
      const response = await announcementApi.getActive();

      if (response) {
        const allItems = response.announcements || response || [];

        // Lọc theo phạm vi hiển thị cho cư dân
        const residentItems = allItems.filter(
          (item) =>
            item.status === "ACTIVE" &&
            (item.visibilityScope === "RESIDENTS" ||
              item.visibilityScope === "ALL")
        );

        // Phân loại theo type
        const announcementItems = residentItems
          .filter((item) => item.type === "ANNOUNCEMENT")
          .sort((a, b) => {
            // Ghim lên trước, sau đó sắp xếp theo ngày tạo
            if (a.isPinned && !b.isPinned) return -1;
            if (!a.isPinned && b.isPinned) return 1;
            return new Date(b.createdAt) - new Date(a.createdAt);
          });

        const eventItems = residentItems
          .filter((item) => item.type === "EVENT")
          .sort((a, b) => {
            // Ghim lên trước, sau đó sắp xếp theo ngày tạo
            if (a.isPinned && !b.isPinned) return -1;
            if (!a.isPinned && b.isPinned) return 1;
            return new Date(b.createdAt) - new Date(a.createdAt);
          });

        setAnnouncements(announcementItems);
        setEvents(eventItems);
      }
    } catch (error) {
      console.error("Error fetching announcements:", error);
      setAnnouncements([]);
      setEvents([]);
    } finally {
      setLoading(false);
    }
  };

  const handleViewDetail = (item) => {
    setSelectedItem(item);
    setDetailModalVisible(true);
  };

  const getTypeIcon = (type) => {
    return type === "EVENT" ? <CalendarOutlined /> : <BellOutlined />;
  };

  const getTypeTag = (type) => {
    const configs = {
      ANNOUNCEMENT: {
        color: "blue",
        text: "Thông báo",
        icon: <BellOutlined />,
      },
      EVENT: { color: "green", text: "Sự kiện", icon: <CalendarOutlined /> },
    };
    const config = configs[type] || configs.ANNOUNCEMENT;
    return (
      <Tag color={config.color} icon={config.icon}>
        {config.text}
      </Tag>
    );
  };

  const renderItemsList = (items, type) => {
    const filteredItems =
      type === "announcements" ? filteredAnnouncements : filteredEvents;

    if (loading) {
      return (
        <div style={{ textAlign: "center", padding: "100px 0" }}>
          <Spin
            size="large"
            tip={`Đang tải ${
              type === "announcements" ? "thông báo" : "sự kiện"
            }...`}
          />
        </div>
      );
    }

    if (filteredItems.length === 0) {
      return (
        <div>
          <Empty
            description={
              searchTerm
                ? `Không tìm thấy ${
                    type === "announcements" ? "thông báo" : "sự kiện"
                  } phù hợp`
                : `Chưa có ${
                    type === "announcements" ? "thông báo" : "sự kiện"
                  } nào`
            }
            image={Empty.PRESENTED_IMAGE_SIMPLE}
          />
        </div>
      );
    }

    // Tách items đã ghim và chưa ghim
    const pinnedItems = filteredItems.filter((item) => item.isPinned);
    const regularItems = filteredItems.filter((item) => !item.isPinned);

    // Tính toán phân trang cho regular items
    const startIndex = (currentPage - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const paginatedRegularItems = regularItems.slice(startIndex, endIndex);

    return (
      <div style={{ width: "100%" }}>
        {/* Items đã ghim */}
        {pinnedItems.length > 0 && (
          <div style={{ marginBottom: 24 }}>
            <Divider orientation="left">
              <Space>
                <PushpinOutlined style={{ color: "#ff4d4f" }} />
                <span style={{ color: "#ff4d4f", fontWeight: "bold" }}>
                  Đã ghim
                </span>
              </Space>
            </Divider>
            <div
              style={{ display: "flex", flexDirection: "column", gap: "12px" }}
            >
              {pinnedItems.map((item) => renderItemRow(item, true))}
            </div>
          </div>
        )}

        {/* Items thường */}
        {regularItems.length > 0 && (
          <div>
            {pinnedItems.length > 0 && (
              <Divider orientation="left">
                <span>Khác</span>
              </Divider>
            )}
            <div
              style={{
                display: "flex",
                flexDirection: "column",
                gap: "12px",
                marginBottom: "24px",
              }}
            >
              {paginatedRegularItems.map((item) => renderItemRow(item, false))}
            </div>

            {/* Phân trang */}
            {regularItems.length > pageSize && (
              <div style={{ textAlign: "center", marginTop: "24px" }}>
                <div
                  style={{
                    display: "inline-block",
                    padding: "16px",
                    backgroundColor: "#fafafa",
                    borderRadius: "8px",
                    border: "1px solid #d9d9d9",
                  }}
                >
                  <Space>
                    <Text type="secondary" style={{ fontSize: "14px" }}>
                      Trang:
                    </Text>
                    <Button.Group>
                      <Button
                        size="small"
                        disabled={currentPage === 1}
                        onClick={() => setCurrentPage((prev) => prev - 1)}
                      >
                        Trước
                      </Button>
                      <Button size="small" type="primary">
                        {currentPage}
                      </Button>
                      <Button
                        size="small"
                        disabled={
                          currentPage >=
                          Math.ceil(regularItems.length / pageSize)
                        }
                        onClick={() => setCurrentPage((prev) => prev + 1)}
                      >
                        Sau
                      </Button>
                    </Button.Group>
                    <Text type="secondary" style={{ fontSize: "12px" }}>
                      ({startIndex + 1}-
                      {Math.min(endIndex, regularItems.length)} /{" "}
                      {regularItems.length})
                    </Text>
                  </Space>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    );
  };

  const renderItemRow = (item, isPinned) => {
    return (
      <Card
        key={item.announcementId}
        hoverable
        style={{
          marginBottom: 12,
          borderLeft: isPinned ? "3px solid #1890ff" : "none",
        }}
        bodyStyle={{ padding: "16px 20px" }}
        onClick={() => handleViewDetail(item)}
      >
        <Row align="middle" gutter={16}>
          <Col flex="auto">
            <Space size={8}>
              {isPinned && (
                <PushpinOutlined style={{ color: "#1890ff" }} />
              )}
              {getTypeTag(item.type)}
              <Text strong style={{ fontSize: 15 }}>
                {item.title}
              </Text>
            </Space>
          </Col>
          <Col>
            <Text type="secondary" style={{ fontSize: 13 }}>
              {dayjs(item.createdAt).format("DD/MM/YYYY")}
            </Text>
          </Col>
        </Row>
      </Card>
    );
  };

  return (
    <>
      <div style={{ padding: "24px", maxWidth: "1200px", margin: "0 auto" }}>
        {/* Header */}
        <div style={{ marginBottom: 24 }}>
          <Title level={3} style={{ margin: 0, color: "#1890ff" }}>
            <BellOutlined style={{ marginRight: 8 }} />
            Tin tức và Thông báo
          </Title>
        </div>
        {/* Search */}
        <div style={{ marginBottom: 24 }}>
          <Search
            placeholder="Tìm kiếm tin tức, thông báo..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            style={{ maxWidth: 400 }}
            enterButton={<SearchOutlined />}
            allowClear
          />
        </div>

        {/* Content */}
        <div
          style={{
            backgroundColor: "#fff",
            borderRadius: 8,
            padding: "24px",
            border: "1px solid #d9d9d9",
          }}
        >
          <Tabs
            activeKey={activeTab}
            onChange={(key) => {
              setActiveTab(key);
              setCurrentPage(1);
            }}
            size="large"
            items={[
              {
                key: "announcements",
                label: (
                  <Space>
                    <NotificationOutlined />
                    <span>Thông báo</span>
                    <Badge
                      count={filteredAnnouncements.length}
                      showZero
                      style={{ backgroundColor: "#52c41a" }}
                    />
                  </Space>
                ),
                children: renderItemsList(announcements, "announcements"),
              },
              {
                key: "events",
                label: (
                  <Space>
                    <CalendarOutlined />
                    <span>Sự kiện</span>
                    <Badge
                      count={filteredEvents.length}
                      showZero
                      style={{ backgroundColor: "#1890ff" }}
                    />
                  </Space>
                ),
                children: renderItemsList(events, "events"),
              },
            ]}
          />
        </div>
      </div>

      {/* Detail Modal */}
      <Modal
        open={detailModalVisible}
        onCancel={() => {
          setDetailModalVisible(false);
          setSelectedItem(null);
        }}
        footer={null}
        width={800}
        title={
          <Space>
            {selectedItem && getTypeIcon(selectedItem.type)}
            <span>Chi tiết {selectedItem?.type === "EVENT" ? "sự kiện" : "thông báo"}</span>
          </Space>
        }
      >
        {selectedItem && (
          <div>
            <Space style={{ marginBottom: 16 }}>
              {selectedItem.isPinned && (
                <Tag color="blue">
                  <PushpinOutlined /> Đã ghim
                </Tag>
              )}
              {getTypeTag(selectedItem.type)}
            </Space>

            <Title level={3} style={{ marginBottom: 16 }}>
              {selectedItem.title}
            </Title>

            <Divider />

            <Space style={{ marginBottom: 16 }}>
              <ClockCircleOutlined />
              <Text type="secondary">
                {dayjs(selectedItem.createdAt).format("DD/MM/YYYY HH:mm")}
              </Text>
            </Space>

            <Divider />

            <Paragraph
              style={{
                whiteSpace: "pre-wrap",
                fontSize: 15,
                lineHeight: 1.8,
              }}
            >
              {selectedItem.content}
            </Paragraph>

            <Divider />

            <div style={{ textAlign: "right" }}>
              <Button
                type="primary"
                onClick={() => {
                  setDetailModalVisible(false);
                  setSelectedItem(null);
                }}
              >
                Đóng
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </>
  );
}
