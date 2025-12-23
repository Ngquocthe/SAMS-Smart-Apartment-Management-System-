import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { BellOutlined } from '@ant-design/icons';
import { Badge, Dropdown, List, Typography, Empty, Spin, Tag } from 'antd';
import { announcementApi } from '../features/building-management/announcementApi';
import { assetsApi } from '../features/building-management/assetsApi';
import ROUTER_PAGE from '../constants/Routes';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import 'dayjs/locale/vi';

dayjs.extend(relativeTime);
dayjs.locale('vi');

const { Text } = Typography;

const AMENITY_NOTIFICATION_TYPES = [
  'AMENITY_BOOKING_SUCCESS',
  'AMENITY_EXPIRATION_REMINDER',
  'AMENITY_EXPIRED',
  'AMENITY_MAINTENANCE_REMINDER'
];
const DISMISSED_STORAGE_KEY = 'amenityDismissedNotifications';

const getInitialDismissedIds = () => {
  if (typeof window === 'undefined') return [];
  try {
    const stored = window.localStorage.getItem(DISMISSED_STORAGE_KEY);
    if (!stored) return [];
    const parsed = JSON.parse(stored);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
};

export default function NotificationBell({ onlyMaintenance = false }) {
  const navigate = useNavigate();
  const [unreadCount, setUnreadCount] = useState(0);
  const [notifications, setNotifications] = useState([]);
  const [assetNotices, setAssetNotices] = useState([]);
  const [loading, setLoading] = useState(false);
  const [dropdownVisible, setDropdownVisible] = useState(false);
  const [dismissedIds, internalSetDismissedIds] = useState(getInitialDismissedIds);
  const setDismissedIds = useCallback((updater) => {
    internalSetDismissedIds((prev) => {
      const next = typeof updater === 'function' ? updater(prev) : updater;
      if (!onlyMaintenance) {
        try {
          localStorage.setItem(DISMISSED_STORAGE_KEY, JSON.stringify(next));
        } catch {}
      }
      return next;
    });
  }, [onlyMaintenance]);
  const intervalRef = useRef(null);

  const fetchUnreadCount = useCallback(async () => {
    if (!onlyMaintenance) return;
    try {
      const maintenanceSchedules = await assetsApi.getDueForReminder().catch(() => []);
      const maintenanceCount = Array.isArray(maintenanceSchedules) ? maintenanceSchedules.length : 0;
      setUnreadCount(maintenanceCount);
    } catch (error) {}
  }, [onlyMaintenance]);

  const isAnnouncementCurrentlyVisible = useCallback((announcement) => {
    if (!announcement) return false;
    const now = dayjs();
    const visibleFrom = announcement.visibleFrom ? dayjs(announcement.visibleFrom) : null;
    const visibleTo = announcement.visibleTo ? dayjs(announcement.visibleTo) : null;
    const statusOk = !announcement.status || announcement.status === 'ACTIVE';
    const afterStart = !visibleFrom || now.isAfter(visibleFrom) || now.isSame(visibleFrom);
    const beforeEnd = !visibleTo || now.isBefore(visibleTo) || now.isSame(visibleTo);
    return statusOk && afterStart && beforeEnd;
  }, []);

  const fetchNotifications = useCallback(async () => {
    try {
      setLoading(true);
      
      if (onlyMaintenance) {
        // Backend đã filter và chỉ trả về các lịch bảo trì chưa hết hạn (due-for-reminder)
        // FE chỉ cần format và hiển thị dữ liệu từ API
        const maintenanceSchedules = await assetsApi.getDueForReminder().catch(() => []);

        const maintenanceList = Array.isArray(maintenanceSchedules) ? maintenanceSchedules.map(item => {
          const assetName = item.asset?.name ?? item.assetName ?? 'Tài sản';
          
          return {
            ...item,
            type: 'maintenance',
            id: item.scheduleId || item.id,
            announcementId: item.scheduleId || item.id,
            title: `Bảo trì tài sản: ${assetName}`,
            content: (
              <>
                <div>Lịch bảo trì sẽ bắt đầu vào ngày {dayjs(item.startDate).format('DD/MM/YYYY')}</div>
                <div>Và kết thúc vào ngày {dayjs(item.endDate).format('DD/MM/YYYY')}</div>
              </>
            ),
            createdAt: item.startDate,
            scheduleId: item.scheduleId || item.id
          };
        }) : [];

        const allNotifications = maintenanceList.sort((a, b) => {
          const dateA = dayjs(a.createdAt || a.startDate);
          const dateB = dayjs(b.createdAt || b.startDate);
          return dateB.diff(dateA);
        });

        setNotifications(allNotifications);
        setUnreadCount(allNotifications.length);
      } else {
        const [unreadResponse, residentAnnouncements] = await Promise.all([
          announcementApi.getUnread('RESIDENT').catch(() => null),
          announcementApi.getByScope('RESIDENT').catch(() => [])
        ]);
        
        let announcements = [];
        if (Array.isArray(unreadResponse)) {
          announcements = unreadResponse;
        } else if (unreadResponse && Array.isArray(unreadResponse.announcements)) {
          announcements = unreadResponse.announcements;
        } else if (unreadResponse && Array.isArray(unreadResponse.data)) {
          announcements = unreadResponse.data;
        }

        const announcementList = announcements.map(item => ({
          ...item,
          type: item.type || item.announcementType || 'announcement',
          id: item.announcementId || item.id,
          bookingId: item.bookingId || item.booking_id || null
        }));

        const allNotifications = announcementList.sort((a, b) => {
          const dateA = dayjs(a.createdAt || a.created_at || a.visibleFrom);
          const dateB = dayjs(b.createdAt || b.created_at || b.visibleFrom);
          return dateB.diff(dateA);
        });

        setNotifications(allNotifications);

        const residentList = Array.isArray(residentAnnouncements)
          ? residentAnnouncements
          : Array.isArray(residentAnnouncements?.announcements)
            ? residentAnnouncements.announcements
            : [];

        const activeAssetNotices = residentList
          .filter(item => (item.type === 'ASSET_MAINTENANCE_NOTICE') && isAnnouncementCurrentlyVisible(item))
          .map(item => ({
            ...item,
            type: 'ASSET_MAINTENANCE_NOTICE',
            id: item.announcementId || item.id
          }))
          .sort((a, b) => {
            const dateA = dayjs(a.createdAt || a.visibleFrom);
            const dateB = dayjs(b.createdAt || b.visibleFrom);
            return dateB.diff(dateA);
          });

        setAssetNotices(activeAssetNotices);
      }
    } catch (error) {
      setNotifications([]);
      setAssetNotices([]);
    } finally {
      setLoading(false);
    }
  }, [onlyMaintenance, isAnnouncementCurrentlyVisible]);

  useEffect(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }

    if (onlyMaintenance) {
      fetchUnreadCount();
      intervalRef.current = setInterval(() => {
        fetchUnreadCount();
      }, 5000);
    } else {
      fetchNotifications();
      intervalRef.current = setInterval(() => {
        fetchNotifications();
      }, 15000);
    }

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    };
  }, [onlyMaintenance, fetchUnreadCount, fetchNotifications]);

  useEffect(() => {
    if (dropdownVisible) {
      fetchNotifications();
    }
  }, [dropdownVisible, fetchNotifications]);

  useEffect(() => {
    if (onlyMaintenance) return;
    const handleNotificationUpdate = () => {
      fetchUnreadCount();
      fetchNotifications();
    };
    window.addEventListener('amenity-notification-updated', handleNotificationUpdate);
    return () => {
      window.removeEventListener('amenity-notification-updated', handleNotificationUpdate);
    };
  }, [onlyMaintenance, fetchUnreadCount, fetchNotifications]);

  useEffect(() => {
    if (onlyMaintenance || notifications.length === 0) return;
    setDismissedIds(prev => {
      const ids = notifications.map(item => item.announcementId || item.id);
      return prev.filter(id => ids.includes(id));
    });
  }, [onlyMaintenance, notifications, setDismissedIds]);

  // Tính số lượng thông báo chưa đọc dựa trên dismissed IDs
  // dismissedIds chỉ dùng để đếm badge count, KHÔNG dùng để ẩn thông báo
  const amenityUnreadCount = useMemo(() => {
    if (onlyMaintenance) return unreadCount;
    const dismissedSet = new Set(dismissedIds);
    return notifications.filter(item => !dismissedSet.has(item.announcementId || item.id)).length;
  }, [onlyMaintenance, unreadCount, notifications, dismissedIds]);

  // Kết hợp tất cả thông báo để hiển thị
  // Backend đã filter theo Status và VisibleTo, frontend chỉ cần hiển thị
  const combinedNotifications = useMemo(() => {
    if (onlyMaintenance) {
      return notifications;
    }

    const map = new Map();
    assetNotices.forEach(item => {
      const key = item.announcementId || item.id;
      if (!key) return;
      map.set(key, item);
    });

    notifications.forEach(item => {
      const key = item.announcementId || item.id;
      if (!key) return;
      if (!map.has(key)) {
        map.set(key, item);
      }
    });

    return Array.from(map.values()).sort((a, b) => {
      const dateA = dayjs(a.createdAt || a.visibleFrom);
      const dateB = dayjs(b.createdAt || b.visibleFrom);
      return dateB.diff(dateA);
    });
  }, [notifications, assetNotices, onlyMaintenance]);

  // Badge count: số thông báo chưa đọc
  const badgeCount = onlyMaintenance ? unreadCount : amenityUnreadCount;

  // Xử lý khi click vào thông báo
  const handleNotificationClick = async (notification) => {
    try {
      const isAmenityNotification = AMENITY_NOTIFICATION_TYPES.includes(notification.type);
      const isAssetMaintenanceNotice = notification.type === 'ASSET_MAINTENANCE_NOTICE';
      const isAmenityMaintenanceReminder = notification.type === 'AMENITY_MAINTENANCE_REMINDER';
      const isAmenityBookingSuccess = notification.type === 'AMENITY_BOOKING_SUCCESS';
      const isMaintenanceCompleted = notification.type === 'MAINTENANCE_COMPLETED';

      // Đóng dropdown
      setDropdownVisible(false);

      // Đánh dấu đã đọc (chỉ để giảm badge count, không ẩn thông báo)
      const notificationId = notification.announcementId || notification.id;
      if (notificationId && !onlyMaintenance) {
        setDismissedIds(prev => (prev.includes(notificationId) ? prev : [...prev, notificationId]));
      }

      // Xử lý thông báo bảo trì tiện ích: chỉ đánh dấu đã đọc, không navigate
      if (isAmenityMaintenanceReminder || isMaintenanceCompleted) {
        return; // Đã đánh dấu đã đọc ở trên
      }

      // Xử lý thông báo đăng ký thành công: navigate đến trang lịch sử đăng ký
      if (isAmenityBookingSuccess) {
        navigate(ROUTER_PAGE.RESIDENT.AMENITY_BOOKING, {
          state: {
            openHistoryModal: true,
            bookingId: notification.bookingId || notification.booking_id || null
          }
        });
        return;
      }

      // Xử lý các thông báo amenity khác (expiration, expired): navigate đến trang lịch sử đăng ký
      if (isAmenityNotification && !isAmenityMaintenanceReminder && !isAmenityBookingSuccess) {
        navigate(ROUTER_PAGE.RESIDENT.AMENITY_BOOKING, {
          state: {
            openHistoryModal: true,
            bookingId: notification.bookingId || notification.booking_id || null
          }
        });
        return;
      }
      
      // Nếu là thông báo conflict, navigate đến trang lịch sử đăng ký (không có bookingId cụ thể)
      if (notification.type === 'AMENITY_BOOKING_CONFLICT') {
        navigate(ROUTER_PAGE.RESIDENT.AMENITY_BOOKING, {
          state: {
            openHistoryModal: true
          }
        });
        return;
      }

      // Xử lý thông báo bảo trì tài sản: chỉ đánh dấu đã đọc
      if (isAssetMaintenanceNotice) {
        return; // Đã đánh dấu đã đọc ở trên
      }

      // Đánh dấu đã đọc cho các thông báo khác (thông báo chung, sự kiện...)
      if (!isAmenityNotification && !isAssetMaintenanceNotice && (notification.announcementId || notification.id)) {
        await announcementApi.markAsRead(notification.announcementId || notification.id);
        // Không cần filter ra khỏi danh sách, để backend tự ẩn khi hết hạn
      }
    } catch (error) {
      console.error('Error handling notification click:', error);
    }
  };

  const formatDate = (dateString) => {
    if (!dateString) return '';
    const date = dayjs(dateString);
    const now = dayjs();
    const diffDays = now.diff(date, 'day');
    
    if (diffDays === 0) {
      return date.format('HH:mm');
    } else if (diffDays === 1) {
      return 'Hôm qua';
    } else if (diffDays < 7) {
      return date.format('dddd');
    } else {
      return date.format('DD/MM/YYYY');
    }
  };

  const notificationContent = (
    <div style={{ 
      width: 360, 
      maxHeight: 480, 
      overflowY: 'auto',
      backgroundColor: '#ffffff',
      borderRadius: '8px',
      boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)'
    }}>
      <div style={{ 
        padding: '12px 16px', 
        borderBottom: '1px solid #f0f0f0',
        backgroundColor: '#ffffff',
        borderRadius: '8px 8px 0 0'
      }}>
        <Text strong style={{ fontSize: '16px', color: '#262626' }}>Thông báo</Text>
      </div>
      {loading ? (
        <div style={{ padding: '24px', textAlign: 'center', backgroundColor: '#ffffff' }}>
          <Spin />
        </div>
      ) : notifications.length === 0 ? (
        <Empty 
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          description="Không có thông báo mới"
          style={{ padding: '24px', backgroundColor: '#ffffff' }}
        />
      ) : (
        <List
          style={{ backgroundColor: '#ffffff' }}
          dataSource={combinedNotifications}
          renderItem={(item) => (
            <List.Item
              style={{ 
                padding: '12px 16px',
                cursor: 'pointer',
                borderBottom: '1px solid #f0f0f0',
                backgroundColor: '#ffffff'
              }}
              onClick={() => handleNotificationClick(item)}
              onMouseEnter={(e) => {
                e.currentTarget.style.backgroundColor = '#f5f5f5';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.backgroundColor = '#ffffff';
              }}
            >
              <List.Item.Meta
                title={
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    { !dismissedIds.includes(item.announcementId || item.id) ? (
                      <span
                        style={{
                          width: 8,
                          height: 8,
                          borderRadius: '50%',
                          backgroundColor: '#ff4d4f',
                          display: 'inline-block'
                        }}
                      />
                    ) : (
                      <span
                        style={{
                          width: 8,
                          height: 8,
                          borderRadius: '50%',
                          backgroundColor: 'transparent',
                          display: 'inline-block'
                        }}
                      />
                    )}
                    <Text strong style={{ fontSize: '14px', color: '#262626' }}>
                      {item.title || 'Thông báo'}
                    </Text>
                    {(item.type === 'maintenance' || item.type === 'MAINTENANCE_REMINDER' || item.type === 'MAINTENANCE_ASSIGNMENT' || item.type === 'ASSET_MAINTENANCE_NOTICE') && (
                      <Tag color="orange" style={{ margin: 0 }}>Bảo trì</Tag>
                    )}
                    {(item.type === 'AMENITY_BOOKING_SUCCESS' || item.type === 'AMENITY_EXPIRATION_REMINDER' || item.type === 'AMENITY_EXPIRED' || item.type === 'AMENITY_BOOKING_CONFLICT' || item.type === 'AMENITY_MAINTENANCE_REMINDER') && (
                      <Tag color={item.type === 'AMENITY_BOOKING_CONFLICT' ? 'red' : 'blue'} style={{ margin: 0 }}>
                        {item.type === 'AMENITY_BOOKING_CONFLICT' ? 'Trùng lịch' : 'Tiện ích'}
                      </Tag>
                    )}
                  </div>
                }
                description={
                  <div>
                    <div style={{ 
                      fontSize: '12px', 
                      color: '#595959',
                      marginBottom: '4px',
                      whiteSpace: 'pre-wrap',
                      lineHeight: 1.4
                    }}>
                      {typeof item.content === 'string' ? (item.content || item.description || '') : (item.content || item.description)}
                    </div>
                    <Text type="secondary" style={{ fontSize: '11px', color: '#8c8c8c' }}>
                      {formatDate(item.createdAt || item.created_at || item.visibleFrom || item.startDate)}
                    </Text>
                  </div>
                }
              />
            </List.Item>
          )}
        />
      )}
    </div>
  );

  return (
    <Dropdown
      overlay={notificationContent}
      trigger={['click']}
      visible={dropdownVisible}
      onVisibleChange={setDropdownVisible}
      placement="bottomRight"
      overlayStyle={{
        backgroundColor: '#ffffff',
        borderRadius: '8px',
        boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)'
      }}
    >
      <div 
        style={{ 
          position: 'relative', 
          cursor: 'pointer',
          padding: '8px',
          borderRadius: '8px',
          transition: 'background-color 0.2s'
        }}
        onMouseEnter={(e) => {
          e.currentTarget.style.backgroundColor = 'rgba(255, 255, 255, 0.1)';
        }}
        onMouseLeave={(e) => {
          e.currentTarget.style.backgroundColor = 'transparent';
        }}
      >
        <Badge count={badgeCount} overflowCount={99}>
          <BellOutlined 
            style={{ 
              fontSize: '20px', 
              color: '#cbd5e1'
            }} 
          />
        </Badge>
      </div>
    </Dropdown>
  );
}

