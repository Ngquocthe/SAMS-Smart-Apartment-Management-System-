import useUserStore from "../stores/userStore";

export const useUser = () => {
  const {
    user,
    isLoading,
    error,
    setUser,
    setLoading,
    setError,
    clearUser,
    fetchUserData,
    updateUserData,
  } = useUserStore();

  return {
    // State
    user,
    isLoading,
    error,

    // Actions
    setUser,
    setLoading,
    setError,
    clearUser,
    fetchUserData,
    updateUserData,

    // Computed values
    isAuthenticated: !!user,
    userFullName: user?.fullName || "",
    userEmail: user?.email || "",
    userId: user?.userId || null,
  };
};

export default useUser;
