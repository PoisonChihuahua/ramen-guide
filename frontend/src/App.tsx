import { Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { ShopListPage } from './pages/ShopListPage';
import { ShopDetailPage } from './pages/ShopDetailPage';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { FavoritesPage } from './pages/FavoritesPage';
import { AboutPage } from './pages/AboutPage';
import { ContactPage } from './pages/ContactPage';
import { TermsPage } from './pages/TermsPage';

function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<ShopListPage />} />
        <Route path="shops/:id" element={<ShopDetailPage />} />
        <Route path="login" element={<LoginPage />} />
        <Route path="register" element={<RegisterPage />} />
        <Route path="favorites" element={<FavoritesPage />} />
        <Route path="about" element={<AboutPage />} />
        <Route path="contact" element={<ContactPage />} />
        <Route path="terms" element={<TermsPage />} />
      </Route>
    </Routes>
  );
}

export default App;
