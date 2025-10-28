"use client";

import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  SheetClose,
} from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Bell, CheckCircle, Eye } from "lucide-react";
import { useState, useEffect } from "react";



export function RightDrawer({ notifications, open, onOpenChange,markAllRead , markAsRead  }: any) {
    const [localNotifications, setLocalNotifications] = useState<any>([]);


  useEffect(() => {
    setLocalNotifications(notifications);
  }, [notifications]);

  // Mark individual notification as read using RTC_API state
 const markAsReadmsg = async (id: string) => {
  try {
    // Call API from RTC_API
    await markAsRead(id);

    // Update local drawer state after API succeeds
    setLocalNotifications((prev:any) =>
      prev.map((n:any) => (n.id === id ? { ...n, isRead: true } : n))
    );

  } catch (err) {
    console.error("Failed to mark notification as read:", err);
  }
};


  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-96 flex flex-col h-full bg-white shadow-lg">
        {/* Header */}
        <SheetHeader className="border-b px-4 py-4 flex items-center justify-between">
          <div>
            <SheetTitle className="text-lg font-bold flex items-center gap-2">
              <Bell className="w-5 h-5 text-blue-500" />
              Notifications
            </SheetTitle>
            <SheetDescription className="text-sm text-gray-500">
              Here are your latest notifications.
            </SheetDescription>
          </div>
          <Button size="sm" variant="outline" onClick={markAllRead}>
            Mark All Read
          </Button>
        </SheetHeader>

        {/* Notifications list */}
        <div className="flex-1 overflow-y-auto px-4 py-2 space-y-3">
          {localNotifications.length === 0 ? (
            <p className="text-sm text-gray-500 text-center mt-4">No notifications</p>
          ) : (
            localNotifications.map((notif:any) => (
              <div
                key={notif.id}
                className={`
                  flex items-start gap-3 p-3 rounded-lg transition-all
                  border ${notif.isRead ? "border-gray-200 bg-gray-50 hover:bg-gray-100" : "border-red-200 bg-red-50 hover:bg-red-100"}
                  shadow-sm
                `}
              >
                {/* Icon */}
                <div className="mt-1">
                  {notif.isRead ? (
                    <CheckCircle className="w-5 h-5 text-gray-400" />
                  ) : (
                    <Bell className="w-5 h-5 text-blue-500 animate-pulse" />
                  )}
                </div>

                {/* Text */}
                <div className="flex-1">
                  <p className="text-sm font-semibold text-gray-800">{notif.createdBy}</p>
                  <p className="text-sm text-gray-600">{notif.message}</p>
                  {!notif.isRead && (
                    <div className="flex-shrink-0 mt-1 flex items-center gap-2">
                      <span className="px-3 py-1 bg-red-500 text-white text-xs font-semibold rounded-2xl">
                        NEW
                      </span>
                      <Eye
                        className="w-4 h-4 text-blue-500 cursor-pointer"
                        onClick={() => markAsReadmsg(notif.id)}
                      />
                    </div>
                  )}
                </div>
              </div>
            ))
          )}
        </div>

        {/* Close button */}
        <SheetClose asChild>
          <Button variant="outline" className="m-4">
            Close
          </Button>
        </SheetClose>
      </SheetContent>
    </Sheet>
  );
}
