/**
 * Chatbot API Client
 * Tích hợp với Apartment Chatbot Backend
 * Tạo riêng instance axios cho chatbot (không cần Keycloak token)
 */
import axios from "axios";

const chatbotApi = axios.create({
  baseURL:
    process.env.REACT_APP_CHATBOT_API_URL || "https://noahbuilding.me/chatbot",
  timeout: 30000,
  headers: {
    "Content-Type": "application/json",
  },
});

// Request interceptor - Log requests
chatbotApi.interceptors.request.use(
  (config) => {
    if (process.env.REACT_APP_ENVIRONMENT === "development") {
      console.log(
        `Chatbot Request: ${config.method?.toUpperCase()} ${config.url}`
      );
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor - Log responses
chatbotApi.interceptors.response.use(
  (response) => {
    if (process.env.REACT_APP_ENVIRONMENT === "development") {
      console.log(
        `Chatbot Response: ${response.status} ${response.config.url}`
      );
    }
    return response;
  },
  (error) => {
    if (process.env.REACT_APP_ENVIRONMENT === "development") {
      console.error(
        `Chatbot Error: ${error.response?.status} ${error.config?.url}`,
        error.response?.data
      );
    }
    return Promise.reject(error);
  }
);

class ChatbotService {
  constructor() {
    this.sessionId = null;
  }

  /**
   * Tạo session mới
   */
  async createSession() {
    try {
      const response = await chatbotApi.post("/session/new");
      this.sessionId = response.data.session_id;
      localStorage.setItem("chatbot_session_id", this.sessionId);
      return response.data;
    } catch (error) {
      console.error("Error creating chatbot session:", error);
      throw error;
    }
  }

  /**
   * Lấy session hiện tại hoặc tạo mới
   */
  async getOrCreateSession() {
    const storedSessionId = localStorage.getItem("chatbot_session_id");
    if (storedSessionId) {
      this.sessionId = storedSessionId;
      return { session_id: storedSessionId };
    }
    return await this.createSession();
  }

  /**
   * Gửi tin nhắn đến chatbot
   */
  async sendMessage(message) {
    try {
      if (!this.sessionId) {
        await this.getOrCreateSession();
      }

      const response = await chatbotApi.post("/chat", {
        message: message,
        session_id: this.sessionId,
      });

      if (response.data.session_id) {
        this.sessionId = response.data.session_id;
        localStorage.setItem("chatbot_session_id", this.sessionId);
      }

      return response.data;
    } catch (error) {
      console.error("Error sending message to chatbot:", error);
      throw error;
    }
  }

  /**
   * Reset cuộc hội thoại
   */
  async resetConversation() {
    try {
      if (!this.sessionId) return;

      const response = await chatbotApi.post(
        `/session/${this.sessionId}/reset`
      );
      return response.data;
    } catch (error) {
      console.error("Error resetting conversation:", error);
      throw error;
    }
  }

  /**
   * Xóa session
   */
  async deleteSession() {
    try {
      if (!this.sessionId) return;

      await chatbotApi.delete(`/session/${this.sessionId}`);
      this.sessionId = null;
      localStorage.removeItem("chatbot_session_id");
    } catch (error) {
      console.error("Error deleting session:", error);
      this.sessionId = null;
      localStorage.removeItem("chatbot_session_id");
    }
  }

  /**
   * Health check
   */
  async healthCheck() {
    try {
      const response = await chatbotApi.get("/health");
      return response.data;
    } catch (error) {
      return { status: "offline" };
    }
  }
}

export default new ChatbotService();
