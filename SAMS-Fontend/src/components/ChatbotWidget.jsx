/**
 * Chatbot Widget Component
 * Widget floating chatbot hiển thị ở trang Homepage
 */
import React, { useState, useEffect, useRef } from "react";
import {
  FloatButton,
  Input,
  Button,
  message,
  Typography,
  Spin,
} from "antd";
import {
  MessageOutlined,
  SendOutlined,
  CloseOutlined,
} from "@ant-design/icons";
import ReactMarkdown from "react-markdown";
import chatbotService from "../lib/chatbotClient";

const { Text } = Typography;

const CHATBOT_WIDGET_STYLES = `
.chatbot-window {
  position: fixed;
  right: 24px;
  bottom: 84px;
  z-index: 1200;
}

.chatbot-panel {
  width: 320px;
  height: 520px;
  max-height: calc(100vh - 150px);
  border-radius: 14px;
  overflow: hidden;
  background: #fff;
  display: flex;
  flex-direction: column;
  box-shadow: 0 20px 45px rgba(15, 23, 42, 0.25);
}

.chatbot-header {
  padding: 14px 16px;
  background: #7c3aed;
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: space-between;
  position: relative;
}

.chatbot-header-brand {
  font-size: 15px;
  font-weight: 600;
  letter-spacing: 0.02em;
}

.chatbot-close-icon {
  position: absolute;
  top: 12px;
  right: 12px;
  background: rgba(255, 255, 255, 0.12);
  border-radius: 50%;
  width: 28px;
  height: 28px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: background 0.2s ease;
}

.chatbot-close-icon:hover {
  background: rgba(255, 255, 255, 0.3);
}

.chatbot-body {
  flex: 1;
  background: #fff;
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: hidden;
}

.chatbot-messages-container {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-height: 0;
  max-height: 100%;
}

.chatbot-message {
  display: flex;
  gap: 10px;
  align-items: flex-end;
  justify-content: flex-end;
}

.chatbot-message.bot {
  justify-content: flex-start;
}

.chatbot-avatar {
  width: 30px;
  height: 30px;
  border-radius: 8px;
  background: linear-gradient(135deg, #6366f1, #4f46e5);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  font-size: 16px;
}

.chatbot-message-content {
  max-width: 100%;
  padding: 12px 14px;
  border-radius: 16px;
  position: relative;
  box-shadow: 0 10px 24px rgba(15, 23, 42, 0.08);
  font-size: 14px;
}

.chatbot-message-content h1,
.chatbot-message-content h2,
.chatbot-message-content h3 {
  margin: 0.5em 0;
  font-weight: 600;
}

.chatbot-message-content ul,
.chatbot-message-content ol {
  margin: 0.5em 0;
  padding-left: 1.5em;
}

.chatbot-message-content li {
  margin: 0.25em 0;
}

.chatbot-message-content p {
  margin: 0.5em 0;
}

.chatbot-message-content strong {
  font-weight: 600;
}

.chatbot-message-content code {
  background: rgba(0, 0, 0, 0.05);
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 13px;
}

.chatbot-message.user .chatbot-message-content {
  background: linear-gradient(135deg, #2563eb, #0ea5e9);
  color: #fff;
  border-bottom-right-radius: 6px;
}

.chatbot-message.bot .chatbot-message-content {
  background: #fff;
  color: #0b1f3a;
  border: 1px solid #e5e9ff;
  border-bottom-left-radius: 6px;
}

.chatbot-message-meta {
  display: flex;
  justify-content: space-between;
  font-size: 11px;
  color: #8e9ab0;
  margin-top: 8px;
  opacity: 0.9;
}

.chatbot-input-area {
  padding: 10px 12px;
  background: #fff;
  display: flex;
  gap: 8px;
  align-items: flex-start;
  border-top: 1px solid #f1f5ff;
}

.chatbot-input-single {
  flex: 1;
  border-radius: 14px;
  border: 1px solid #e2e8f0;
  box-shadow: inset 0 1px 0 rgba(48, 64, 255, 0.04);
  font-size: 14px;
  line-height: 1.5;
}

.chatbot-send-button {
  border-radius: 14px;
  padding: 0 20px;
  height: 36px;
}

.chatbot-message-content .ant-spin {
  margin-right: 10px;
}

.chatbot-message-loading .chatbot-message-content {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.chatbot-messages-container::-webkit-scrollbar {
  width: 6px;
}

.chatbot-messages-container::-webkit-scrollbar-track {
  background: transparent;
}

.chatbot-messages-container::-webkit-scrollbar-thumb {
  background: rgba(124, 58, 237, 0.6);
  border-radius: 3px;
}
`;

const ChatbotWidget = () => {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState([]);
  const [inputValue, setInputValue] = useState("");
  const [loading, setLoading] = useState(false);
  const [isInitialized, setIsInitialized] = useState(false);
  const messagesEndRef = useRef(null);

  // Scroll to bottom khi có tin nhắn mới
  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  // Initialize chatbot session
  useEffect(() => {
    if (open && !isInitialized) {
      initializeChat();
    }
  }, [open, isInitialized]);

  const initializeChat = async () => {
    try {
      await chatbotService.getOrCreateSession();
      setIsInitialized(true);

      // Tin nhắn chào mừng
      setMessages([
        {
          role: "bot",
          content:
            "Xin chào! Tôi là trợ lý ảo của chung cư. Tôi có thể giúp bạn:\n\n" +
            "🏢 Tìm kiếm căn hộ\n" +
            "🏊 Xem thông tin tiện ích\n" +
            "💰 Tra cứu phí dịch vụ\n" +
            "📊 Thống kê căn hộ\n\n" +
            "Bạn cần tôi giúp gì?",
          timestamp: new Date(),
        },
      ]);
    } catch (error) {
      message.error("Không thể kết nối đến chatbot. Vui lòng thử lại sau.");
    }
  };

  const handleSend = async () => {
    if (!inputValue.trim() || loading) return;

    const userMessage = {
      role: "user",
      content: inputValue.trim(),
      timestamp: new Date(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setInputValue("");
    setLoading(true);

    try {
      const response = await chatbotService.sendMessage(userMessage.content);

      if (response.success) {
        const botMessage = {
          role: "bot",
          content: response.response,
          functionCalls: response.function_calls,
          timestamp: new Date(),
        };
        setMessages((prev) => [...prev, botMessage]);
      } else {
        throw new Error(response.error || "Unknown error");
      }
    } catch (error) {
      message.error("Có lỗi xảy ra. Vui lòng thử lại.");
      const errorMessage = {
        role: "bot",
        content: "Xin lỗi, tôi gặp lỗi khi xử lý yêu cầu của bạn. Vui lòng thử lại.",
        timestamp: new Date(),
      };
      setMessages((prev) => [...prev, errorMessage]);
    } finally {
      setLoading(false);
    }
  };

  const handleReset = async () => {
    try {
      await chatbotService.resetConversation();
      setMessages([]);
      message.success("Đã reset cuộc hội thoại");
      await initializeChat();
    } catch (error) {
      message.error("Không thể reset cuộc hội thoại");
    }
  };

  const formatTimestamp = (date) => {
    return date.toLocaleTimeString("vi-VN", {
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  return (
    <>
      <style>{CHATBOT_WIDGET_STYLES}</style>
      <FloatButton
        icon={<MessageOutlined />}
        type="primary"
        style={{
          right: 24,
          bottom: 120,
          width: 62,
          height: 62,
          borderRadius: 18,
          
          boxShadow: "0 12px 24px rgba(37, 99, 235, 0.4)",
          border: "none",
        }}
        onClick={() => setOpen(true)}
        tooltip="Hỏi Chatbot"
      />

      {open && (
        <div className="chatbot-window">
          <div className="chatbot-panel">
            <div className="chatbot-header">
              <div className="chatbot-header-brand">
                <Text strong>Chat Bot</Text>
              </div>

              <CloseOutlined
                className="chatbot-close-icon"
                onClick={() => setOpen(false)}
              />
            </div>

            <div className="chatbot-body">
              <div className="chatbot-messages-container">
                {messages.map((msg, idx) => (
                  <div key={idx} className={`chatbot-message ${msg.role}`}>
                  {msg.role === "bot" && (
                    <div className="chatbot-avatar">🤖</div>
                  )}
                    <div className="chatbot-message-content">
                      {msg.role === "bot" ? (
                        <ReactMarkdown>{msg.content}</ReactMarkdown>
                      ) : (
                        <Text style={{ whiteSpace: "pre-wrap" }}>
                          {msg.content}
                        </Text>
                      )}

                      <div className="chatbot-message-meta">
                      <Text type="secondary">{formatTimestamp(msg.timestamp)}</Text>
                        <Text type="secondary">
                          {msg.role === "bot" ? "Chatbot" : "Bạn"}
                        </Text>
                      </div>
                    </div>
                  </div>
                ))}

                {loading && (
                  <div className="chatbot-message bot chatbot-message-loading">
                    <div className="chatbot-message-content">
                      <Spin size="small" />
                      <Text style={{ marginLeft: 8 }}>Đang suy nghĩ...</Text>
                    </div>
                  </div>
                )}

                <div ref={messagesEndRef} />
              </div>

              <div className="chatbot-input-area">
                <Input
                  value={inputValue}
                  onChange={(e) => setInputValue(e.target.value)}
                  onPressEnter={handleSend}
                  placeholder="Nhập câu hỏi..."
                  disabled={loading}
                  className="chatbot-input-single"
                  allowClear
                />
                <Button
                  type="primary"
                  icon={<SendOutlined />}
                  onClick={handleSend}
                  loading={loading}
                  disabled={!inputValue.trim()}
                  className="chatbot-send-button"
                >
                  Gửi
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
};

export default ChatbotWidget;
