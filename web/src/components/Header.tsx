import { Bell, BellOff, LogOut, Plus, Server, User } from "lucide-react"
import { Button } from "./ui/button"
import { RightDrawer } from "./RightDrawer"
import { useRtc } from "@/services/RTC_API";
import { useEffect, useState, useRef } from "react";
import toast from "react-hot-toast";
import { authApi } from "@/services/api";
import { useNavigate } from "react-router-dom";

export function Header() {

    const [openRightDrawer, setOpenRightDrawer] = useState(false);
    let navigate = useNavigate();


    const {  notifications, unreadCount, markAllRead, markAsRead, isConnected } = useRtc();

    // We no longer register a second handler on the SignalR connection here because
    // the global `RtcProvider` already registers handlers and updates `notifications`.
    // Registering another handler caused duplicate notifications. Instead we show
    // a toast when `notifications` changes and a new notification appears.
        const latestNotificationIdRef = useRef<string | null>(null);
        // Skip toasting for the initial notifications load (so reloading the page doesn't show old notifications)
        const skipInitialRef = useRef(true);
        useEffect(() => {
            if (!notifications || notifications.length === 0) return;
            const latest = notifications[0];
            if (!latest) return;

            if (skipInitialRef.current) {
                // on first load, just record the id and don't show toast
                latestNotificationIdRef.current = latest.id;
                skipInitialRef.current = false;
                return;
            }

            if (latestNotificationIdRef.current !== latest.id) {
                // new notification arrived after initial load
                latestNotificationIdRef.current = latest.id;
                toast.custom(t => (
                    <div className={`${t.visible ? "animate-enter" : "animate-leave"} max-w-sm w-full bg-white shadow-lg rounded-xl pointer-events-auto flex ring-1 ring-black ring-opacity-5 overflow-hidden`}>
                        <div className="flex-shrink-0 flex items-center justify-center w-14 bg-blue-500">
                            <Bell className="w-6 h-6 text-white" />
                        </div>
                        <div className="flex-1 p-4">
                            <p className="text-md font-semibold text-gray-900 truncate">Notification</p>
                            <p className="mt-1 text-md text-gray-700 line-clamp-2">{latest.message}</p>
                        </div>
                        <div className="flex items-start justify-center pr-3 pt-3">
                            <button onClick={() => toast.dismiss(t.id)} className="text-gray-400 hover:text-gray-600 text-lg font-bold">×</button>
                        </div>
                    </div>
                ), { position: "bottom-right", duration: 3000 });
            }
        }, [notifications]);

    let SignOut = async () => {

        let loading = toast.loading("logging out...");
        try {
            let res = await authApi.Logout();
            if (res.success) {
                toast.dismiss(loading);
                navigate("/auth");
                return toast.success("logged out..");
            } else {
                toast.dismiss(loading);
                navigate("/auth");
                return toast.error(res.error || "failed to logged out..")
            }

        } catch (error) {
            toast.dismiss(loading);
            navigate("/auth");
            return toast.error("some thing went wrong,,")
        }


    }

    return <>

        <header className=" mx-auto mt-4 sticky top-0 left-0 right-0 w-[98%] shadow shadow-black rounded-2xl z-50">
            {/* The drawer itself */}
            <RightDrawer notifications={notifications} open={openRightDrawer} onOpenChange={setOpenRightDrawer} markAllRead={markAllRead} markAsRead={markAsRead} />

            <div className="container mx-auto px-3 py-4 group relative overflow-hidden rounded-xl border border-border/40 bg-gradient-to-r from-card via-card to-card/80 backdrop-blur-sm transition-all duration-300 hover:border-primary/40">
                <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                    <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 w-full">
                        {/* Left Section - Title and Icon */}
                        <div className="flex items-center gap-3">
                            <div className="flex h-10 w-10 sm:h-12 sm:w-12 items-center justify-center rounded-xl bg-gradient-to-br from-primary to-secondary p-[2px] shadow-lg shadow-primary/20">
                                <div className="flex h-full w-full items-center justify-center rounded-[10px] bg-background shadow-black">
                                    <Server className="h-5 w-5 sm:h-6 sm:w-6 text-primary" />
                                </div>
                            </div>
                            <div>
                                <h1 className="text-2xl sm:text-3xl font-bold bg-gradient-to-r from-primary via-primary to-secondary bg-clip-text">
                                    Device Management
                                </h1>
                                <p className="text-xs sm:text-sm text-muted-foreground">
                                    Manage your devices and signals
                                </p>
                            </div>
                        </div>

                        {/* Right Section - Actions */}
                        <div className="flex flex-wrap items-center gap-3 sm:gap-4">
                            {/* Notification Bell */}
                            <p
                                onClick={() => setOpenRightDrawer(true)}
                                className={`
                    flex items-center justify-center relative
                    w-8 h-8 p-[2px] rounded-full cursor-pointer
                    transition-colors duration-300
                    ${isConnected ? "bg-green-500 hover:bg-green-600" : "bg-red-500 hover:bg-red-600"}
                  `}
                            >
                                {unreadCount > 0 && (
                                    <span className="text-white absolute -top-3 -right-[10px] bg-red-500 px-1.5 py-0.4 rounded-full text-[12px]">
                                        {unreadCount}
                                    </span>
                                )}
                                {isConnected ? (
                                    <Bell className="w-8 h-8 text-white p-[2px]" />
                                ) : (
                                    <BellOff className="w-8 h-8 text-white p-[2px]" />
                                )}
                            </p>

                            {/* User Info */}
                            <p className="cursor-pointer text-right w-fit rounded-2xl hidden sm:flex items-center gap-2">
                                <User className="inline bg-green-500 p-[2px] hover:bg-green-600 h-8 w-8 text-white rounded-full" />
                                <span className="hidden md:inline">{localStorage.getItem("username")}</span>
                            </p>

                            {/* Sign Out Button */}
                            <Button
                                onClick={SignOut}
                                className="cursor-pointer bg-red-500 hover:bg-red-600 shadow-lg shadow-primary/20"
                                size="sm"
                            >
                                <LogOut className="mr-0 sm:mr-2 h-4 w-4" />
                                <span className="hidden sm:inline">Sign Out</span>
                            </Button>

                            {/* Add Device Button */}
                            {/* <Button
                                onClick={handleCreate}
                                className="bg-primary cursor-pointer text-primary-foreground hover:bg-primary/90 shadow-lg shadow-primary/20"
                                size="sm"
                            >
                                <Plus className="mr-0 sm:mr-2 h-4 w-4" />
                                <span className="hidden sm:inline">Add Device</span>
                            </Button> */}
                        </div>
                    </div>


                </div>

            </div>

        </header>
    </>
}