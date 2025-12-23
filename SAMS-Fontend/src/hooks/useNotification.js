import { message, notification } from 'antd';
import { createContext, useContext } from 'react';

// Create context for notification
const NotificationContext = createContext(null);

/**
 * Provider component for notifications
 */
export const NotificationProvider = ({ children }) => {
  // Create notification API
  const [messageApi, messageContextHolder] = message.useMessage();
  const [notificationApi, notificationContextHolder] = notification.useNotification();

  // Notification functions
  const showMessage = (type, content, duration = 3) => {
    const config = {
      content,
      duration,
      className: 'custom-message-alert',
      style: {
        marginTop: '20vh', // Hiển thị cao hơn trên màn hình
      },
    };

    switch (type) {
      case 'success':
        messageApi.success(config);
        break;
      case 'warning':
        messageApi.warning(config);
        break;
      case 'error':
      case 'danger':
        messageApi.error(config);
        break;
      default:
        messageApi.info(config);
    }
  };

  /**
   * Hiển thị thông báo chi tiết hơn (notification)
   * @param {string} type - Loại thông báo: success, warning, error, info
   * @param {string} title - Tiêu đề thông báo
   * @param {string} description - Nội dung chi tiết
   * @param {number} duration - Thời gian hiển thị (giây)
   */
  const showNotification = (type, title, description, duration = 4.5) => {
    const config = {
      message: title,
      description,
      duration,
      placement: 'topRight',
      style: {
        borderRadius: '4px',
        boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
      },
    };

    switch (type) {
      case 'success':
        notificationApi.success(config);
        break;
      case 'warning':
        notificationApi.warning(config);
        break;
      case 'error':
      case 'danger':
        notificationApi.error(config);
        break;
      default:
        notificationApi.info(config);
    }
  };

  const contextValue = {
    showMessage,
    showNotification,
  };

  return (
    <NotificationContext.Provider value={contextValue}>
      {messageContextHolder}
      {notificationContextHolder}
      {children}
    </NotificationContext.Provider>
  );
};

/**
 * Custom hook để hiển thị thông báo nhất quán trong ứng dụng
 */
export const useNotification = () => {
  const context = useContext(NotificationContext);
  
  if (!context) {
    // Fallback to static methods if context is not available
    return {
      showMessage: (type, content, duration = 3) => {
        const config = {
          content,
          duration,
        };
        
        switch (type) {
          case 'success':
            message.success(config);
            break;
          case 'warning':
            message.warning(config);
            break;
          case 'error':
          case 'danger':
            message.error(config);
            break;
          default:
            message.info(config);
        }
      },
      showNotification: (type, title, description, duration = 4.5) => {
        const config = {
          message: title,
          description,
          duration,
          placement: 'topRight',
        };
        
        switch (type) {
          case 'success':
            notification.success(config);
            break;
          case 'warning':
            notification.warning(config);
            break;
          case 'error':
          case 'danger':
            notification.error(config);
            break;
          default:
            notification.info(config);
        }
      }
    };
  }
  
  return context;
};

export default useNotification;