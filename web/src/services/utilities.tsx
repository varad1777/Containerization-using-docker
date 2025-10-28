export const getRole = () => {
  return localStorage.getItem("role") || ""; // returns empty string if not set
};