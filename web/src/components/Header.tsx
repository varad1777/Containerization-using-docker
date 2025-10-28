import { Bell, BellOff, LogOut, Plus, Server, User } from "lucide-react"
import { Button } from "./ui/button"
import { RightDrawer } from "./RightDrawer"
import { RTC_API } from "@/services/RTC_API";
import { useState } from "react";
import toast from "react-hot-toast";
import { authApi } from "@/services/api";
import { useNavigate } from "react-router-dom";

export function Header() {

    const [openRightDrawer, setOpenRightDrawer] = useState(false);
    const { notifications, isConnected, unreadCount, markAllRead, markAsRead } = RTC_API();
    let navigate = useNavigate();

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
            <RightDrawer notifications={notifications} open={openRightDrawer} onOpenChange={setOpenRightDrawer}  markAllRead={markAllRead} markAsRead={markAsRead }  />

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
                                onClick={() =>  setOpenRightDrawer(true) }
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