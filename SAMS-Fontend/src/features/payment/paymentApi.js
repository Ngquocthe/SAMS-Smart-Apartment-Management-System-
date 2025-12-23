import apiClient from '../../lib/apiClient';

export const paymentApi = {
  // Tạo mã QR thanh toán
  createPayment: async (paymentData) => {
    try {
      const response = await apiClient.post('/Payment/create', {
        amount: paymentData.amount,
        description: paymentData.description || "Thanh toán dịch vụ SAMS",
        expiredAt: paymentData.expiredAt || 30,
        items: paymentData.items || []
      });

      if (response.data?.success) {
        return {
          success: true,
          orderCode: response.data.orderCode,
          qrCode: response.data.qrCode,
          checkoutUrl: response.data.checkoutUrl,
          paymentLinkId: response.data.paymentLinkId
        };
      } else {
        throw new Error(response.data?.message || 'Không thể tạo mã QR thanh toán');
      }
    } catch (error) {
      console.error('Payment create error:', error);
      throw new Error(
        error.response?.data?.message || 
        error.message || 
        'Có lỗi xảy ra khi tạo thanh toán'
      );
    }
  },

  // Kiểm tra trạng thái thanh toán
  checkPaymentStatus: async (orderCode, amount) => {
    try {
      const response = await apiClient.get(
        `/Payment/status/${orderCode}?amount=${amount}`
      );

      if (response.data?.success) {
        return {
          success: true,
          status: response.data.status, // PENDING, PAID, FAILED
          message: response.data.message,
          data: response.data.data
        };
      } else {
        throw new Error(response.data?.message || 'Không thể kiểm tra trạng thái thanh toán');
      }
    } catch (error) {
      console.error('Payment status check error:', error);
      throw new Error(
        error.response?.data?.message || 
        error.message || 
        'Có lỗi xảy ra khi kiểm tra trạng thái thanh toán'
      );
    }
  }
};