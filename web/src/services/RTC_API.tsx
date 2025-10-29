import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import toast from "react-hot-toast";
import { Bell } from "lucide-react";
import axios from "axios";

export interface Notification {
  id: string;
  createdBy: string;
  message: string;
  createdAt: string;
  isRead: boolean;
}

export const RTC_API = (jwtToken?: string) => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [isConnected, setIsConnected] = useState(false);

  // Unread count derived from notifications
  const unreadCount = Array.isArray(notifications)
    ? notifications.filter(n => !n.isRead).length
    : 0;

  // Audio setup
  let audioContext: AudioContext | null = null;
  const getAudioContext = () => {
    if (!audioContext) {
      audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
      const resume = () => {
        if (audioContext && audioContext.state === "suspended") {
          audioContext.resume();
        }
        window.removeEventListener("click", resume);
        window.removeEventListener("keydown", resume);
      };
      window.addEventListener("click", resume);
      window.addEventListener("keydown", resume);
    }
    return audioContext;
  };
  const playSoftNotification = () => {
    const ctx = getAudioContext();
    const oscillator = ctx.createOscillator();
    oscillator.type = "triangle";
    oscillator.frequency.setValueAtTime(880, ctx.currentTime);

    const gainNode = ctx.createGain();
    gainNode.gain.setValueAtTime(0.1, ctx.currentTime);
    gainNode.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.3);

    oscillator.connect(gainNode);
    gainNode.connect(ctx.destination);
    oscillator.start();
    oscillator.stop(ctx.currentTime + 0.3);
  };

  // Mark all notifications as read
  const markAllRead = async () => {
    try {
      await axios.put("http://localhost:5000/api/notifications/mark-all-read", {},
        {
          withCredentials: true,
          headers: { "Content-Type": "application/json" }
        });
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
    } catch (err) {
      console.error("Failed to mark notifications as read:", err);
    }
  };

  // Mark a single notification as read
  const markAsRead = async (id: string) => {
    try {
      await axios.put(`http://localhost:5000/api/notifications/mark-read/${id}`, {}, {
        withCredentials: true,
        headers: { "Content-Type": "application/json" }
      });
      setNotifications(prev =>
        prev.map(n => (n.id === id ? { ...n, isRead: true } : n))
      );
    } catch (err) {
      console.error("Failed to mark notification as read:", err);
      toast.error("Failed to mark notification as read");
    }
  };

  useEffect(() => {
    // Fetch existing notifications on login
    axios.get("http://localhost:5000/api/notifications", {
      withCredentials: true
    })
      .then(res => setNotifications(res.data))
      .catch(err => console.error("Failed to fetch notifications:", err));

    // Connect to SignalR hub
    const conn = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5000/notificationHub", {
        accessTokenFactory: () => jwtToken || ""
      })
      .withAutomaticReconnect()
      .build();

    setConnection(conn);

    


    // Listen to broadcast average notifications
    conn.on("ReceiveNotification", (id: string, createdBy: string, message: string, createdAt: string) => {
      playSoftNotification();
      const notification: Notification = { id, createdBy, message, createdAt, isRead: false };
      setNotifications(prev => [notification, ...prev]);

      toast.custom(
        t => (
          <div className={`${t.visible ? "animate-enter" : "animate-leave"} max-w-sm w-full bg-white shadow-lg rounded-xl pointer-events-auto flex ring-1 ring-black ring-opacity-5 overflow-hidden`}>
            <div className="flex-shrink-0 flex items-center justify-center w-14 bg-blue-500">
              <Bell className="w-6 h-6 text-white" />
            </div>
            <div className="flex-1 p-4">
              <p className="text-md font-semibold text-gray-900 truncate">Notification</p>
              <p className="mt-1 text-md text-gray-700 line-clamp-2">{message}</p>
            </div>
            <div className="flex items-start justify-center pr-3 pt-3">
              <button onClick={() => toast.dismiss(t.id)} className="text-gray-400 hover:text-gray-600 text-lg font-bold">×</button>
            </div>
          </div>
        ),
        { position: "bottom-right", duration: 3000 }
      );
    });


    conn.start()
      .then(() => setIsConnected(true))
      .catch(err => console.error("SignalR Connection Error:", err));

    conn.onclose(() => setIsConnected(false));
    conn.onreconnected(() => setIsConnected(true));
   

    return () => {
      conn.stop();
    };
  }, [jwtToken]);

  return { connection, notifications, setNotifications, isConnected, unreadCount, markAllRead, markAsRead };
};
