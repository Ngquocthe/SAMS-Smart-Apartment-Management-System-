import { useEffect, useMemo, useState, useCallback } from "react";
import {
  Table,
  Card,
  Space,
  Button,
  Tag,
  Input,
  Select,
  Flex,
  message,
  Tooltip,
  Typography,
  Empty,
} from "antd";
import {
  EyeOutlined,
  ReloadOutlined,
  SearchOutlined,
  HomeOutlined,
  UserOutlined,
  PhoneOutlined,
  MailOutlined,
} from "@ant-design/icons";
import { coreApi } from "../../features/building/coreApi";
import { residentsApi } from "../../features/residents/residentsApi";
import dayjs from "dayjs";

const { Text } = Typography;

export default function ResidentDirectory({
  defaultBuildingId,
  onViewDetail,
}) {
  const [buildings, setBuildings] = useState([]);
  const [buildingId, setBuildingId] = useState(defaultBuildingId || undefined);
  const [searchText, setSearchText] = useState("");
  const [apartmentId, setApartmentId] = useState(null);
  const [sortBy, setSortBy] = useState(null);
  const [sortDir, setSortDir] = useState("asc");

  const [data, setData] = useState([]);
  const [total, setTotal] = useState(0);
  const [paging, setPaging] = useState({ current: 1, pageSize: 20 });
  const [loading, setLoading] = useState(false);

  const schemaName = useMemo(() => {
    if (!buildingId || !buildings?.length) return undefined;
    const b = buildings.find((x) => x.id === buildingId);
    return b?.schemaName;
  }, [buildingId, buildings]);

  // Load buildings
  useEffect(() => {
    (async () => {
      try {
        const items = await coreApi.getBuildings();
        setBuildings(items);
        if (!defaultBuildingId && items?.length) {
          const firstId = items[0]?.id;
          setBuildingId(firstId);
        }
      } catch (e) {
        console.error(e);
        message.error("Không tải được danh sách toà nhà");
      }
    })();
  }, [defaultBuildingId]);

  const loadData = useCallback(async () => {
    if (!schemaName) {
      setData([]);
      setTotal(0);
      return;
    }
    setLoading(true);

    const { current, pageSize } = paging;

    try {
      const {
        items,
        totalCount,
        pageNumber,
        pageSize: serverPageSize,
      } = await residentsApi.getPaged(schemaName, {
        pageNumber: current,
        pageSize,
        apartmentId: apartmentId || undefined,
        q: searchText || undefined,
        sortBy: sortBy || undefined,
        sortDir: sortDir || undefined,
      });

      setData(items || []);
      setTotal(totalCount ?? 0);

      if (pageNumber && pageNumber !== current) {
        setPaging((p) => ({ ...p, current: pageNumber }));
      }
      if (serverPageSize && serverPageSize !== pageSize) {
        setPaging((p) => ({ ...p, pageSize: serverPageSize }));
      }
    } catch (e) {
      console.error(e);
      message.error("Không tải được danh sách cư dân");
      setData([]);
      setTotal(0);
    } finally {
      setLoading(false);
    }
  }, [schemaName, searchText, apartmentId, sortBy, sortDir, paging]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleView = (record) => {
    if (onViewDetail) {
      onViewDetail(record);
    }
  };

  const resetFilters = () => {
    setSearchText("");
    setApartmentId(null);
    setSortBy(null);
    setSortDir("asc");
    setPaging((p) => ({ ...p, current: 1 }));
  };

  // Columns
  const columns = [
    {
      title: "Họ tên",
      dataIndex: "fullName",
      key: "fullName",
      sorter: true,
      render: (text, record) => (
        <Space>
          <UserOutlined style={{ color: "#1890ff" }} />
          <Text strong={record.apartments?.some((a) => a.isPrimary)}>
            {text || "—"}
          </Text>
        </Space>
      ),
    },
    {
      title: "Số điện thoại",
      dataIndex: "phone",
      key: "phone",
      render: (text) => (
        text ? (
          <Space>
            <PhoneOutlined />
            <Text>{text}</Text>
          </Space>
        ) : "—"
      ),
    },
    {
      title: "Email",
      dataIndex: "email",
      key: "email",
      responsive: ["md"],
      render: (text) => (
        text ? (
          <Space>
            <MailOutlined />
            <Text>{text}</Text>
          </Space>
        ) : "—"
      ),
    },
    {
      title: "Căn hộ",
      key: "apartments",
      responsive: ["lg"],
      render: (_, record) => {
        const apartments = record.apartments || [];
        if (apartments.length === 0) return "—";
        
        return (
          <Space wrap>
            {apartments.map((apt, idx) => (
              <Tag
                key={idx}
                color={apt.isPrimary ? "blue" : "default"}
                icon={apt.isPrimary ? <HomeOutlined /> : null}
              >
                {apt.apartmentNumber || "—"}
                {apt.isPrimary && " (Chủ hộ)"}
              </Tag>
            ))}
          </Space>
        );
      },
    },
    {
      title: "Giới tính",
      dataIndex: "gender",
      key: "gender",
      responsive: ["lg"],
      render: (text) => {
        if (!text) return "—";
        const genderMap = {
          MALE: "Nam",
          FEMALE: "Nữ",
          OTHER: "Khác",
        };
        return genderMap[text] || text;
      },
    },
    {
      title: "Trạng thái",
      key: "status",
      render: (_, record) => {
        const status = record.status || "UNKNOWN";
        const statusMap = {
          ACTIVE: { color: "green", text: "Hoạt động" },
          INACTIVE: { color: "default", text: "Không hoạt động" },
          PENDING: { color: "orange", text: "Chờ duyệt" },
        };
        const statusInfo = statusMap[status] || { color: "default", text: status };
        return <Tag color={statusInfo.color}>{statusInfo.text}</Tag>;
      },
    },
    {
      title: "Ngày tạo",
      dataIndex: "createdAt",
      key: "createdAt",
      responsive: ["xl"],
      sorter: true,
      render: (text) => (text ? dayjs(text).format("DD/MM/YYYY") : "—"),
    },
    {
      title: "Thao tác",
      key: "actions",
      width: 120,
      render: (_, record) => (
        <Button
          type="link"
          icon={<EyeOutlined />}
          onClick={() => handleView(record)}
          disabled={!schemaName}
        >
          Xem chi tiết
        </Button>
      ),
    },
  ];

  // Building options
  const buildingOptions = useMemo(
    () =>
      (buildings || []).map((b) => ({
        value: b.id,
        label: (
          <Flex gap={8} align="center">
            <HomeOutlined />
            <span>{b.buildingName}</span>
            {b.code && <Tag style={{ marginLeft: 8 }}>{b.code}</Tag>}
          </Flex>
        ),
      })),
    [buildings]
  );

  // Sort options
  const sortOptions = [
    { value: "fullName", label: "Họ tên" },
    { value: "createdAt", label: "Ngày tạo" },
    { value: "phone", label: "Số điện thoại" },
  ];

  const handleTableChange = (pagination, filters, sorter) => {
    if (sorter && sorter.field) {
      setSortBy(sorter.field);
      setSortDir(sorter.order === "ascend" ? "asc" : "desc");
    } else {
      setSortBy(null);
      setSortDir("asc");
    }
  };

  return (
    <Card
      title="Danh sách cư dân"
      extra={
        <Button
          onClick={() => loadData()}
          icon={<ReloadOutlined />}
          type="default"
          disabled={!schemaName}
        >
          Tải lại
        </Button>
      }
      styles={{ header: { borderBottom: "1px solid var(--ant-color-border)" } }}
    >
      {/* Filters */}
      <Card size="small" style={{ marginBottom: 16 }}>
        <Flex gap={12} wrap="wrap">
          <Select
            style={{ minWidth: 280 }}
            placeholder="Chọn toà nhà"
            options={buildingOptions}
            value={buildingId}
            onChange={(v) => {
              setBuildingId(v);
              setPaging((p) => ({ ...p, current: 1 }));
            }}
            allowClear
            showSearch
            optionFilterProp="label"
          />

          <Input
            style={{ minWidth: 320 }}
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            onPressEnter={() => {
              setPaging((p) => ({ ...p, current: 1 }));
              loadData();
            }}
            prefix={<SearchOutlined />}
            allowClear
            placeholder="Tìm theo tên / email / số điện thoại"
          />

          <Select
            style={{ minWidth: 180 }}
            placeholder="Sắp xếp theo"
            options={sortOptions}
            value={sortBy}
            onChange={(v) => {
              setSortBy(v || null);
              setPaging((p) => ({ ...p, current: 1 }));
            }}
            allowClear
            disabled={!schemaName}
          />

          <Select
            style={{ minWidth: 120 }}
            placeholder="Thứ tự"
            value={sortDir}
            onChange={(v) => {
              setSortDir(v);
              setPaging((p) => ({ ...p, current: 1 }));
            }}
            disabled={!sortBy || !schemaName}
            options={[
              { value: "asc", label: "Tăng dần" },
              { value: "desc", label: "Giảm dần" },
            ]}
          />

          <Space>
            <Button
              type="primary"
              onClick={() => {
                setPaging((p) => ({ ...p, current: 1 }));
                loadData();
              }}
              disabled={!schemaName}
            >
              Tìm kiếm
            </Button>
            <Button onClick={resetFilters}>Xoá lọc</Button>
          </Space>
        </Flex>
      </Card>

      {/* Table */}
      {data.length === 0 && !loading ? (
        <Empty description="Không có dữ liệu cư dân" />
      ) : (
        <Table
          columns={columns}
          dataSource={data}
          rowKey="residentId"
          loading={loading}
          tableLayout="auto"
          scroll={{ x: true }}
          onChange={handleTableChange}
          pagination={{
            current: paging.current,
            pageSize: paging.pageSize,
            total,
            showSizeChanger: true,
            onChange: (current, pageSize) => setPaging({ current, pageSize }),
            showTotal: (t) => `Tổng ${t} cư dân`,
            pageSizeOptions: ["10", "20", "50", "100"],
          }}
        />
      )}
    </Card>
  );
}









