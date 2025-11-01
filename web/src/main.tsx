import { createRoot } from 'react-dom/client'
import './index.css'
import AppWrapper from './App.tsx'
import { RtcProvider } from "@/services/RTC_API.tsx";

createRoot(document.getElementById('root')!).render(
  <>
    <RtcProvider>
      <AppWrapper />
    </RtcProvider>
  </>
)
