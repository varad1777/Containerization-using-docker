import { useState } from 'react'
import { BrowserRouter, Route, Routes, useLocation } from 'react-router-dom'
import { Toaster } from 'react-hot-toast'

import Index from "@/Pages/Index.jsx"
import Auth from "@/Pages/Auth"
import NotFoundPage from "./Pages/PageNotFound.jsx"
import SignalList from './Pages/SignalList'
import { Header } from './components/Header'
import Varad from './Pages/Varad'

function AppWrapper() {
  return (
    <BrowserRouter>
      <App />
    </BrowserRouter>
  )
}

function App() {
  const location = useLocation()

  // Check if current path is "/auth"
  const showHeader = location.pathname !== '/auth'

  return (
    <>
      <Toaster />
      {showHeader && <Header />}
      <Routes>
        <Route path="/" element={<Index />} />
        <Route path="/auth" element={<Auth />} />
        <Route path="*" element={<NotFoundPage />} />
        <Route path="/ggg" element={<Varad />} />
        <Route path="/assets/:assetId/signals" element={<SignalList />} />
      </Routes>
    </>
  )
}

export default AppWrapper
