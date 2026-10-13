import { Route, Routes } from 'react-router'
import Layout from './components/Layout'
import ScenariosPage from './pages/ScenariosPage'
import ScenarioPage from './pages/ScenarioPage'

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<ScenariosPage />} />
        <Route path="scenarios/:id" element={<ScenarioPage />} />
        <Route path="*" element={<p>Page not found.</p>} />
      </Route>
    </Routes>
  )
}