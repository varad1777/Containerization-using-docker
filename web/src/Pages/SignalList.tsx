// SignalPage.tsx
import { useCallback, useEffect, useState } from "react";
import { Plus, Edit2, Trash2, ChevronLeft, ChevronRight, Search, Activity, AlertTriangle, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { BackgroundServiceApi, signalsApi } from "@/services/api"; // path to your api file
import toast from "react-hot-toast";
import { useNavigate, useParams } from "react-router-dom";
import debounce from "lodash.debounce";
import { Textarea } from "@/components/ui/textarea";
import { getRole } from "@/services/utilities";
import { Slider } from "@/components/ui/slider";
import { SliderRange, SliderThumb, SliderTrack } from "@radix-ui/react-slider";
import { useRtc } from "@/services/RTC_API";






// --- Types
type SignalDto = {
    id: number;
    name: string;
    description: string;
    strength: number;
    assetId: string;
};

type PagedResult<T> = {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
};

// --- Props
export default function SignalList() {
    const [search, setSearch] = useState("");
    const [page, setPage] = useState(1);
    const [pageSize] = useState(5);
    const [loading, setLoading] = useState(false);
    const [Strength, SetStrength] = useState(50)

    const [paged, setPaged] = useState<PagedResult<SignalDto>>({
        items: [],
        totalCount: 0,
        page: 1,
        pageSize,
        totalPages: 0,
    });


    // Dialog states
    const [openCreate, setOpenCreate] = useState(false);
    const [editing, setEditing] = useState<SignalDto | null>(null);
    const [confirmDeleteId, setConfirmDeleteId] = useState<number | null>(null);

    // Form state (create/edit)
    const [formName, setFormName] = useState("");
    const [formDesc, setFormDesc] = useState("");
    const [formErrors, setFormErrors] = useState<any>({});

    const { assetId } = useParams<{ assetId: string }>();
    let navigate = useNavigate();
    const [averageLocal, setAverageLocal] = useState<string | null>(null);

    const {  average } = useRtc();

    console.log(average)




    // useEffect(() => {
    //     if (!connection) return;
    //     // Use a named handler so we can clean it up
    //     const avgHandler = (id: string, createdBy: string, message: string, createdAt: string) => {
    //         console.log("SignalList ReceivedAvarage event:", { id, createdBy, message, createdAt });
    //         setAverageLocal(message);
    //     };

    //     // ensure no duplicate registration
    //     connection.off("ReceivedAvarage", avgHandler);
    //     connection.on("ReceivedAvarage", avgHandler);

    //     return () => {
    //         connection.off("ReceivedAvarage", avgHandler);
    //     };
    // }, [connection]);

    // If context average updates, reflect in local UI too
    useEffect(() => {
        if (average) {
            setAverageLocal(average)
        } else {
            setAverageLocal(null)
        };
    }, [assetId, average]);

    // Fetch function
    const fetchSignals = async (p = page, q = search, assetId: any) => {
        setLoading(true);
        try {
            const resp = await signalsApi.getByAsset(assetId, { page: p, pageSize, search: q });
            if (!resp.success) throw new Error(resp.error || "Failed to fetch signals");
            // resp.data is the server payload (PagedResult)
            const payload = resp.data;
            // tolerate naming differences (items vs Items)
            const items = payload.items ?? payload.Items ?? payload.Items ?? [];
            const totalCount = payload.totalCount ?? payload.TotalCount ?? 0;
            const curPage = payload.page ?? payload.Page ?? p;
            const curPageSize = payload.pageSize ?? payload.PageSize ?? pageSize;
            const totalPages = payload.totalPages ?? payload.TotalPages ?? Math.max(1, Math.ceil(totalCount / curPageSize));

            setPaged({
                items,
                totalCount,
                page: curPage,
                pageSize: curPageSize,
                totalPages,
            });
            setPage(curPage);
        } catch (err: any) {
            console.error(err);
            // optional toast
            toast.error(err?.message ?? "Failed to load signals");
            return navigate("/")

        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        getAvarage(assetId)
        fetchSignals(1, search, assetId);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [assetId]);



    // Pagination handlers
    const gotoPrev = () => {
        if (paged.page > 1) fetchSignals(paged.page - 1, search, assetId);
    };
    const gotoNext = () => {
        if (paged.page < paged.totalPages) fetchSignals(paged.page + 1, search, assetId);
    };

    // Create / Edit handlers
    const openCreateDialog = () => {
        setEditing(null);
        setFormName("");
        setFormDesc("");
        setOpenCreate(true);
    };

    const openEditDialog = (s: SignalDto) => {
        setEditing(s);
        setFormName(s.name);
        setFormDesc(s.description);
        SetStrength(s.strength);
        setOpenCreate(true);
    };

    const handleSave = async () => {
        if (!formName.trim()) {
            toast.error("Name is required");
            return;
        }

        try {

            let resp;
            if (editing) {
                // update
                const payload = { id: editing.id, name: formName, description: formDesc, Strength, assetId };
                resp = await signalsApi.update(assetId, editing.id, payload);

            } else {
                // create
                resp = await signalsApi.create(assetId, { name: formName, description: formDesc, Strength });
            }

            if (resp.statusCode == 401) {
                toast.error("Un-Authorised access...");
                return navigate("/auth");
            }
            if (resp.statusCode == 403) {
                toast.error("You dont have permission to access this...");
                return navigate("/auth");
            }

            if (resp.success) {
                toast.success(editing ? "Signal Updated Successfully" : "Signal Created Successfully");
                setOpenCreate(false);
                setFormErrors({});
                getAvarage(assetId)
                fetchSignals(editing ? page : 1, search, assetId);
            } else {
                // API returned error, save in state
                setFormErrors(resp.error);
                toast.error("Failed to save Signal");
            }


        } catch (err: any) {
            console.error(err);
            toast.error("Failed to save");
        }
    };

    // Delete
    const confirmDelete = (id: number) => setConfirmDeleteId(id);
    const doDelete = async () => {
        if (confirmDeleteId == null) return;
        try {
            const resp = await signalsApi.delete(assetId, confirmDeleteId);
            console.log(resp);

            if (!resp.success) {
                if (resp.statusCode == 403) {
                    setConfirmDeleteId(null);
                    return toast.error("you don't have permission to perform this action.");
                }
                setConfirmDeleteId(null);
                throw new Error(resp.error || "Delete failed");
            }
            toast.success("Signal deleted");
            // recalc page if last item removed
            const nextTotal = paged.totalCount - 1;
            const lastPage = Math.max(1, Math.ceil(nextTotal / pageSize));
            const newPage = page > lastPage ? lastPage : page;
            setConfirmDeleteId(null);
            getAvarage(assetId)
            fetchSignals(newPage, search, assetId);
        } catch (err: any) {
            console.error(err);
            toast.error("Failed to delete");
        }
    };


    const debouncedSearch = useCallback(
        debounce((query: string) => {
            fetchSignals(1, query, assetId); // page 1 for every new search
        }, 800), // 500ms debounce
        []
    );

    let getAvarage = async (assetId: any) => {
        let res = await BackgroundServiceApi.getAvarage(assetId)
        if (!res?.success) {
            toast.error(res.error || "Failed to get avarage, please after 2 sec.")
        }
    }

    useEffect(() => {
        debouncedSearch(search);

        // cancel debounce on unmount
        return () => {
            debouncedSearch.cancel();
        };
    }, [search, debouncedSearch]);

    return (



        <div className="space-y-4 px-4 py-4">

            {/* Results Card */}
            <div className="shadow shadow-black rounded-2xl">
                <div className="py-3 px-4">
                    {/* Header with Results Count */}
                    <div className="flex items-center justify-between mb-4 pb-3 border-b border-border/50">
                        <div className="flex items-center gap-3">

                            <div>
                                <Label className=" text-2xl font-semibold">Signal Results</Label>
                                <div className="text-md  text-muted-foreground mt-0.5">
                                    {paged.totalCount} total • Page {paged.page} of {paged.totalPages}
                                </div>
                            </div>
                        </div>


                        <div className="flex items-center gap-1">
                            <div
                                className="px-4 py-2 rounded-xl 
             bg-white/5 backdrop-blur-sm 
             border border-white/10 
              text-sm font-semibold 
             shadow-inner shadow-primary/10 
             flex items-center justify-center 
             transition-all duration-300"
                            >
                                Average: {averageLocal ?? "Loading..."}
                            </div>
                            <div className="relative flex-1 w-full">
                                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                                <Input 
                                    placeholder="Search signals by name or description..."
                                    value={search}
                                    onChange={(e) => setSearch(e.target.value)}
                                    className="pl-10 border-border/50 w-[20rem]
                               bg-background/50 focus:border-primary/50 transition-colors"
                                />
                            </div>


                            <div className="flex items-center gap-2">
                                <Button
                                    variant="outline"
                                    className="border-border/50 hover:bg-accent transition-all duration-300"
                                    onClick={() => setSearch("")}
                                >
                                    <X className="mr-2 h-4 w-4" />
                                    Clear
                                </Button>
                                <Button
                                    onClick={openCreateDialog}
                                    className="bg-gradient-to-r from-primary to-primary/90 shadow-md shadow-primary/20 hover:shadow-lg hover:shadow-primary/30 transition-all duration-300"
                                >
                                    <Plus className="mr-2 h-4 w-4" />
                                    New Signal
                                </Button>



                            </div>
                        </div>
                    </div>

                    {/* Signal List */}
                    <div className="space-y-2">
                        {loading ? (
                            <div className="flex flex-col items-center justify-center py-10">
                                <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary/30 border-t-primary" />
                                <p className="mt-3 text-sm text-muted-foreground">Loading signals...</p>
                            </div>
                        ) : paged.items.length === 0 ? (
                            <div className="flex flex-col items-center justify-center py-12 text-center">
                                <div className="flex h-16 w-16 items-center justify-center rounded-full bg-muted/50 mb-3">
                                    <Activity className="h-8 w-8 text-muted-foreground" />
                                </div>
                                <p className="text-sm font-medium text-foreground">No signals found</p>
                                <p className="text-xs text-muted-foreground mt-1">Try adjusting your search criteria</p>
                            </div>
                        ) : (
                            paged.items.map((s) => (
                                <div
                                    key={s.id}
                                    className="group relative overflow-hidden rounded-lg border border-border/40 transition-all duration-300 hover:border-primary/40 "
                                >
                                    <div className="absolute left-0 top-0 bottom-0 w-1 bg-gradient-to-b from-primary to-secondary opacity-0 transition-opacity duration-300 group-hover:opacity-100" />

                                    <div className="relative flex items-center justify-between p-4">
                                        <div className="flex items-start gap-3 flex-1 min-w-0">
                                            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg text-white bg-black transition-all duration-300 group-hover:scale-110 ">
                                                <Activity className="h-5 w-5" />
                                            </div>
                                            <div className="grid grid-cols-12 gap-4 items-start ml-4">
                                                {/* Name & description */}
                                                <div className="col-span-12">
                                                    <div className="font-semibold text-foreground group-hover:text-primary transition-colors">
                                                        {s.name}
                                                    </div>
                                                    <div className="text-sm text-muted-foreground line-clamp-2 mt-0.5">
                                                        {s.description}
                                                    </div>
                                                </div>

                                                {/* Strength */}
                                                <div className="text-sm text-muted-foreground line-clamp-1 col-span-2">
                                                    <strong>Strength:-</strong> {s.strength}
                                                </div>
                                            </div>

                                        </div>

                                        <div className="flex items-center gap-2 shrink-0 ml-4">
                                            <Button
                                                size="icon"
                                                variant="outline"
                                                className="h-9 w-9 border-black hover:bg-white hover:text-black cursor-pointer hover:scale-110 transition-all duration-300"
                                                onClick={() => openEditDialog(s)}
                                                title="Edit Signal"
                                            >
                                                <Edit2 className="h-4 w-4" />
                                            </Button>
                                            {getRole() === "Admin" && (
                                                <Button
                                                    size="icon"
                                                    variant="outline"
                                                    className="h-9 w-9 border-red-500 bg-background/50 text-red-500 transition-all duration-200 hover:bg-white hover:text-red-500 cursor-pointer hover:scale-110"
                                                    onClick={() => confirmDelete(s.id)}
                                                    title="Delete Signal"
                                                >
                                                    <Trash2 className="h-4 w-4" />
                                                </Button>
                                            )}
                                        </div>
                                    </div>
                                </div>
                            ))
                        )}
                    </div>

                    {/* Pagination Footer */}
                    {paged.items.length > 0 && (
                        <div className="mt-4 pt-4 border-t border-border/50 flex items-center justify-between">
                            <div className="text-sm text-muted-foreground">
                                Showing <span className="font-medium text-foreground">{(paged.page - 1) * paged.pageSize + 1}</span> - <span className="font-medium text-foreground">{(paged.page - 1) * paged.pageSize + paged.items.length}</span> of <span className="font-medium text-foreground">{paged.totalCount}</span>
                            </div>


                            <div className="flex items-center gap-2">
                                <Button
                                    size="sm"
                                    variant="outline"
                                    onClick={() => fetchSignals(1, search, assetId)}
                                    disabled={paged.page === 1}
                                    className="border-border/50 hover:bg-accent disabled:opacity-50"
                                >
                                    First
                                </Button>
                                <Button
                                    size="sm"
                                    variant="outline"
                                    onClick={gotoPrev}
                                    disabled={paged.page === 1}
                                    className="border-border/50 hover:bg-accent disabled:opacity-50"
                                >
                                    <ChevronLeft className="mr-1 h-3 w-3" />
                                    Prev
                                </Button>
                                <div className="px-3 py-1.5 text-sm font-medium border border-primary/50 bg-primary/10 text-primary rounded">
                                    {paged.page}
                                </div>
                                <Button
                                    size="sm"
                                    variant="outline"
                                    onClick={gotoNext}
                                    disabled={paged.page === paged.totalPages}
                                    className="border-border/50 hover:bg-accent disabled:opacity-50"
                                >
                                    Next
                                    <ChevronRight className="ml-1 h-3 w-3" />
                                </Button>
                                <Button
                                    size="sm"
                                    variant="outline"
                                    onClick={() => fetchSignals(paged.totalPages, search, assetId)}
                                    disabled={paged.page === paged.totalPages}
                                    className="border-border/50 hover:bg-accent disabled:opacity-50"
                                >
                                    Last
                                </Button>
                            </div>
                        </div>
                    )}
                </div>
            </div>

            {/* Create / Edit Dialog */}
            <Dialog open={openCreate} onOpenChange={setOpenCreate}>
                <DialogContent className="sm:max-w-[500px]">
                    <DialogHeader>
                        <DialogTitle className="flex items-center gap-2 text-xl">
                            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
                                <Activity className="h-4 w-4" />
                            </div>
                            {editing ? "Edit Signal" : "Create New Signal"}
                        </DialogTitle>
                    </DialogHeader>

                    <div className="space-y-4 py-2">
                        <div className="space-y-2">
                            <Label className="text-sm font-medium">Signal Name <span className="text-red-500">*</span></Label>
                            <Input 
                                value={formName}
                                onChange={(e) => setFormName(e.target.value)}
                                maxLength={15}
                                placeholder="e.g., Temperature Sensor"
                                className="border-border/50 bg-background/50 focus:border-primary/50 transition-colors"
                            />
                            <p className="text-xs text-muted-foreground">{formName.length}/15 characters</p>
                            {formErrors?.Name && Array.isArray(formErrors.Name) && (
                                <div className="space-y-1">
                                    {formErrors.Name.map((err: string, idx: number) => (
                                        <p key={idx} className="text-xs text-red-500 flex items-center gap-1">
                                            <AlertTriangle className="h-3 w-3" />
                                            {err}
                                        </p>
                                    ))}
                                </div>
                            )}
                        </div>

                        <div className="space-y-2">
                            <Label className="text-sm font-medium">Description <span className="text-red-500">*</span></Label>
                            <Textarea
                                value={formDesc}
                                onChange={(e) => setFormDesc(e.target.value)}
                                className="min-h-[100px] border-border/50 bg-background/50 focus:border-primary/50 transition-colors resize-none"
                                placeholder="Describe the signal's purpose and functionality..."
                                maxLength={150}
                            />
                            <p className="text-xs text-muted-foreground">{formDesc.length}/150 characters</p>
                            {formErrors?.Description && Array.isArray(formErrors.Description) && (
                                <div className="space-y-1">
                                    {formErrors.Description.map((err: string, idx: number) => (
                                        <p key={idx} className="text-xs text-red-500 flex items-center gap-1">
                                            <AlertTriangle className="h-3 w-3" />
                                            {err}
                                        </p>
                                    ))}
                                </div>
                            )}
                        </div>
                        <div className="space-y-2">
                            <Label className="text-sm font-medium">Select Strength</Label>
                            <Slider
                                value={[Strength]}        // current value
                                min={1}                // minimum value
                                max={100}              // maximum value
                                step={1}               // increments
                                onValueChange={(val) => SetStrength(val[0])} // update state
                            >
                                <SliderTrack>
                                    <SliderRange />
                                </SliderTrack>
                                <SliderThumb />
                            </Slider>
                            <p className="mt-2 text-center">Value: {Strength}</p>
                        </div>

                        <div className="flex justify-end gap-2 pt-2">
                            <Button
                                variant="outline"
                                onClick={() => setOpenCreate(false)}
                                className="border-border/50"
                            >
                                Cancel
                            </Button>
                            <Button
                                onClick={handleSave}
                                className="bg-gradient-to-r from-primary to-primary/90 shadow-md shadow-primary/20"
                            >
                                {editing ? "Save Changes" : "Create Signal"}
                            </Button>
                        </div>
                    </div>
                </DialogContent>
            </Dialog>

            {/* Delete Confirmation Modal */}
            {confirmDeleteId !== null && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm animate-in fade-in duration-200">
                    <div className="bg-card border border-border/40 p-6 rounded-xl shadow-2xl w-full max-w-md animate-in zoom-in-95 duration-200">
                        <div className="flex items-center gap-3 mb-4">
                            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-red-500/10 text-red-500">
                                <AlertTriangle className="h-6 w-6" />
                            </div>
                            <div>
                                <div className="font-semibold text-lg">Confirm Deletion</div>
                                <div className="text-sm text-muted-foreground">This action cannot be undone</div>
                            </div>
                        </div>
                        <div className="mb-6 text-sm text-muted-foreground">
                            Are you sure you want to permanently delete this signal? All associated data will be lost.
                        </div>
                        <div className="flex justify-end gap-2">
                            <Button
                                variant="outline"
                                onClick={() => setConfirmDeleteId(null)}
                                className="border-border/50"
                            >
                                Cancel
                            </Button>
                            <Button
                                variant="destructive"
                                onClick={doDelete}
                                className="bg-red-500 hover:bg-red-600 shadow-md shadow-red-500/20"
                            >
                                <Trash2 className="mr-2 h-4 w-4" />
                                Delete Signal
                            </Button>
                        </div>
                    </div>
                </div>
            )}
        </div>

    );
}
