import { Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { ProtectedRoute } from './components/ProtectedRoute';
import { GuestRoute } from './components/GuestRoute';
import { ShopListPage } from './pages/ShopListPage';
import { ShopDetailPage } from './pages/ShopDetailPage';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { FavoritesPage } from './pages/FavoritesPage';
import { AboutPage } from './pages/AboutPage';
import { ContactPage } from './pages/ContactPage';
import { TermsPage } from './pages/TermsPage';
import { MyPage } from './pages/MyPage';
import { AdminShopListPage } from './pages/AdminShopListPage';
import { AdminShopFormPage } from './pages/AdminShopFormPage';

function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<ShopListPage />} />
        <Route path="shops/:id" element={<ShopDetailPage />} />
        <Route path="about" element={<AboutPage />} />
        <Route path="contact" element={<ContactPage />} />
        <Route path="terms" element={<TermsPage />} />

        {/* 未ログイン専用 */}
        <Route element={<GuestRoute />}>
          <Route path="login" element={<LoginPage />} />
          <Route path="register" element={<RegisterPage />} />
        </Route>

        {/* ログイン必須 */}
        <Route element={<ProtectedRoute />}>
          <Route path="mypage" element={<MyPage />} />
          <Route path="favorites" element={<FavoritesPage />} />
        </Route>

        {/* 管理者専用 */}
        <Route element={<ProtectedRoute requireAdmin />}>
          <Route path="admin" element={<AdminShopListPage />} />
          <Route path="admin/shops/new" element={<AdminShopFormPage />} />
          <Route path="admin/shops/:id/edit" element={<AdminShopFormPage />} />
        </Route>
      </Route>
    </Routes>
  );
}

export default App;
