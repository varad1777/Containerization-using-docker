// src/rtc/RtcContext.tsx
import React, { createContext, useContext, useEffect, useMemo, useState } from "react";
import * as signalR from "@microsoft/signalr";
import axios from "axios";

export type Notification = {
  id: string;
  createdBy: string;
  message: string;
  createdAt: string;
  isRead: boolean;
};

type RtcContextValue = {
  connection: signalR.HubConnection | null;
  isConnected: boolean;
  notifications: Notification[];
  setNotifications: (n: Notification[] | ((prev: Notification[]) => Notification[])) => void;
  unreadCount: number;
  markAllRead: () => Promise<void>;
  markAsRead: (id: string) => Promise<void>;
  average: string | null;
};

const RtcContext = createContext<RtcContextValue | undefined>(undefined);

export const RtcProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [average, setAverage] = useState<string | null>(null);

  const RAW_BASE = (import.meta.env?.VITE_API_BASE_URL as string) || "http://localhost:5000/api";
  const API_BASE_URL = RAW_BASE.replace(/\/$/, "");
  // hubURL points to the hub endpoint (no /api)
  const hubUrl = API_BASE_URL.replace(/\/api$/, "") + "/notificationHub";

  const unreadCount = notifications.filter(n => !n.isRead).length;

  useEffect(() => {
    // fetch existing notifications once
    axios.get(`${API_BASE_URL}/notifications`, { withCredentials: true })
      .then(res => setNotifications(Array.isArray(res.data) ? res.data : []))
      .catch(err => console.error("Failed to fetch notifications:", err));

    // create single connection instance
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { withCredentials: true }) // use cookie auth
      .withAutomaticReconnect()
      .build();

    // global handlers
    const notificationHandler = (id: string, createdBy: string, message: string, createdAt: string) => {
      setNotifications(prev => [{ id, createdBy, message, createdAt, isRead: false }, ...prev]);
    };
    const averageHandler = (_id: string, _createdBy: string, message: string) => {
      setAverage(message);
    };

    // register handlers BEFORE start to avoid race conditions
    conn.on("ReceiveNotification", notificationHandler);
    conn.on("ReceivedAvarage", averageHandler);

    setConnection(conn);

    conn.start()
      .then(() => setIsConnected(true))
      .catch(err => console.error("SignalR start error:", err));

    conn.onclose(() => setIsConnected(false));
    conn.onreconnected(() => setIsConnected(true));

    return () => {
      try {
        conn.off("ReceiveNotification", notificationHandler);
        conn.off("ReceivedAvarage", averageHandler);
        conn.stop().catch(() => {});
      } catch (e) {
        // ignore cleanup errors
      }
      setConnection(null);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // run once

  const markAllRead = async () => {
    try {
      await axios.put(`${API_BASE_URL}/notifications/mark-all-read`, {}, { withCredentials: true });
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
    } catch (err) {
      console.error("Failed to mark all read:", err);
    }
  };

  const markAsRead = async (id: string) => {
    try {
      await axios.put(`${API_BASE_URL}/notifications/mark-read/${id}`, {}, { withCredentials: true });
      setNotifications(prev => prev.map(n => (n.id === id ? { ...n, isRead: true } : n)));
    } catch (err) {
      console.error("Failed to mark read:", err);
    }
  };

  const value = useMemo(() => ({
    connection,
    isConnected,
    notifications,
    setNotifications,
    unreadCount,
    markAllRead,
    markAsRead,
    average,
  }), [connection, isConnected, notifications, average, unreadCount]);

  return <RtcContext.Provider value={value}>{children}</RtcContext.Provider>;
};

export const useRtc = () => {
  const ctx = useContext(RtcContext);
  if (!ctx) throw new Error("useRtc must be used inside RtcProvider");
  return ctx;
};
