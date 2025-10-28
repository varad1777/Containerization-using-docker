

export function Loader({ className = "" }: { className?: string }) {
  return <div className="h-100 w-100 flex items-center justify-center m-auto">
    <div className={`loader ${className}`} />
  </div>;
}