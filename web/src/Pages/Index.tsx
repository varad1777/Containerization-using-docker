import { useEffect, useState } from "react";

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"




import {  Server, AlertTriangle } from "lucide-react";
import toast from "react-hot-toast";
import { assetsApi } from "@/services/api";
// import { DeviceDialog } from "@/components/DeviceDialog";
import { DeviceCard } from "@/components/DeviceCard";
import { useNavigate } from "react-router-dom";
import { DeviceForm } from "@/components/DeviceForm";




export default function Index() {

  const [editingAsset, setEditingAsset] = useState<any>(null);

  const [assets, SetAssets] = useState([]);
  const [formErrors, setFormErrors] = useState({});
  const [dialogOpen, setDialogOpen] = useState(false);
  const [isDeleteDialogOpen, setDeleteIsDialogOpen] = useState(false);
  const [Delete, SetDelete] = useState<any>(false);
  const [loader, setLoader] = useState<boolean>(false);

  let navigate = useNavigate();


  const handleSave = async (asset: any) => {
    let loading = toast.loading("Processing...");
    try {
      let data;
      if (editingAsset?.id) {
        data = await assetsApi.update(editingAsset.id, asset);
      } else {
        data = await assetsApi.create(asset);
      }

      toast.dismiss(loading);

      if (data.statusCode == 401) {
        toast.dismiss(loading)
        toast.error("Un-Authorised access...");
        return navigate("/auth");
      }
      if (data.statusCode == 403) {
        toast.dismiss(loading)
        toast.error("You dont have permission to access this...");
        return navigate("/auth");
      }


      if (data.success) {
        toast.success(editingAsset?.id ? "Device Updated Successfully" : "Device Created Successfully");
        // setDialogOpen(false);
        setEditingAsset(null);
        setFormErrors({});
        getAllAssets();
      } else {
        // API returned error, save in state
        setFormErrors(data?.error);
        console.log(data?.error);
        toast.error( (data?.error && typeof data.error === "object"
  ? "Failed to save device" : data?.error)  || "Failed to save device");
      }

    } catch (err) {
      // Only unexpected errors end up here
      console.log(err);
      toast.dismiss(loading);
      toast.error("Something went wrong!");
    }
  };


  const handleDeleteClick = (id: any) => {
    setDeleteIsDialogOpen(true);
    SetDelete(id)
  };


  // Delete device
  const handleConfirmDelete = async () => {
    setDeleteIsDialogOpen(false);
    let deleteId
    if (Delete != null) {

      deleteId = Delete
    }
    const loading = toast.loading("Deleting, please wait...");
    try {
      let res = await assetsApi.delete(deleteId);

      console.log(res);


      if (res.statusCode == 401) {
        toast.dismiss(loading)
        toast.error("Un-Authorised access...");
        return navigate("/auth");
      }
      if (res.statusCode == 403) {
        toast.dismiss(loading)
        toast.error("You dont have permission to access this...");
        // return navigate("/auth");
        return
      }
      toast.dismiss(loading);
      getAllAssets();
      toast.success("Device deleted successfully");
      SetDelete(null)
    } catch (err) {

      console.log(err)
      toast.dismiss(loading);
      toast.error("Failed to delete device");
      SetDelete(null)
    }
  };


  const handleEdit = (asset: any) => {
    setLoader(true);
    setEditingAsset(asset);
    setFormErrors({})


    setTimeout(() => {
      setLoader(false);
    }, 500);
  };



  const getAllAssets = async () => {
    let loading = toast.loading("Loading Devices...");
    const res = await assetsApi.getAll(); // Call API
    if (res.success) {
      SetAssets(res.data); // Set state if successful
      toast.dismiss(loading)
      toast.success("Device loaded successfully..");
    } else {
      if (res.statusCode == 401) {
        toast.dismiss(loading)
        toast.error("Un-Authorised access...");
        return navigate("/auth");
      }
      if (res.statusCode == 403) {
        toast.dismiss(loading)
        toast.error("You dont have permission to access this...");
        return navigate("/auth");
      }
      toast.dismiss(loading)
      toast.error(res.error || "error to fetch the data..")
    }
  };

  useEffect(() => {
    getAllAssets()
  }, [])


  // const handleCreate = () => {
  //   setEditingAsset(null);
  //   setDialogOpen(true);
  // };



  const handleCloseForm = () => {
    setEditingAsset(null);
    setDialogOpen(false);
    setFormErrors({})
  };

  return (


    <div className="min-h-screen bg-background">

      {/* Animated background */}


      {/* Content */}
      <div className="relative">





        {/* Main Content */}
        <main className="container mx-auto px-4 py-4">



          <div className="grid relative grid-cols-8 gap-4">
            <div className="col-span-5 shadow  p-4 shadow-black rounded-2xl border-black">
              {assets && assets.length > 0 ? (
                <div className="space-y-3">
                  {assets.map((asset: any) => (
                    <DeviceCard
                      key={asset.id}
                      asset={asset}
                      onEdit={handleEdit}
                      onDelete={() => handleDeleteClick(asset.id)}

                    />
                  ))}
                </div>
              ) : (
                <div className="flex min-h-[400px] items-center justify-center">
                  <div className="text-center space-y-4">
                    <div className="mx-auto flex h-20 w-20 items-center justify-center rounded-full bg-muted/30">
                      <Server className="h-10 w-10 text-muted-foreground" />
                    </div>
                    <div>
                      <h3 className="text-xl font-semibold text-foreground mb-2">
                        No devices yet
                      </h3>
                      <p className="text-muted-foreground mb-4">
                        Get started by adding your first device
                      </p>

                    </div>
                  </div>
                </div>
              )}
            </div>
            <div className="col-span-3  ">
              <div className="shadow shadow-black rounded-2xl p-4 border-black sticky top-28">

                <DeviceForm formErrors={formErrors}
                  open={dialogOpen}
                  onOpenChange={setDialogOpen}
                  asset={editingAsset}
                  onSave={handleSave}
                  setFormErrors={setFormErrors}
                  loader={loader}
                  setLoader={setLoader}
                  onClose={handleCloseForm} />

              </div>


            </div>
          </div>


        </main>
      </div>

      {/* Dialog
      <DeviceDialog
        formErrors={formErrors}
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        asset={editingAsset}
        onSave={handleSave}
        setFormErrors={setFormErrors}

      /> */}

      <Dialog open={isDeleteDialogOpen} onOpenChange={setDeleteIsDialogOpen}>
        <DialogContent className=" bg-card border border-border/40 p-6 rounded-xl shadow-2xl w-full max-w-md animate-in zoom-in-95 duration-200">

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
            Are you sure you want to permanently delete this Asset? All associated signals will be lost.
          </div>
          <DialogFooter className="flex justify-end gap-2">
            <Button
              className="px-4 py-2 cursor-pointer "
              onClick={() => setDeleteIsDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              className="px-4 py-2 bg-red-500 text-white cursor-pointer rounded"
              onClick={handleConfirmDelete}
            >
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>

  );
}
