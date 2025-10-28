import { useEffect, useState } from "react";
import { Activity, AlertTriangle, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card } from "@/components/ui/card";
import { Loader } from "./Loader";

export function DeviceForm({ asset, onSave, formErrors, setFormErrors , setLoader,  loader,  onClose}: any) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");

  useEffect(() => {
    if (asset) {
        console.log(asset)
      setName(asset.name || "");
      setDescription(asset.description || "");
    } else {
      setName("");
      setDescription("");
      setFormErrors({});
    }
  }, [asset]);

  const handleSave = () => {
    if (!name.trim() || !description.trim()) return;
    
    onSave({
      name: name.trim(),
      description: description.trim()
    });
    setName("")
    setDescription("")
  };

  console.log(loader)
  const handleCancel = () => {
    setName("")
    setDescription("")
    setFormErrors({});
    onClose?.();   
  };

  return (
    <div    className="border-border/40 bg-gradient-to-br from-card via-card to-card/80 backdrop-blur-sm">
      {
        loader ? <Loader/>:
        <div className="p-2">
        {/* Header */}
        <div className="flex items-center justify-between mb-6 pb-4 border-b border-border/50">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <Activity className="h-5 w-5" />
            </div>
            <div>
              <h2 className="text-xl font-semibold text-foreground">
                {asset ? "Edit Device" : "Create New Device"}
              </h2>
              <p className="text-sm text-muted-foreground">
                {asset ? "Update device information" : "Add a new device to your system"}
              </p>
            </div>
          </div>
          <Button
            onClick={handleCancel}
            className="c cursor-pointer"
          >
           Reset
          </Button>
        </div>

        {/* Form */}
        <div className="space-y-5">
          {/* Device Name */}
          <div className="space-y-2">
            <Label htmlFor="name" className="text-sm font-medium">
              Device Name <span className="text-red-500">*</span>
            </Label>
            <Input
              id="name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g., Production Line Sensor"
              maxLength={15}
              className="border-border/50 bg-background/50 focus:border-primary/50 transition-colors"
            />
            <p className="text-xs text-muted-foreground">{name.length}/15 characters</p>
            {formErrors?.Name && (
              <p className="text-xs text-red-500 flex items-center gap-1">
                <AlertTriangle className="h-3 w-3" />
                {formErrors.Name}
              </p>
            )}
          </div>

          {/* Description */}
          <div className="space-y-2">
            <Label htmlFor="description" className="text-sm font-medium">
              Description <span className="text-red-500">*</span>
            </Label>
            <Textarea
              id="description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Describe the device's purpose and functionality..."
              maxLength={150}
              rows={4}
              className="border-border/50 bg-background/50 focus:border-primary/50 transition-colors resize-none"
            />
            <p className="text-xs text-muted-foreground">{description.length}/150 characters</p>
            {formErrors?.Description && (
              <p className="text-xs text-red-500 flex items-center gap-1">
                <AlertTriangle className="h-3 w-3" />
                {formErrors.Description}
              </p>
            )}
          </div>
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-3 mt-6 pt-4 border-t border-border/50">
          <Button
            variant="outline"
            onClick={handleCancel}
            className="border-border/50"
          >
            Cancel
          </Button>
          <Button
            onClick={handleSave}
            disabled={!name.trim() || !description.trim()}
            className="bg-gradient-to-r from-primary to-primary/90 shadow-md shadow-primary/20 disabled:opacity-50"
          >
            {asset ? "Update Device" : "Create Device"}
          </Button>
        </div>
      </div>
      }
    </div>
  );
}