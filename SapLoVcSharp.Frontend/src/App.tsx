import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { TooltipProvider } from '@/components/ui/tooltip';
import Layout from './components/shared/Layout';
import MaterialSelection from './components/MaterialSelection';
import MaterialDetail from './components/MaterialDetail/MaterialDetail';

function App() {
  return (
    <TooltipProvider>
      <BrowserRouter>
        <Layout>
          <Routes>
            <Route path="/" element={<MaterialSelection />} />
            <Route path="/material/:materialNumber" element={<MaterialDetail />} />
          </Routes>
        </Layout>
      </BrowserRouter>
    </TooltipProvider>
  );
}

export default App;
