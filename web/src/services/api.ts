import axios from "axios";

const RAW_BASE = (import.meta.env?.VITE_API_BASE_URL as string) || "http://localhost:5000/api";
const API_BASE_URL = RAW_BASE.replace(/\/$/, "");
// API base URL
// const API_BASE_URL = "https://localhost:7066/api";

// Create Axios instance
const api = axios.create({
  baseURL: API_BASE_URL,
  headers: { "Content-Type": "application/json" },
  withCredentials: true,
});

// Interceptor for 401
api.interceptors.response.use(
  response => response,
  async (error) => {
    const originalRequest = error.config;

    // Only handle 401 errors
    if (
      error.response?.status === 401 &&
      !originalRequest._retry &&
      !originalRequest.url.includes("/auth/refresh-token") &&
      !originalRequest.url.includes("/auth/login") &&
      !originalRequest.url.includes("/auth/register")
    ) {
      originalRequest._retry = true;

      try {
        // Call refresh token endpoint
        const refreshResponse = await api.post("/auth/refresh-token");

        if (refreshResponse.status === 200) {
          return api(originalRequest);
        }
      } catch (refreshError) {
        console.log("Refresh token failed:", refreshError);
        // Redirect to login page if refresh fails

        return Promise.reject(refreshError);
      }
    }

    // Reject all other errors normally
    return Promise.reject(error);
  }
);




// Helper function to handle API calls
const handleRequest = async (request: Promise<any>) => {
  try {

    console.log(API_BASE_URL)
    const { data, status } = await request;
    return { success: true, data, error: null, statusCode: status };
  } catch (err: any) {
    console.log(err)
    return {
      success: false,
      statusCode: err.response?.status || 500,
      data: err,
      error:
        err.response?.data?.[0]?.description ||
        err.response?.data?.errors ||
        err.response?.data?.message ||
        err.response?.data ||
        err.message ||
        "Something went wrong",
    };
  }
};


// small helper to build query strings for pagination/search
const buildQuery = (params: Record<string, any>) => {
  const qs = Object.entries(params)
    .filter(([, v]) => v !== undefined && v !== null && v !== "")
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(String(v))}`)
    .join("&");
  return qs ? `?${qs}` : "";
};

// ===== Auth API =====
export const authApi = {
  login: (credentials: { username: string; password: string }) =>
    handleRequest(api.post("/auth/login", credentials)),
  signup: (credentials: { username: string; password: string; role?: string }) =>
    handleRequest(api.post("/auth/register", credentials)),
  Logout: () =>
    handleRequest(api.post("/auth/logout")),
};

// ===== Assets API =====
export const assetsApi = {
  getAll: () => handleRequest(api.get("/assets")),
  getById: (id: string) => handleRequest(api.get(`/assets/${id}`)),
  create: (asset: any) => handleRequest(api.post("/assets", asset)),
  update: (id: string, asset: any) => handleRequest(api.put(`/assets/${id}`, asset)),
  delete: (id: string) => handleRequest(api.delete(`/assets/${id}`)),
};



// ---------------------------------------------------------------------------

// ===== Signals API (asset-scoped, paginated, searchable) =====
// Note: backend routes are expected to be:
// GET    /api/assets/{assetId}/signals?page=&pageSize=&search=
// GET    /api/assets/{assetId}/signals/{id}
// POST   /api/assets/{assetId}/signals
// PUT    /api/assets/{assetId}/signals/{id}
// DELETE /api/assets/{assetId}/signals/{id}
export const signalsApi = {
  /**
   * Get paged signals for a specific asset
   * @param assetId GUID of asset
   * @param options.page page number (1-based)
   * @param options.pageSize items per page
   * @param options.search optional search string (searches name+description)
   */
  getByAsset: (
    assetId: string,
    options: { page?: number; pageSize?: number; search?: string } = {}
  ) => {
    const { page = 1, pageSize = 10, search = "" } = options;
    const qs = buildQuery({ page, pageSize, search });
    return handleRequest(api.get(`/assets/${assetId}/signals${qs}`));
  },

  /**
   * Get single signal by id (scoped to asset)
   */
  getById: (assetId: string, signalId: number) =>
    handleRequest(api.get(`/assets/${assetId}/signals/${signalId}`)),

  /**
   * Create a signal for an asset
   * payload example: { name: string, description: string }
   */
  create: (assetId: any, payload: { name: string; description: string, Strength: number }) =>
    handleRequest(api.post(`/assets/${assetId}/signals`, payload)),

  /**
   * Update a signal by id (scoped to asset)
   * payload example: { id: number, name: string, description: string, assetId: string }
   */
  update: (assetId: any, signalId: number, payload: any) =>
    handleRequest(api.put(`/assets/${assetId}/signals/${signalId}`, payload)),

  /**
   * Delete a signal by id (scoped to asset)
   */
  delete: (assetId: any, signalId: number) =>
    handleRequest(api.delete(`/assets/${assetId}/signals/${signalId}`)),
};

export const BackgroundServiceApi = {
  getAvarage: (assetId: any) => handleRequest(api.post("/Averages", { ColumnName: "Strength", AssetId :assetId })),

};