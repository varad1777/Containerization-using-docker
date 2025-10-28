import { Edit2, Trash2, Zap, ArrowRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import { getRole } from "@/services/utilities";
import { useNavigate } from "react-router-dom";

export function DeviceCard({ asset, onEdit, onDelete }: any) {
  const navigate = useNavigate();
  
  const handleViewSignals = () => {
    navigate(`/assets/${asset.id}/signals`);
  };

  return (
    <div className="group relative overflow-hidden rounded-xl border border-border/40 bg-gradient-to-r from-card via-card to-card/80 backdrop-blur-sm transition-all duration-300 hover:border-primary/40 ">
      
      
      {/* Left accent bar */}
      <div className="absolute left-0 top-0 bottom-0 w-1 bg-gradient-to-b from-primary to-secondary opacity-0 transition-opacity duration-300 group-hover:opacity-100" />

      <div className="relative flex items-center gap-4 p-4 md:gap-6 md:p-5">
        {/* Icon */}
        <div className="shrink-0">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-primary to-primary/80 shadow-lg shadow-primary/20 ring-1 ring-primary/30 transition-all duration-300 group-hover:scale-110 group-hover:shadow-xl ">
            <Zap className="h-6 w-6 text-white" />
          </div>
        </div>

        {/* Content */}
        <div className="flex-1 min-w-0 text-start">
          <h3 className="mb-1 text-lg font-semibold text-foreground transition-colors duration-300 group-hover:text-primary">
            {asset.name}
          </h3>
          <p className="text-sm text-muted-foreground line-clamp-1">
            {asset.description}
          </p>
        </div>

        {/* Actions */}
        <div className="flex items-center gap-2 shrink-0">
          <Button
            onClick={() => onEdit(asset)}
            size="icon"
            variant="outline"
            className="h-9 w-9 border transition-all duration-200 border-black hover:bg-white hover:text-black cursor-pointer hover:scale-110"
          >
            <Edit2 className="h-4 w-4" />
          </Button>

          {getRole() === "Admin" && (
            <Button
              onClick={() => asset.id && onDelete(asset.id)}
              size="icon"
              variant="outline"
              className="h-9 w-9 border-red-500 bg-background/50 text-red-500 transition-all duration-200 hover:bg-white hover:text-red-500 cursor-pointer hover:scale-110"
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          )}

          <Button 
            onClick={handleViewSignals}
            className="group/btn relative overflow-hidden bg-gradient-to-r from-primary to-primary/90 shadow-md shadow-primary/20 transition-all duration-300 hover:shadow-lg hover:shadow-primary/30 hover:scale-105"
          >
            <span className="relative z-10 flex items-center gap-2">
              View Signals
              <ArrowRight className="h-4 w-4 transition-transform duration-300 group-hover/btn:translate-x-1" />
            </span>
            {/* Button shine effect */}
            <div className="absolute inset-0 translate-x-[-100%] bg-gradient-to-r from-transparent via-white/20 to-transparent transition-transform duration-500 group-hover/btn:translate-x-[100%]" />
          </Button>
        </div>
      </div>
    </div>
  );
}