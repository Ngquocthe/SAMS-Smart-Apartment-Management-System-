import { create } from "zustand";
import { userApi } from "../features/user/userApi";

const useUserStore = create((set, get) => ({
  // User data
  user: null,
  isLoading: false,
  error: null,

  // Actions
  setUser: (userData) => set({ user: userData, error: null }),
  setLoading: (loading) => set({ isLoading: loading }),
  setError: (error) => set({ error }),
  clearUser: () => set({ user: null, error: null }),

  // Fetch user data from API using userApi
  fetchUserData: async (userId) => {
    set({ isLoading: true, error: null });

    try {
      const userData = await userApi.getUserById(userId);
      set({ user: userData, isLoading: false, error: null });
      return userData;
    } catch (error) {
      console.error("Error fetching user data:", error);
      set({ error: error.message, isLoading: false });
      throw error;
    }
  },

  // Update user data
  updateUserData: async (userId, userData) => {
    set({ isLoading: true, error: null });

    try {
      const updatedUser = await userApi.updateUser(userId, userData);
      set({ user: updatedUser, isLoading: false, error: null });
      return updatedUser;
    } catch (error) {
      console.error("Error updating user data:", error);
      set({ error: error.message, isLoading: false });
      throw error;
    }
  },
}));

export default useUserStore;
