"use client"

import { useState } from "react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import {
  Field,
  FieldDescription,
  FieldGroup,
  FieldLabel,
  FieldSeparator,
} from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import { toast } from "react-hot-toast" // Install react-hot-toast
import { authApi } from "@/services/api"
import { useNavigate } from "react-router-dom"

export function AuthPage() {
  const [isSignup, setIsSignup] = useState(false)
  const [username, setUsername] = useState("admin")
  const [password, setPassword] = useState("Varad@123")
  const [confirmPassword, setConfirmPassword] = useState("")
  const [loading, setLoading] = useState(false)
  const[signupError, setSignupError] = useState({})

  let navigate = useNavigate()

  const toggleMode = () => setIsSignup(!isSignup)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)

    if (isSignup && password !== confirmPassword) {
      toast.error("Passwords do not match")
      setLoading(false)
      return
    }

    try {
      

      const payload = {
        username,
        password,
        ...(isSignup && { role: "User" }) // Default role
      }

      const res =  isSignup ? await authApi.signup(payload) : await authApi.login(payload)

      console.log("The res is ",res)

     

      // if(isSignup && res?.errors)

      if (!res?.success) {
        console.log(res)
        toast.error(res?.error || "Something went wrong")
        setLoading(false)
        return
      }
       localStorage.setItem("username" , res?.data?.user);
      if (Array.isArray(res?.data?.roles) && res.data.roles.length > 0) {
      localStorage.setItem("role", res.data.roles[0]);
     }

      // Login returns token, register can auto-login if backend returns token
      if (!isSignup || (isSignup && res?.data?.token)) {
     
        toast.success(isSignup ? "Signup successful!" : "Login successful!")
      
        
    
        
      return navigate("/")
      } else {
        toast.success("Signup successful! Please login now")
        setIsSignup(!isSignup)
        return;
      }
    } catch (err: any) {
     
      console.log(err)
      toast.error("Network error. Try again later.")
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted p-6">
      <Card className="w-full max-w-2xl overflow-hidden">
        <CardContent className="">
          <div className="p-6 md:p-8">
            <form onSubmit={handleSubmit}>
              <FieldGroup>
                <div className="flex flex-col items-center gap-2 text-center">
                  <h1 className="text-2xl font-bold">
                    {isSignup ? "Create your account" : "Sign in to your account"}
                  </h1>
                  <p className="text-muted-foreground text-sm">
                    {isSignup
                      ? "Enter your details to create a new account"
                      : "Enter your username and password to login"}
                  </p>
                </div>

                {/* Username */}
                <Field>
                  <FieldLabel htmlFor="username">Username</FieldLabel>
                  <Input
                    id="username"
                    type="text"
                    placeholder="Your username"
                    required
                    value={username }
                    onChange={(e) => setUsername(e.target.value)}
                  />
                </Field>

                {/* Password */}
                <Field>
                  <FieldLabel htmlFor="password">Password</FieldLabel>
                  <Input
                    id="password"
                    type="password"
                    required
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                  />
                </Field>

                {/* Confirm Password (Signup only) */}
                {isSignup && (
                  <Field>
                    <FieldLabel htmlFor="confirm-password">Confirm Password</FieldLabel>
                    <Input
                      id="confirm-password"
                      type="password"
                      required
                      value={confirmPassword}
                      onChange={(e) => setConfirmPassword(e.target.value)}
                    />
                    <FieldDescription>Must be at least 8 characters long</FieldDescription>
                  </Field>
                )}

                <Field>
                  <Button type="submit" className="w-full" disabled={loading}>
                    {loading ? "Please wait..." : isSignup ? "Create Account" : "Login"}
                  </Button>
                </Field>

                <FieldDescription className="text-center">
                  {isSignup ? (
                    <>
                      Already have an account?{" "}
                      <button
                        type="button"
                        onClick={toggleMode}
                        className="text-blue-500 hover:underline"
                      >
                        Sign in
                      </button>
                    </>
                  ) : (
                    <>
                      Don't have an account?{" "}
                      <button
                        type="button"
                        onClick={toggleMode}
                        className="text-blue-500 hover:underline"
                      >
                        Sign up
                      </button>
                    </>
                  )}
                </FieldDescription>
              </FieldGroup>
            </form>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
