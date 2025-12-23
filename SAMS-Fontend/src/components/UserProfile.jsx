import { useEffect, useState, useMemo } from "react";
import { useUser } from "../hooks/useUser";
import { keycloak } from "../keycloak/initKeycloak";
import { Avatar } from "antd";

const UserProfile = () => {
  const { user, isLoading, error, fetchUserData } = useUser();
  const [keycloakUser, setKeycloakUser] = useState(null);

  const firstInitial = useMemo(
    () =>
      (user?.firstName?.trim?.() || user?.fullName?.trim?.() || "U")
        .charAt(0)
        .toUpperCase(),
    [user?.firstName, user?.fullName]
  );

  useEffect(() => {
    if (keycloak.authenticated) {
      setKeycloakUser(keycloak.tokenParsed);
      // Fetch additional user data if needed
      if (keycloak.tokenParsed?.sub) {
        fetchUserData(keycloak.tokenParsed.sub);
      }
    }
  }, [fetchUserData]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-red-600 text-center">
          <p className="text-xl font-semibold mb-2">Lỗi tải thông tin</p>
          <p>{error}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-gray-50 py-8">
      <div className="mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 mb-6">
          <div className="px-6 py-8">
            <div className="flex items-center space-x-6">
              <div className="flex-shrink-0">
                <Avatar
                  size={120}
                  shape="circle"
                  src={user?.avatarUrl || undefined}
                  onError={(e) => {
                    e.currentTarget.src = "";
                  }}
                  style={{ fontWeight: 700 }}
                >
                  {firstInitial}
                </Avatar>
              </div>

              <div className="flex-1">
                <h1 className="text-3xl font-bold text-gray-900">
                  {user?.fullName || "Người dùng"}
                </h1>
                <p className="text-lg text-gray-600 mt-1">
                  {user?.email || "Chưa có email"}
                </p>
                <div className="flex items-center mt-2">
                  <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-green-100 text-green-800">
                    <span className="w-2 h-2 bg-green-400 rounded-full mr-2"></span>
                    Đang hoạt động
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Main Content */}
        <div className="space-y-6">
          {/* Personal Information (full width) */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200">
            <div className="px-6 py-4 border-b border-gray-200">
              <h2 className="text-xl font-semibold text-gray-900">
                Thông tin cá nhân
              </h2>
            </div>
            <div className="px-6 py-6">
              <dl className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                <div>
                  <dt className="text-sm font-medium text-gray-500">
                    Họ và tên
                  </dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {user?.fullName || "Chưa cập nhật"}
                  </dd>
                </div>
                <div>
                  <dt className="text-sm font-medium text-gray-500">Email</dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {user?.email || "Chưa có email"}
                  </dd>
                </div>
                <div>
                  <dt className="text-sm font-medium text-gray-500">
                    Số điện thoại
                  </dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {user?.phone || "Chưa cập nhật"}
                  </dd>
                </div>
                <div>
                  <dt className="text-sm font-medium text-gray-500">Địa chỉ</dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {user?.address || "Chưa cập nhật"}
                  </dd>
                </div>
                <div className="sm:col-span-2">
                  <dt className="text-sm font-medium text-gray-500">Vai trò</dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {keycloakUser?.realm_access?.roles?.find((role) =>
                      [
                        "admin",
                        "building-manager",
                        "receptionist",
                        "resident",
                        "security",
                      ].includes(role)
                    ) || "Chưa xác định"}
                  </dd>
                </div>
              </dl>
            </div>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Quick Actions */}
            <div className="bg-white rounded-lg shadow-sm border border-gray-200">
              <div className="px-6 py-4 border-b border-gray-200">
                <h3 className="text-lg font-semibold text-gray-900">
                  Thao tác nhanh
                </h3>
              </div>
              <div className="px-6 py-4 space-y-3">
                <button className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 rounded-md transition-colors">
                  Chỉnh sửa thông tin
                </button>
                <button className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 rounded-md transition-colors">
                  Đổi mật khẩu
                </button>
                <button className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 rounded-md transition-colors">
                  Cài đặt thông báo
                </button>
              </div>
            </div>

            {/* Account Status */}
            <div className="bg-white rounded-lg shadow-sm border border-gray-200">
              <div className="px-6 py-4 border-b border-gray-200">
                <h3 className="text-lg font-semibold text-gray-900">
                  Trạng thái tài khoản
                </h3>
              </div>
              <div className="px-6 py-4 space-y-3">
                <div className="flex items-center justify-between">
                  <span className="text-sm text-gray-600">Trạng thái</span>
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                    Hoạt động
                  </span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-gray-600">Xác thực</span>
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                    Đã xác thực
                  </span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-gray-600">
                    Lần đăng nhập cuối
                  </span>
                  <span className="text-xs text-gray-500">
                    {keycloakUser?.iat
                      ? new Date(keycloakUser.iat * 1000).toLocaleString(
                          "vi-VN"
                        )
                      : "Không xác định"}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default UserProfile;
