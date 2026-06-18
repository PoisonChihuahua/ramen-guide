import { Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { ShopListPage } from './pages/ShopListPage';
import { AskPage } from './pages/AskPage';
import { ShopDetailPage } from './pages/ShopDetailPage';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';

function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<ShopListPage />} />
        <Route path="ask" element={<AskPage />} />
        <Route path="shops/:id" element={<ShopDetailPage />} />
        <Route path="login" element={<LoginPage />} />
        <Route path="register" element={<RegisterPage />} />
      </Route>
    </Routes>
  );
}

export default App;
