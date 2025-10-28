import axios from "axios";
import React, { useState } from "react";

export default function Varad() {
    const [loading, setLoading] = useState(false);
    const [message, setMessage] = useState("");

    const handleClick = async () => {
        setLoading(true);
        setMessage("");

        try {
            setLoading(true);

            const response = await axios.post(
                "https://localhost:7066/api/Averages",
                { ColumnName: "Strength" }, // payload/data
                {
                    headers: {
                        "Content-Type": "application/json"
                    },
                    withCredentials: true
                }
            );

           

           
        } catch (error: any) {
          
        } finally {
            setLoading(false);
        }
    };

    return (
        <div>
            <button
                onClick={handleClick}
                disabled={loading}
                className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:bg-gray-400"
            >
                {loading ? "Loading..." : "Calculate Average"}
            </button>

            {message && <p className="mt-2">{message}</p>}
        </div>
    );
}
