# Keycloak Integration Guide

## Tổng quan

Hệ thống đã được tích hợp với Keycloak để xử lý authentication và quản lý user data thông qua Zustand store.

## Các file đã được tạo/cập nhật

### 1. User Store (Zustand)

- **File**: `src/stores/userStore.js`
- **Chức năng**: Quản lý state của user data
- **API**:
  - `fetchUserData(userId)`: Lấy thông tin user từ API
  - `updateUserData(userId, userData)`: Cập nhật thông tin user
  - `setUser(userData)`: Set user data trực tiếp
  - `clearUser()`: Xóa user data

### 2. User API

- **File**: `src/features/user/userApi.js`
- **Chức năng**: API functions để tương tác với backend
- **Endpoints**:
  - `getUserById(userId)`: Lấy thông tin user theo ID
  - `updateUser(userId, userData)`: Cập nhật thông tin user
  - `getUsers(params)`: Lấy danh sách users

### 3. User Hook

- **File**: `src/hooks/useUser.js`
- **Chức năng**: Hook để sử dụng user store dễ dàng
- **Returns**:
  - `user`: User data object
  - `isLoading`: Loading state
  - `error`: Error message
  - `isAuthenticated`: Boolean check authentication
  - `userFullName`, `userEmail`, `userId`: Computed values

### 4. Keycloak Integration

- **File**: `src/keycloak/initKeycloak.js` (đã cập nhật)
- **Chức năng**:
  - Tự động lưu token vào localStorage
  - Gọi API để lấy thông tin user sau khi authenticate
  - Expose keycloak instance lên window object

### 5. API Client

- **File**: `src/lib/apiClient.js` (đã cập nhật)
- **Chức năng**:
  - Tự động thêm Bearer token vào requests
  - Tự động refresh token khi sắp hết hạn
  - Handle token expiration

## Cách sử dụng

### 1. Trong component React

```jsx
import React from "react";
import { useUser } from "../hooks/useUser";

const MyComponent = () => {
  const { user, isLoading, error, isAuthenticated } = useUser();

  if (isLoading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;
  if (!isAuthenticated) return <div>Please log in</div>;

  return (
    <div>
      <h1>Welcome, {user.fullName}!</h1>
      <p>Email: {user.email}</p>
    </div>
  );
};
```

### 2. Cập nhật thông tin user

```jsx
import { useUser } from "../hooks/useUser";

const UpdateProfile = () => {
  const { updateUserData, user } = useUser();

  const handleUpdate = async () => {
    try {
      await updateUserData(user.userId, {
        firstName: "New Name",
        phone: "0123456789",
      });
      console.log("User updated successfully");
    } catch (error) {
      console.error("Update failed:", error);
    }
  };

  return <button onClick={handleUpdate}>Update Profile</button>;
};
```

### 3. Sử dụng trực tiếp store

```jsx
import useUserStore from "../stores/userStore";

const MyComponent = () => {
  const { user, fetchUserData } = useUserStore();

  const handleRefresh = () => {
    fetchUserData("user-id-here");
  };

  return (
    <div>
      <p>User: {user?.fullName}</p>
      <button onClick={handleRefresh}>Refresh</button>
    </div>
  );
};
```

## User Data Structure

API endpoint `api/users/{id}` trả về data với cấu trúc:

```json
{
  "userId": "2c2b44b7-d3e8-4852-b612-5bd5b361c22c",
  "username": "kayoko",
  "email": "ngocqd2@gmail.com",
  "phone": "0338750792",
  "firstName": "Ngoc",
  "lastName": "Trinh",
  "fullName": "Trinh Ngoc",
  "dob": "03-01-2003",
  "address": "Quất Động - Thường Tín - Hà Nội"
}
```

## Token Management

- Token được tự động lưu vào `localStorage` với key `access_token`
- API client tự động thêm Bearer token vào mọi requests
- Token được tự động refresh khi sắp hết hạn (30 giây trước khi expire)
- Nếu refresh thất bại, user sẽ được logout tự động

## Environment Variables

Đảm bảo các biến môi trường sau được cấu hình:

```env
REACT_APP_API_BASE_URL=http://localhost:5000
REACT_APP_ENVIRONMENT=development
```

## Example Component

Xem file `src/components/UserProfile.jsx` để có ví dụ hoàn chỉnh về cách hiển thị thông tin user.
