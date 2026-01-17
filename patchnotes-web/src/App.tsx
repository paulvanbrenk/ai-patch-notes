import { Routes, Route } from 'react-router-dom'
import { Home } from './pages/Home'
import { DesignSystem } from './pages/DesignSystem'

function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route path="/design" element={<DesignSystem />} />
    </Routes>
  )
}

export default App
