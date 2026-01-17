import { Routes, Route } from 'react-router-dom'
import { Home } from './pages/Home'
import { DesignSystem } from './pages/DesignSystem'
import { Timeline } from './pages/Timeline'

function App() {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route path="/design" element={<DesignSystem />} />
      <Route path="/timeline" element={<Timeline />} />
    </Routes>
  )
}

export default App
