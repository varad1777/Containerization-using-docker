import React from "react";

import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { useNavigate } from "react-router-dom";

export default function NotFoundPage() {

  let navigate = useNavigate();
  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-900 p-6">
      <Card className="max-w-md w-full text-center p-8 shadow-md">
        <CardContent>
          <h1 className="text-3xl font-bold text-slate-900 dark:text-slate-100">
            404 - Page Not Found
          </h1>
          <p className="mt-2 text-slate-600 dark:text-slate-300">
            The page you are looking for does not exist.
          </p>

          <div className="mt-6 flex justify-center gap-4">
           
              <Button  onClick={()=>{
                navigate("/")
              }} >Go Home</Button>
            
            <Button variant="outline" onClick={() => window.history.back()}>
              Go Back
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}