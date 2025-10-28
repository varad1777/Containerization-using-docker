// import { useEffect, useState } from "react";
// import { Plus, X } from "lucide-react";
// import {
//     Dialog,
//     DialogContent,
//     DialogDescription,
//     DialogFooter,
//     DialogHeader,
//     DialogTitle,
// } from "./ui/dialog";

// import { Button } from "@/components/ui/button";
// import { Input } from "@/components/ui/input";
// import { Label } from "@/components/ui/label";
// import { Textarea } from "@/components/ui/textarea";



// export function DeviceDialog({ open, onOpenChange, asset, onSave, formErrors, setFormErrors }: any) {
//     const [name, setName] = useState<any>("");
//     const [description, setDescription] = useState<any>("");


//     useEffect(() => {
//         if (asset) {
//             setName(asset.name);
//             setDescription(asset.description);
//         } else {
//             setName("");
//             setDescription("");
//             setFormErrors([]);
//         }
//     }, [asset, open]);






//     const handleSave = () => {
//         if (!name.trim()) return;

//         onSave({
//             name: name.trim(),
//             description: description.trim()
//         });



//     };

//     return (
//         <Dialog open={open} onOpenChange={onOpenChange}>
//             <DialogContent className="max-w-2xl border-border/50 bg-card/95 backdrop-blur-xl max-h-[90vh] overflow-y-auto">
//                 <DialogHeader>
//                     <DialogTitle className="text-2xl text-foreground">
//                         {asset ? "Edit Device" : "Create New Device"}
//                     </DialogTitle>
//                     <h3 className="text-muted-foreground">
//                         {asset ? "Update device information and signals" : "Add a new device with its signals"}
//                     </h3>
//                 </DialogHeader>

//                 <div className="space-y-6 py-4">
//                     {/* Device Info */}
//                     <div className="space-y-4">
//                         <div className="space-y-2">
//                             <Label htmlFor="name" className="text-foreground">
//                                 Device Name <span className="text-destructive">*</span>
//                             </Label>

//                             <Input
//                                 id="name"
//                                 value={name}
//                                 onChange={(e: any) => setName(e.target.value)}
//                                 placeholder="Enter device name"
//                                 maxLength={15}
//                                 className="border-border/50 bg-background/50"
//                             />
//                             <p className="text-xs text-muted-foreground">{name.length}/15 characters</p>
//                             {formErrors?.Name && <Label htmlFor="name" className="text-[#FF0000]">
//                                 <span className="text-destructive">{formErrors.Name}</span>
//                             </Label>}
//                         </div>

//                         <div className="space-y-2">
//                             <Label htmlFor="description" className="text-foreground">
//                                 Description <span className="text-destructive">*</span>
//                             </Label>
//                             <Textarea
//                                 id="description"
//                                 value={description}
//                                 onChange={(e: any) => setDescription(e.target.value)}
//                                 placeholder="Enter device description"
//                                 maxLength={150}
//                                 rows={3}
//                                 className="border-border/50 bg-background/50 resize-none"
//                             />
//                             <p className="text-xs text-muted-foreground">{description.length}/150 characters</p>
//                             {formErrors?.Description && <Label htmlFor="name" className="text-[#FF0000]">
//                                 <span className="text-destructive">{formErrors.Description}</span>
//                             </Label>}
//                         </div>
//                     </div>

                    
//                 </div>

//                 <DialogFooter>
//                     <Button

//                         onClick={() => {
//                             setFormErrors({})
//                             onOpenChange(false)
//                         }}
//                         className="border-border/50"
//                     >
//                         Cancel
//                     </Button>
//                     <Button
//                         onClick={handleSave}
//                         disabled={!name.trim() || !description.trim()}
//                         className="bg-primary text-primary-foreground hover:bg-primary/90"
//                     >
//                         {asset ? "Update Device" : "Create Device"}
//                     </Button>
//                 </DialogFooter>
//             </DialogContent>
//         </Dialog>
//     );
// }




