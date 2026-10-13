import { useEffect, useState } from 'react'

const API_URL = import.meta.env.VITE_API_URL as string

export default function App() {
  const [status, setStatus] = useState('Checking API...')

  useEffect(() => {
    fetch(`${API_URL}/api/v1/ping`)
      .then((r) => r.json())
      .then((d) => setStatus(`API says: ${d.message}`))
      .catch((e) => setStatus(`Error: ${e}`))
  }, [])

  return (
    <main style={{ padding: 24, fontFamily: 'sans-serif' }}>
      <h1>Cash-Flow Calendar</h1>
      <p>API URL: {String(API_URL)}</p>
      <p>{status}</p>
    </main>
  )
}